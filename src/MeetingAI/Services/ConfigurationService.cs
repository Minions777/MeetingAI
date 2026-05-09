using System.IO;
using System.Text.Json;
using MeetingAI.Models;

namespace MeetingAI.Services;

public class ConfigurationService
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService()
    {
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeetingAI", "settings.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    public List<AIModelConfig> LoadConfigs()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                LoggerService.Info("配置文件不存在，创建默认配置");
                var defaults = new List<AIModelConfig>
                {
                    AIModelConfig.CreateDefault(AIProvider.OpenAI),
                    AIModelConfig.CreateDefault(AIProvider.DeepSeek)
                };
                SaveConfigs(defaults);
                return defaults;
            }

            var json = File.ReadAllText(_configPath);
            var configs = JsonSerializer.Deserialize<List<AIModelConfig>>(json, _jsonOptions);
            
            if (configs == null || configs.Count == 0)
            {
                LoggerService.Warning("配置文件为空，创建默认配置");
                var defaults = new List<AIModelConfig> { AIModelConfig.CreateDefault(AIProvider.OpenAI) };
                SaveConfigs(defaults);
                return defaults;
            }

            LoggerService.Info("加载了 " + configs.Count + " 个配置");
            return configs;
        }
        catch (Exception ex)
        {
            LoggerService.Error("加载配置失败", ex);
            return new List<AIModelConfig> { AIModelConfig.CreateDefault(AIProvider.OpenAI) };
        }
    }

    public void SaveConfigs(List<AIModelConfig> configs)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(configs, _jsonOptions);
            File.WriteAllText(_configPath, json);
            LoggerService.Info("保存了 " + configs.Count + " 个配置");
        }
        catch (Exception ex)
        {
            LoggerService.Error("保存配置失败", ex);
            throw;
        }
    }

    public string GetConfigPath() => _configPath;
}
