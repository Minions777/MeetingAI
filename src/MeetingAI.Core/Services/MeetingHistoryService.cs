using System.Text;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public class MeetingHistoryService
{
    private readonly string _historyDirectory;
    private readonly string _historyDirectoryRoot;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private List<MeetingRecord>? _cache;
    private DateTime _cacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(2);

    public MeetingHistoryService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MeetingAI",
            "History"))
    {
    }

    internal MeetingHistoryService(string historyDirectory)
    {
        _historyDirectory = Path.GetFullPath(historyDirectory);
        _historyDirectoryRoot = EnsureTrailingSeparator(_historyDirectory);
        Directory.CreateDirectory(_historyDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task SaveAsync(MeetingRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.Id))
            record.Id = Guid.NewGuid().ToString("N");

        record.SavedAt = DateTime.UtcNow;

        var filePath = GetRecordPath(record.Id);
        var json = JsonSerializer.Serialize(record, _jsonOptions);

        await _ioLock.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(filePath, json);
            InvalidateCache();
        }
        finally
        {
            _ioLock.Release();
        }

        LoggerService.Info($"会议记录已保存: {record.Id}");
    }

    public async Task<MeetingRecord?> LoadAsync(string id)
    {
        var filePath = GetRecordPath(id);

        if (!File.Exists(filePath))
        {
            LoggerService.Warning($"会议记录不存在: {id}");
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<MeetingRecord>(json, _jsonOptions);
    }

    public async Task<IReadOnlyList<MeetingRecord>> GetAllAsync()
    {
        if (_cache != null && !IsCacheExpired())
            return _cache;

        var records = await LoadRecordsAsync(Directory.EnumerateFiles(_historyDirectory, "*.json"));
        var sorted = records.OrderByDescending(GetRecordSortTime).ToList();

        await _cacheLock.WaitAsync();
        try
        {
            _cache = sorted;
            _cacheTime = DateTime.UtcNow;
        }
        finally
        {
            _cacheLock.Release();
        }

        return sorted;
    }

    public async Task<IReadOnlyList<MeetingRecord>> GetRecentAsync(int count)
    {
        if (count <= 0)
            return Array.Empty<MeetingRecord>();

        var all = await GetAllAsync();
        return all.Take(count).ToList();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var filePath = GetRecordPath(id);

        await _ioLock.WaitAsync();
        try
        {
            if (!File.Exists(filePath))
            {
                LoggerService.Warning($"会议记录不存在，无法删除: {id}");
                return false;
            }

            File.Delete(filePath);
            InvalidateCache();
        }
        finally
        {
            _ioLock.Release();
        }

        LoggerService.Info($"会议记录已删除: {id}");
        return true;
    }

    public async Task<IReadOnlyList<MeetingRecord>> SearchAsync(string keyword)
    {
        var allRecords = await GetAllAsync();

        if (string.IsNullOrWhiteSpace(keyword))
            return allRecords;

        var lowerKeyword = keyword.ToLowerInvariant();

        return allRecords.Where(r =>
            (r.Title?.ToLowerInvariant().Contains(lowerKeyword) ?? false) ||
            (r.Transcript?.Text?.ToLowerInvariant().Contains(lowerKeyword) ?? false) ||
            (r.Summary?.Overview?.ToLowerInvariant().Contains(lowerKeyword) ?? false) ||
            r.Summary?.KeyPoints.Any(kp => kp.ToLowerInvariant().Contains(lowerKeyword)) == true
        ).ToList();
    }

    public async Task<IReadOnlyList<MeetingRecord>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        var allRecords = await GetAllAsync();

        return allRecords.Where(r =>
            r.StartedAt >= start && r.StartedAt <= end
        ).ToList();
    }

    public string ExportToMarkdown(MeetingRecord record)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {record.Title ?? "会议记录"}");
        sb.AppendLine();

        sb.AppendLine("## 基本信息");
        sb.AppendLine($"- **日期**: {record.StartedAt:yyyy-MM-dd}");
        sb.AppendLine($"- **时间**: {record.StartedAt:HH:mm} - {record.EndedAt:HH:mm}");
        sb.AppendLine($"- **时长**: {record.Duration:hh\\:mm\\:ss}");
        if (!string.IsNullOrEmpty(record.AudioFilePath))
            sb.AppendLine($"- **音频文件**: {record.AudioFilePath}");
        sb.AppendLine();

        if (record.Summary != null)
        {
            sb.AppendLine("## AI 摘要");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(record.Summary.Overview))
            {
                sb.AppendLine($"**会议概要**: {record.Summary.Overview}");
                sb.AppendLine();
            }

            AppendList(sb, "关键要点", record.Summary.KeyPoints);
            AppendList(sb, "行动项", record.Summary.ActionItems);
            AppendList(sb, "决议", record.Summary.Decisions);
        }

        if (!string.IsNullOrEmpty(record.Transcript?.Text))
        {
            sb.AppendLine("## 转录文本");
            sb.AppendLine();
            sb.AppendLine(record.Transcript.Text);
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine($"*由 MeetingAI 生成 | {DateTime.Now:yyyy-MM-dd HH:mm}*");

        return sb.ToString();
    }

    public string ExportToText(MeetingRecord record)
    {
        var sb = new StringBuilder();

        sb.AppendLine(record.Title ?? "会议记录");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();

        sb.AppendLine($"日期: {record.StartedAt:yyyy-MM-dd}");
        sb.AppendLine($"时间: {record.StartedAt:HH:mm} - {record.EndedAt:HH:mm}");
        sb.AppendLine($"时长: {record.Duration:hh\\:mm\\:ss}");
        sb.AppendLine();

        if (record.Summary != null)
        {
            if (!string.IsNullOrEmpty(record.Summary.Overview))
                sb.AppendLine($"【概要】{record.Summary.Overview}");

            AppendNumberedList(sb, "关键要点", record.Summary.KeyPoints);
            AppendNumberedList(sb, "行动项", record.Summary.ActionItems);
        }

        return sb.ToString();
    }

    public async Task ExportAllAsync(string outputDirectory, ExportFormat format)
    {
        var records = await GetAllAsync();
        Directory.CreateDirectory(outputDirectory);

        foreach (var record in records)
        {
            var ext = format switch { ExportFormat.Markdown => "md", ExportFormat.Text => "txt", _ => "md" };
            var fileName = $"{record.StartedAt:yyyyMMdd_HHmmss}_{NormalizeRecordId(record.Id)[..8]}.{ext}";
            var filePath = Path.Combine(outputDirectory, fileName);

            var content = format switch
            {
                ExportFormat.Markdown => ExportToMarkdown(record),
                ExportFormat.Text => ExportToText(record),
                _ => ExportToMarkdown(record)
            };

            await File.WriteAllTextAsync(filePath, content);
        }

        LoggerService.Info($"已导出 {records.Count} 条会议记录到: {outputDirectory}");
    }

    public async Task<MeetingHistoryStats> GetStatsAsync()
    {
        var records = await GetAllAsync();
        var averageTicks = records.Any() ? (long)records.Average(r => r.Duration.Ticks) : 0;

        return new MeetingHistoryStats
        {
            TotalRecords = records.Count,
            TotalDuration = TimeSpan.FromTicks(records.Sum(r => r.Duration.Ticks)),
            FirstMeeting = records.LastOrDefault()?.StartedAt,
            LastMeeting = records.FirstOrDefault()?.StartedAt,
            AverageDuration = TimeSpan.FromTicks(Math.Max(0, averageTicks))
        };
    }

    private string GetRecordPath(string id)
    {
        var safeId = NormalizeRecordId(id);
        var fullPath = Path.GetFullPath(Path.Combine(_historyDirectory, $"{safeId}.json"));

        if (!fullPath.StartsWith(_historyDirectoryRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Meeting record path escaped the history directory.");

        return fullPath;
    }

    private async Task<List<MeetingRecord>> LoadRecordsAsync(IEnumerable<string> files)
    {
        var records = new List<MeetingRecord>();

        foreach (var file in files)
        {
            try
            {
                var fullPath = Path.GetFullPath(file);
                if (!fullPath.StartsWith(_historyDirectoryRoot, StringComparison.OrdinalIgnoreCase))
                    continue;

                var json = await File.ReadAllTextAsync(fullPath);
                var record = JsonSerializer.Deserialize<MeetingRecord>(json, _jsonOptions);
                if (record != null)
                    records.Add(record);
            }
            catch (Exception ex)
            {
                LoggerService.Error($"加载会议记录失败: {file}", ex);
            }
        }

        return records;
    }

    private static DateTime GetRecordSortTime(MeetingRecord record)
    {
        if (record.SavedAt != default)
            return record.SavedAt;

        return record.StartedAt;
    }

    private bool IsCacheExpired() => DateTime.UtcNow - _cacheTime > _cacheTtl;

    private void InvalidateCache()
    {
        _cache = null;
        _cacheTime = DateTime.MinValue;
    }

    private static string NormalizeRecordId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Meeting record id cannot be empty.", nameof(id));

        var safeId = new string(id
            .Select(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_')
            .ToArray())
            .Trim('_');

        if (string.IsNullOrWhiteSpace(safeId))
            throw new ArgumentException("Meeting record id contains no valid file-name characters.", nameof(id));

        return safeId;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static void AppendList(StringBuilder sb, string title, IEnumerable<string> items)
    {
        var values = items.ToList();
        if (values.Count == 0)
            return;

        sb.AppendLine($"**{title}**:");
        foreach (var item in values)
            sb.AppendLine($"- {item}");
        sb.AppendLine();
    }

    private static void AppendList(StringBuilder sb, string title, IEnumerable<ActionItem> items)
    {
        var values = items.ToList();
        if (values.Count == 0)
            return;

        sb.AppendLine($"**{title}**:");
        foreach (var item in values)
        {
            var assignee = string.IsNullOrEmpty(item.Assignee) ? "" : $" [@{item.Assignee}]";
            var due = item.DueDate == null ? "" : $" (截止: {item.DueDate:yyyy-MM-dd})";
            var priority = item.Priority == Priority.High || item.Priority == Priority.Critical ? " ⚠️" : "";
            sb.AppendLine($"- {item.Description}{assignee}{due}{priority}");
        }
        sb.AppendLine();
    }

    private static void AppendNumberedList(StringBuilder sb, string title, IEnumerable<string> items)
    {
        var values = items.ToList();
        if (values.Count == 0)
            return;

        sb.AppendLine();
        sb.AppendLine($"【{title}】");
        foreach (var (item, index) in values.Select((item, index) => (item, index)))
            sb.AppendLine($"  {index + 1}. {item}");
    }

    private static void AppendNumberedList(StringBuilder sb, string title, IEnumerable<ActionItem> items)
    {
        var values = items.ToList();
        if (values.Count == 0)
            return;

        sb.AppendLine();
        sb.AppendLine($"【{title}】");
        foreach (var (item, index) in values.Select((item, index) => (item, index)))
        {
            var assignee = string.IsNullOrEmpty(item.Assignee) ? "" : $" @{item.Assignee}";
            var due = item.DueDate == null ? "" : $" (截止: {item.DueDate:yyyy-MM-dd})";
            sb.AppendLine($"  {index + 1}. {item.Description}{assignee}{due}");
        }
    }
}

public enum ExportFormat
{
    Markdown,
    Text
}

public static class ExportFormatExtensions
{
    public static string Extension(this ExportFormat format) => format switch
    {
        ExportFormat.Markdown => "md",
        ExportFormat.Text => "txt",
        _ => "md"
    };
}

public class MeetingHistoryStats
{
    public int TotalRecords { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public DateTime? FirstMeeting { get; set; }
    public DateTime? LastMeeting { get; set; }
    public TimeSpan AverageDuration { get; set; }
}
