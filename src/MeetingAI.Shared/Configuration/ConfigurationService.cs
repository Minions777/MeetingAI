using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Shared.Configuration;

public class ConfigurationService
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings? _cachedSettings;
    private DateTime _lastLoadTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    
    public ConfigurationService()
    {
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
        // 检查缓存是否有效
        if (_cachedSettings != null && !IsCacheExpired())
            return _cachedSettings;
            
        try
        {
            if (!File.Exists(_configPath))
            {
                LoggerService.Info("配置文件不存在，创建默认配置");
                _cachedSettings = AppSettings.CreateDefault();
                Save(_cachedSettings);
                return _cachedSettings;
            }
            
            var json = File.ReadAllText(_configPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            
            if (settings == null)
            {
                LoggerService.Warning("配置文件为空，创建默认配置");
                _cachedSettings = AppSettings.CreateDefault();
                Save(_cachedSettings);
                return _cachedSettings;
            }
            
            // Migrate if needed
            if (settings.Providers.Any())
            {
                foreach (var provider in settings.Providers)
                {
                    SecureStorage.DecryptConfig(provider);
                }
            }
            
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
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            
            // Encrypt sensitive data before saving
            foreach (var provider in settings.Providers)
            {
                SecureStorage.EncryptConfig(provider);
            }
            
            settings.UpdatedAt = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_configPath, json);
            
            // Decrypt after saving for in-memory use
            foreach (var provider in settings.Providers)
            {
                SecureStorage.DecryptConfig(provider);
            }
            
            _cachedSettings = settings;
            _lastLoadTime = DateTime.UtcNow;
            LoggerService.Info("配置已保存并更新缓存");
        }
        catch (Exception ex)
        {
            LoggerService.Error("保存配置失败", ex);
            throw;
        }
    }
    
    /// <summary>
    /// 检查缓存是否过期
    /// </summary>
    private bool IsCacheExpired()
    {
        return DateTime.UtcNow - _lastLoadTime > _cacheExpiration;
    }
    
    /// <summary>
    /// 强制刷新配置（清除缓存并重新加载）
    /// </summary>
    public AppSettings Reload()
    {
        LoggerService.Info("强制刷新配置...");
        ClearCache();
        return Load();
    }
    
    /// <summary>
    /// 清除配置缓存
    /// </summary>
    public void ClearCache()
    {
        _cachedSettings = null;
        _lastLoadTime = DateTime.MinValue;
        LoggerService.Debug("配置缓存已清除");
    }
    
    /// <summary>
    /// 获取备份目录路径
    /// </summary>
    public string GetBackupPath()
    {
        var backupDir = Constants.AppConstants.Paths.Backup;
        Directory.CreateDirectory(backupDir);
        return Path.Combine(backupDir, $"settings_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
    }
    
    /// <summary>
    /// 备份当前配置
    /// </summary>
    public void Backup()
    {
        if (File.Exists(_configPath))
        {
            var backupPath = GetBackupPath();
            File.Copy(_configPath, backupPath, true);
            LoggerService.Info($"配置已备份到: {backupPath}");
        }
    }
    
    /// <summary>
    /// 从备份恢复配置
    /// </summary>
    public bool RestoreFromBackup(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                LoggerService.Error($"备份文件不存在: {backupPath}");
                return false;
            }
            
            // 先备份当前配置
            Backup();
            
            // 复制备份文件到配置路径
            File.Copy(backupPath, _configPath, true);
            
            // 清除缓存并重新加载
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
    
    /// <summary>
    /// 获取所有可用的备份文件
    /// </summary>
    public IEnumerable<FileInfo> GetBackupFiles()
    {
        var backupDir = Constants.AppConstants.Paths.Backup;
        if (!Directory.Exists(backupDir))
            return Enumerable.Empty<FileInfo>();
            
        return Directory.GetFiles(backupDir, "settings_backup_*.json")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .ToList();
    }
    
    /// <summary>
    /// 验证配置完整性
    /// </summary>
    public (bool IsValid, List<string> Errors) ValidateConfiguration()
    {
        var errors = new List<string>();
        
        try
        {
            var settings = Load();
            
            // 检查是否有可用的 Provider
            if (!settings.Providers.Any(p => p.IsEnabled))
            {
                errors.Add("没有已启用的 AI Provider");
            }
            
            // 检查默认 Provider 是否存在
            var defaultProvider = settings.Providers.FirstOrDefault(p => p.Id == settings.DefaultProviderId);
            if (defaultProvider == null)
            {
                errors.Add("默认 Provider 不存在");
            }
            else if (string.IsNullOrEmpty(defaultProvider.ApiKey))
            {
                errors.Add($"默认 Provider '{defaultProvider.Name}' 未配置 API Key");
            }
            
            // 验证加密配置
            foreach (var provider in settings.Providers.Where(p => p.IsEnabled))
            {
                if (string.IsNullOrEmpty(provider.ApiKey))
                {
                    errors.Add($"Provider '{provider.Name}' API Key 为空");
                }
            }
            
            return (errors.Count == 0, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"配置验证失败: {ex.Message}");
            return (false, errors);
        }
    }
    
    /// <summary>
    /// 导出配置（不含敏感信息）
    /// </summary>
    public string ExportSafe()
    {
        var settings = Load();
        
        // 创建一个安全的副本，移除 API Key
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
