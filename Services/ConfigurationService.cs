using System.IO;
using System.Text.Json;
using MeetingAI.Models;

namespace MeetingAI.Services;

public class ConfigurationService
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService()
    {
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeetingAI");
        
        _configFilePath = Path.Combine(_configDirectory, "config.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        Directory.CreateDirectory(_configDirectory);
    }

    public List<AIModelConfig> LoadConfigs()
    {
        try
        {
            if (!File.Exists(_configFilePath))
                return new List<AIModelConfig>();

            var json = File.ReadAllText(_configFilePath);
            var configs = JsonSerializer.Deserialize<List<AIModelConfig>>(json, _jsonOptions);
            return configs ?? new List<AIModelConfig>();
        }
        catch
        {
            return new List<AIModelConfig>();
        }
    }

    public void SaveConfigs(IEnumerable<AIModelConfig> configs)
    {
        try
        {
            var json = JsonSerializer.Serialize(configs.ToList(), _jsonOptions);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"保存配置失败: {ex.Message}", ex);
        }
    }

    public string GetRecordingsDirectory()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MeetingAI",
            "Recordings");
        Directory.CreateDirectory(path);
        return path;
    }
}