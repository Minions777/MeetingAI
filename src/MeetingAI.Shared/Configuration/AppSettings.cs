namespace MeetingAI.Shared.Configuration;

public class AppSettings
{
    public string Version { get; set; } = "2.0.0";
    public List<ProviderConfig> Providers { get; set; } = new();
    public string DefaultProviderId { get; set; } = string.Empty;
    public string Language { get; set; } = "zh-CN";
    public RecordingSettings Recording { get; set; } = new();
    public bool IsDarkTheme { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static AppSettings CreateDefault()
    {
        var settings = new AppSettings();

        // Create default providers
        var openAI = ProviderConfig.CreateDefault(AIProviderType.OpenAI);
        var deepSeek = ProviderConfig.CreateDefault(AIProviderType.DeepSeek);

        settings.Providers.Add(openAI);
        settings.Providers.Add(deepSeek);
        settings.DefaultProviderId = openAI.Id;

        return settings;
    }
}
