using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

/// <summary>
/// 会议历史服务
/// 管理会议记录的存储、查询和导出
/// </summary>
public class MeetingHistoryService
{
    private readonly string _historyDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly object _lock = new();
    
    public MeetingHistoryService()
    {
        _historyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MeetingAI", "History");
            
        Directory.CreateDirectory(_historyDirectory);
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    /// <summary>
    /// 保存会议记录
    /// </summary>
    public async Task SaveAsync(MeetingRecord record)
    {
        if (record == null)
            throw new ArgumentNullException(nameof(record));
            
        if (string.IsNullOrEmpty(record.Id))
            record.Id = Guid.NewGuid().ToString("N");
            
        record.SavedAt = DateTime.UtcNow;
        
        var filePath = GetRecordPath(record.Id);
        
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(record, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        LoggerService.Info($"会议记录已保存: {record.Id}");
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 加载会议记录
    /// </summary>
    public Task<MeetingRecord?> LoadAsync(string id)
    {
        var filePath = GetRecordPath(id);
        
        if (!File.Exists(filePath))
        {
            LoggerService.Warning($"会议记录不存在: {id}");
            return Task.FromResult<MeetingRecord?>(null);
        }
        
        lock (_lock)
        {
            var json = File.ReadAllText(filePath);
            var record = JsonSerializer.Deserialize<MeetingRecord>(json, _jsonOptions);
            return Task.FromResult(record);
        }
    }
    
    /// <summary>
    /// 获取所有会议记录（按时间倒序）
    /// </summary>
    public Task<IReadOnlyList<MeetingRecord>> GetAllAsync()
    {
        var records = new List<MeetingRecord>();
        
        lock (_lock)
        {
            var files = Directory.GetFiles(_historyDirectory, "*.json");
            
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var record = JsonSerializer.Deserialize<MeetingRecord>(json, _jsonOptions);
                    if (record != null)
                        records.Add(record);
                }
                catch (Exception ex)
                {
                    LoggerService.Error($"加载会议记录失败: {file}", ex);
                }
            }
        }
        
        var sortedRecords = records
            .OrderByDescending(r => r.StartedAt)
            .ToList();
            
        return Task.FromResult<IReadOnlyList<MeetingRecord>>(sortedRecords);
    }
    
    /// <summary>
    /// 删除会议记录
    /// </summary>
    public Task<bool> DeleteAsync(string id)
    {
        var filePath = GetRecordPath(id);
        
        if (!File.Exists(filePath))
        {
            LoggerService.Warning($"会议记录不存在，无法删除: {id}");
            return Task.FromResult(false);
        }
        
        lock (_lock)
        {
            File.Delete(filePath);
        }
        
        LoggerService.Info($"会议记录已删除: {id}");
        return Task.FromResult(true);
    }
    
    /// <summary>
    /// 搜索会议记录
    /// </summary>
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
    
    /// <summary>
    /// 获取日期范围内的记录
    /// </summary>
    public async Task<IReadOnlyList<MeetingRecord>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        var allRecords = await GetAllAsync();
        
        return allRecords.Where(r =>
            r.StartedAt >= start && r.StartedAt <= end
        ).ToList();
    }
    
    /// <summary>
    /// 导出记录为 Markdown
    /// </summary>
    public string ExportToMarkdown(MeetingRecord record)
    {
        var sb = new System.Text.StringBuilder();
        
        // 标题
        sb.AppendLine($"# {record.Title ?? "会议记录"}");
        sb.AppendLine();
        
        // 基本信息
        sb.AppendLine("## 基本信息");
        sb.AppendLine($"- **日期**: {record.StartedAt:yyyy-MM-dd}");
        sb.AppendLine($"- **时间**: {record.StartedAt:HH:mm} - {record.EndedAt:HH:mm}");
        sb.AppendLine($"- **时长**: {record.Duration:hh\\:mm\\:ss}");
        if (!string.IsNullOrEmpty(record.AudioFilePath))
            sb.AppendLine($"- **音频文件**: {record.AudioFilePath}");
        sb.AppendLine();
        
        // 摘要
        if (record.Summary != null)
        {
            sb.AppendLine("## AI 摘要");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(record.Summary.Overview))
            {
                sb.AppendLine($"**会议概要**: {record.Summary.Overview}");
                sb.AppendLine();
            }
            
            if (record.Summary.KeyPoints.Any())
            {
                sb.AppendLine("**关键要点**:");
                foreach (var point in record.Summary.KeyPoints)
                    sb.AppendLine($"- {point}");
                sb.AppendLine();
            }
            
            if (record.Summary.ActionItems.Any())
            {
                sb.AppendLine("**行动项**:");
                foreach (var item in record.Summary.ActionItems)
                    sb.AppendLine($"- {item}");
                sb.AppendLine();
            }
            
            if (record.Summary.Decisions.Any())
            {
                sb.AppendLine("**决议**:");
                foreach (var decision in record.Summary.Decisions)
                    sb.AppendLine($"- {decision}");
                sb.AppendLine();
            }
        }
        
        // 转录文本
        if (!string.IsNullOrEmpty(record.Transcript?.Text))
        {
            sb.AppendLine("## 转录文本");
            sb.AppendLine();
            sb.AppendLine(record.Transcript.Text);
            sb.AppendLine();
        }
        
        // 底部信息
        sb.AppendLine("---");
        sb.AppendLine($"*由 MeetingAI 生成 | {DateTime.Now:yyyy-MM-dd HH:mm}*");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 导出记录为纯文本
    /// </summary>
    public string ExportToText(MeetingRecord record)
    {
        var sb = new System.Text.StringBuilder();
        
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
            
            if (record.Summary.KeyPoints.Any())
            {
                sb.AppendLine();
                sb.AppendLine("【关键要点】");
                foreach (var (point, i) in record.Summary.KeyPoints.Select((p, i) => (p, i)))
                    sb.AppendLine($"  {i + 1}. {point}");
            }
            
            if (record.Summary.ActionItems.Any())
            {
                sb.AppendLine();
                sb.AppendLine("【行动项】");
                foreach (var (item, i) in record.Summary.ActionItems.Select((p, i) => (p, i)))
                    sb.AppendLine($"  {i + 1}. {item}");
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 批量导出
    /// </summary>
    public async Task ExportAllAsync(string outputDirectory, ExportFormat format)
    {
        var records = await GetAllAsync();
        Directory.CreateDirectory(outputDirectory);
        
        foreach (var record in records)
        {
            var ext = format switch { ExportFormat.Markdown => "md", ExportFormat.Text => "txt", _ => "md" };
            var fileName = $"{record.StartedAt:yyyyMMdd_HHmmss}_{record.Id[..8]}.{ext}";
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
    
    /// <summary>
    /// 获取记录统计信息
    /// </summary>
    public async Task<MeetingHistoryStats> GetStatsAsync()
    {
        var records = await GetAllAsync();
        
        return new MeetingHistoryStats
        {
            TotalRecords = records.Count,
            TotalDuration = TimeSpan.FromTicks(records.Sum(r => r.Duration.Ticks)),
            FirstMeeting = records.LastOrDefault()?.StartedAt,
            LastMeeting = records.FirstOrDefault()?.StartedAt,
            AverageDuration = records.Any() 
                ? TimeSpan.FromTicks(records.Average(r => r.Duration.Ticks) > 0 ? (long)records.Average(r => r.Duration.Ticks) : 0)
                : TimeSpan.Zero
        };
    }
    
    /// <summary>
    /// 获取记录文件路径
    /// </summary>
    private string GetRecordPath(string id)
    {
        return Path.Combine(_historyDirectory, $"{id}.json");
    }
}

/// <summary>
/// 导出格式
/// </summary>
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

/// <summary>
/// 会议历史统计
/// </summary>
public class MeetingHistoryStats
{
    public int TotalRecords { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public DateTime? FirstMeeting { get; set; }
    public DateTime? LastMeeting { get; set; }
    public TimeSpan AverageDuration { get; set; }
}
