using System.Text.Json;
using System.Text.Json.Serialization;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Shared.Configuration;

public class ConfigurationService
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings? _cachedSettings;
    
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
        if (_cachedSettings != null)
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
            LoggerService.Info("配置已保存");
        }
        catch (Exception ex)
        {
            LoggerService.Error("保存配置失败", ex);
            throw;
        }
    }
    
    public void ClearCache() => _cachedSettings = null;
    
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
}
