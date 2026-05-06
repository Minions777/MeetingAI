using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.i18n;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Client.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ConfigurationService _configService;
    
    [ObservableProperty] private ObservableCollection<ProviderConfig> _providers = new();
    [ObservableProperty] private ProviderConfig? _selectedProvider;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusText = "";
    
    // Editing fields
    [ObservableProperty] private string _editName = "";
    [ObservableProperty] private AIProviderType _editProviderType;
    
    private string _editApiKey = "";
    public string EditApiKey
    {
        get => _editApiKey;
        set => SetProperty(ref _editApiKey, value);
    }
    
    [ObservableProperty] private string _editBaseUrl = "";
    [ObservableProperty] private string _editModel = "";
    [ObservableProperty] private string _editWhisperModel = "";
    [ObservableProperty] private int _editMaxTokens = 4096;
    [ObservableProperty] private double _editTemperature = 0.7;
    [ObservableProperty] private bool _editIsEnabled = true;
    
    public IReadOnlyList<AIProviderType> AvailableProviderTypes { get; } = Enum.GetValues<AIProviderType>();
    
    public SettingsViewModel(ConfigurationService configService)
    {
        _configService = configService;
        LoadProviders();
    }
    
    private void LoadProviders()
    {
        var settings = _configService.Load();
        Providers.Clear();
        foreach (var provider in settings.Providers)
        {
            Providers.Add(provider);
        }
    }
    
    [RelayCommand]
    private void AddProvider()
    {
        SelectedProvider = null;
        EditName = "新配置";
        EditProviderType = AIProviderType.OpenAI;
        EditApiKey = "";
        EditBaseUrl = "https://api.openai.com/v1";
        EditModel = "gpt-4o-mini";
        EditWhisperModel = "whisper-1";
        EditMaxTokens = 4096;
        EditTemperature = 0.7;
        EditIsEnabled = true;
        IsEditing = true;
    }
    
    [RelayCommand]
    private void EditProvider(ProviderConfig? provider)
    {
        if (provider == null) return;
        
        SelectedProvider = provider;
        EditName = provider.Name;
        EditProviderType = provider.ProviderType;
        EditApiKey = provider.ApiKey;
        EditBaseUrl = provider.BaseUrl;
        EditModel = provider.Model;
        EditWhisperModel = provider.WhisperModel;
        EditMaxTokens = provider.MaxTokens;
        EditTemperature = provider.Temperature;
        EditIsEnabled = provider.IsEnabled;
        IsEditing = true;
    }
    
    [RelayCommand]
    private void DeleteProvider(ProviderConfig? provider)
    {
        if (provider == null) return;
        
        var settings = _configService.Load();
        var toRemove = settings.Providers.FirstOrDefault(p => p.Id == provider.Id);
        if (toRemove != null)
        {
            settings.Providers.Remove(toRemove);
            _configService.Save(settings);
            Providers.Remove(provider);
            StatusText = "Provider deleted";
        }
    }
    
    [RelayCommand]
    private void SaveProvider()
    {
        var settings = _configService.Load();
        
        ProviderConfig config;
        if (SelectedProvider != null)
        {
            config = settings.Providers.First(p => p.Id == SelectedProvider.Id);
        }
        else
        {
            config = new ProviderConfig { Id = Guid.NewGuid().ToString() };
            settings.Providers.Add(config);
        }
        
        config.Name = EditName;
        config.ProviderType = EditProviderType;
        config.ApiKey = EditApiKey;
        config.BaseUrl = EditBaseUrl;
        config.Model = EditModel;
        config.WhisperModel = EditWhisperModel;
        config.MaxTokens = EditMaxTokens;
        config.Temperature = EditTemperature;
        config.IsEnabled = EditIsEnabled;
        config.UpdatedAt = DateTime.UtcNow;
        
        _configService.Save(settings);
        _configService.ClearCache();
        
        IsEditing = false;
        LoadProviders();
        StatusText = LocalizationManager.Get("Success");
        LoggerService.Info($"Provider saved: {config.Name}");
    }
    
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }
    
    [RelayCommand]
    private void SaveAndClose()
    {
        if (IsEditing)
        {
            SaveProvider();
        }
        // The window will handle closing
    }
    
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrEmpty(EditApiKey))
        {
            StatusText = "请输入 API Key";
            return;
        }
        
        try
        {
            StatusText = "测试连接中...";
            
            var testConfig = new ProviderConfig
            {
                ProviderType = EditProviderType,
                ApiKey = EditApiKey,
                BaseUrl = EditBaseUrl,
                Model = EditModel,
                TimeoutSeconds = 30
            };
            
            var provider = ProviderFactory.Create(testConfig);
            var success = await provider.TestConnectionAsync();
            
            StatusText = success 
                ? LocalizationManager.Get("ConnectionSuccess") 
                : LocalizationManager.Get("ConnectionFailed");
        }
        catch (Exception ex)
        {
            StatusText = $"{LocalizationManager.Get("Error")}: {ex.Message}";
            LoggerService.Error("Connection test failed", ex);
        }
    }
    
    [RelayCommand]
    private void SetDefault(ProviderConfig? provider)
    {
        if (provider == null) return;
        
        var settings = _configService.Load();
        settings.DefaultProviderId = provider.Id;
        _configService.Save(settings);
        _configService.ClearCache();
        
        StatusText = $"默认 Provider 已设置为: {provider.Name}";
    }
}
