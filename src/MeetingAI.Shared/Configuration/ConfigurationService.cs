using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Shared.Configuration;

public class ConfigurationService : IConfigurationService
{
    public event EventHandler? SettingsChanged;

    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ISecureStorage _secureStorage;
    private AppSettings? _cachedSettings;
    private DateTime _lastLoadTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public ConfigurationService(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
        _configPath = Constants.AppConstants.Paths.Settings;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public AppSettings Load()
    {
        if (_cachedSettings != null && !IsCacheExpired())
            return _cachedSettings;

        try
        {
            if (!File.Exists(_configPath))
            {
                LoggerService.Info("配置文件不存在，创建默认配置");
                _cachedSettings = AppSettings.CreateDefault();
                Task.Run(() => PersistSettings(_cachedSettings, raiseEvent: false));
                return _cachedSettings;
            }

            var json = File.ReadAllText(_configPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            if (settings == null)
            {
                LoggerService.Warning("配置文件为空，创建默认配置");
                _cachedSettings = AppSettings.CreateDefault();
                Task.Run(() => PersistSettings(_cachedSettings, raiseEvent: false));
                return _cachedSettings;
            }

            DecryptAllProviders(settings);
            LoggerService.Info($"加载了 {settings.Providers.Count} 个Provider配置");
            _cachedSettings = settings;
            _lastLoadTime = DateTime.UtcNow;
            return settings;
        }
        catch (Exception ex)
        {
            LoggerService.Error("加载配置失败", ex);
            _cachedSettings = AppSettings.CreateDefault();
            return _cachedSettings;
        }
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (_cachedSettings != null && !IsCacheExpired())
            return _cachedSettings;

        try
        {
            if (!File.Exists(_configPath))
            {
                LoggerService.Info("配置文件不存在，创建默认配置");
                _cachedSettings = AppSettings.CreateDefault();
                await PersistSettingsAsync(_cachedSettings, raiseEvent: false);
                return _cachedSettings;
            }

            var json = await File.ReadAllTextAsync(_configPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            if (settings == null)
            {
                LoggerService.Warning("配置文件为空，创建默认配置");
                _cachedSettings = AppSettings.CreateDefault();
                await PersistSettingsAsync(_cachedSettings, raiseEvent: false);
                return _cachedSettings;
            }

            DecryptAllProviders(settings);
            LoggerService.Info($"加载了 {settings.Providers.Count} 个Provider配置");
            _cachedSettings = settings;
            _lastLoadTime = DateTime.UtcNow;
            return settings;
        }
        catch (Exception ex)
        {
            LoggerService.Error("加载配置失败", ex);
            _cachedSettings = AppSettings.CreateDefault();
            return _cachedSettings;
        }
    }

    public void Save(AppSettings settings)
    {
        Task.Run(() => PersistSettingsAsync(settings, raiseEvent: true)).GetAwaiter().GetResult();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await PersistSettingsAsync(settings, raiseEvent: true);
    }

    private void PersistSettings(AppSettings settings, bool raiseEvent)
        => PersistSettingsAsync(settings, raiseEvent).GetAwaiter().GetResult();

    private async Task PersistSettingsAsync(AppSettings settings, bool raiseEvent)
    {
        await _saveLock.WaitAsync();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);

            EncryptAllProviders(settings);
            settings.UpdatedAt = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_configPath, json);

            DecryptAllProviders(settings);

            _cachedSettings = settings;
            _lastLoadTime = DateTime.UtcNow;
            LoggerService.Info("配置已保存并更新缓存");

            if (raiseEvent)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            LoggerService.Error("保存配置失败", ex);
            throw;
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private void EncryptAllProviders(AppSettings settings)
    {
        foreach (var provider in settings.Providers)
            _secureStorage.EncryptConfig(provider);
    }

    private void DecryptAllProviders(AppSettings settings)
    {
        foreach (var provider in settings.Providers)
            _secureStorage.DecryptConfig(provider);
    }

    private bool IsCacheExpired() => DateTime.UtcNow - _lastLoadTime > _cacheExpiration;

    public AppSettings Reload()
    {
        LoggerService.Info("强制刷新配置...");
        ClearCache();
        return Load();
    }

    public void ClearCache()
    {
        _cachedSettings = null;
        _lastLoadTime = DateTime.MinValue;
        LoggerService.Debug("配置缓存已清除");
    }

    public string GetBackupPath()
    {
        var backupDir = Constants.AppConstants.Paths.Backup;
        Directory.CreateDirectory(backupDir);
        return Path.Combine(backupDir, $"settings_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
    }

    public void Backup()
    {
        if (File.Exists(_configPath))
        {
            var backupPath = GetBackupPath();
            File.Copy(_configPath, backupPath, true);
            LoggerService.Info($"配置已备份到: {backupPath}");
        }
    }

    public bool RestoreFromBackup(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                LoggerService.Error($"备份文件不存在: {backupPath}");
                return false;
            }
            Backup();
            File.Copy(backupPath, _configPath, true);
            ClearCache();
            Load();
            LoggerService.Info($"配置已从备份恢复: {backupPath}");
            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Error("恢复配置失败", ex);
            return false;
        }
    }

    public IEnumerable<FileInfo> GetBackupFiles()
    {
        var backupDir = Constants.AppConstants.Paths.Backup;
        if (!Directory.Exists(backupDir))
            return [];

        return Directory.GetFiles(backupDir, "settings_backup_*.json")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .ToList();
    }

    public (bool IsValid, List<string> Errors) ValidateConfiguration()
    {
        var errors = new List<string>();

        try
        {
            var settings = Load();

            if (!settings.Providers.Any(p => p.IsEnabled))
                errors.Add("没有已启用的 AI Provider");

            var defaultProvider = settings.Providers.FirstOrDefault(p => p.Id == settings.DefaultProviderId);
            if (defaultProvider == null)
                errors.Add("默认 Provider 不存在");
            else if (string.IsNullOrEmpty(defaultProvider.ApiKey))
                errors.Add($"默认 Provider '{defaultProvider.Name}' 未配置 API Key");

            foreach (var provider in settings.Providers.Where(p => p.IsEnabled))
            {
                if (string.IsNullOrEmpty(provider.ApiKey))
                    errors.Add($"Provider '{provider.Name}' API Key 为空");
            }

            return (errors.Count == 0, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"配置验证失败: {ex.Message}");
            return (false, errors);
        }
    }

    public string ExportSafe()
    {
        var settings = Load();

        var safeSettings = new AppSettings
        {
            Version = settings.Version,
            DefaultProviderId = settings.DefaultProviderId,
            UpdatedAt = settings.UpdatedAt,
            Providers = settings.Providers.Select(p => new ProviderConfig
            {
                Id = p.Id,
                Name = p.Name,
                ProviderType = p.ProviderType,
                Model = p.Model,
                ApiKey = "***REDACTED***",
                BaseUrl = p.BaseUrl,
                IsEnabled = p.IsEnabled,
                Temperature = p.Temperature,
                MaxTokens = p.MaxTokens,
                SystemPrompt = p.SystemPrompt,
                UpdatedAt = p.UpdatedAt
            }).ToList()
        };

        return JsonSerializer.Serialize(safeSettings, _jsonOptions);
    }
}