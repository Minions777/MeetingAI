using System.IO;
using System.Text.Json;
using MeetingAI.Models;

namespace MeetingAI.Services;

public class ConfigurationService
{
    private readonly string _configPath;

    public ConfigurationService()
    {
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeetingAI",
            "configs.json");
    }

    public List<AIModelConfig> LoadConfigs()
    {
        if (!File.Exists(_configPath))
            return new List<AIModelConfig>();

        var json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<List<AIModelConfig>>(json) ?? new List<AIModelConfig>();
    }

    public void SaveConfigs(List<AIModelConfig> configs)
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(configs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}