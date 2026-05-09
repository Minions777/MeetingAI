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
    private readonly IConfigurationService _configService;

    public Action? RequestClose { get; set; }

    [ObservableProperty] private ObservableCollection<ProviderConfig> _providers = new();
    [ObservableProperty] private ProviderConfig? _selectedProvider;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusText = "";
    
    // 预设相关
    [ObservableProperty] private ObservableCollection<ProviderPreset> _availablePresets = new();
    [ObservableProperty] private ProviderPreset? _selectedPreset;
    [ObservableProperty] private ObservableCollection<string> _availableChatModels = new();
    [ObservableProperty] private ObservableCollection<string> _availableWhisperModels = new();
    [ObservableProperty] private bool _isCustomUrl;
    
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
    
    public SettingsViewModel(IConfigurationService configService)
    {
        _configService = configService;
        LoadPresets();
        LoadProviders();
    }
    
    /// <summary>
    /// 加载所有预设配置
    /// </summary>
    private void LoadPresets()
    {
        var presets = ProviderPreset.GetAll();
        AvailablePresets.Clear();
        foreach (var preset in presets)
        {
            AvailablePresets.Add(preset);
        }
    }
    
    /// <summary>
    /// 当预设选择变化时，更新 URL 和模型列表
    /// </summary>
    partial void OnSelectedPresetChanged(ProviderPreset? value)
    {
        if (value == null) return;
        
        EditBaseUrl = value.DefaultUrl;
        EditProviderType = value.ProviderType;
        
        // 更新可选模型列表
        AvailableChatModels.Clear();
        foreach (var model in value.ChatModels)
        {
            AvailableChatModels.Add(model);
        }
        
        AvailableWhisperModels.Clear();
        foreach (var model in value.WhisperModels)
        {
            AvailableWhisperModels.Add(model);
        }
        
        // 设置默认模型
        if (!string.IsNullOrEmpty(value.DefaultChatModel))
            EditModel = value.DefaultChatModel;
        
        if (!string.IsNullOrEmpty(value.DefaultWhisperModel))
            EditWhisperModel = value.DefaultWhisperModel;
        
        // Ollama 不需要 API Key
        IsCustomUrl = false;
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
        
        // 默认选择 OpenAI 预设
        var defaultPreset = AvailablePresets.FirstOrDefault(p => p.ProviderType == AIProviderType.OpenAI);
        SelectedPreset = defaultPreset;
        
        EditName = "新配置";
        EditProviderType = AIProviderType.OpenAI;
        EditApiKey = "";
        EditBaseUrl = defaultPreset?.DefaultUrl ?? "https://api.openai.com/v1";
        EditModel = defaultPreset?.DefaultChatModel ?? "gpt-4o-mini";
        EditWhisperModel = defaultPreset?.DefaultWhisperModel ?? "whisper-1";
        EditMaxTokens = 4096;
        EditTemperature = 0.7;
        EditIsEnabled = true;
        IsCustomUrl = false;
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
        
        // 根据 ProviderType 匹配预设
        var matchingPreset = AvailablePresets.FirstOrDefault(p => 
            p.ProviderType == provider.ProviderType && 
            p.DefaultUrl == provider.BaseUrl);
        
        if (matchingPreset != null)
        {
            SelectedPreset = matchingPreset;
            IsCustomUrl = false;
        }
        else
        {
            // URL 被自定义过，选择对应类型的预设但标记为自定义 URL
            SelectedPreset = AvailablePresets.FirstOrDefault(p => p.ProviderType == provider.ProviderType);
            IsCustomUrl = true;
        }
        
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
        // 表单验证
        if (string.IsNullOrWhiteSpace(EditName))
        {
            StatusText = "请输入配置名称";
            return;
        }
        
        // Ollama 等本地部署不需要 API Key
        var currentPreset = SelectedPreset;
        if (string.IsNullOrWhiteSpace(EditApiKey) && currentPreset?.RequiresApiKey != false)
        {
            StatusText = "请输入 API Key";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(EditBaseUrl))
        {
            StatusText = "请输入 Base URL";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(EditModel))
        {
            StatusText = "请输入模型名称";
            return;
        }
        
        if (EditMaxTokens < 1 || EditMaxTokens > 128000)
        {
            StatusText = "Max Tokens 必须在 1-128000 之间";
            return;
        }
        
        if (EditTemperature < 0 || EditTemperature > 2)
        {
            StatusText = "Temperature 必须在 0-2 之间";
            return;
        }
        
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
        IsCustomUrl = false;
    }
    
    /// <summary>
    /// 切换自定义 URL 模式
    /// </summary>
    [RelayCommand]
    private void ToggleCustomUrl()
    {
        IsCustomUrl = !IsCustomUrl;
        if (!IsCustomUrl && SelectedPreset != null)
        {
            // 恢复为预设默认 URL
            EditBaseUrl = SelectedPreset.DefaultUrl;
        }
    }
    
    /// <summary>
    /// 重置 URL 为当前预设的默认值
    /// </summary>
    [RelayCommand]
    private void ResetUrlToDefault()
    {
        if (SelectedPreset != null)
        {
            EditBaseUrl = SelectedPreset.DefaultUrl;
            IsCustomUrl = false;
        }
    }
    
    [RelayCommand]
    private void SaveAndClose()
    {
        if (IsEditing)
        {
            SaveProvider();
        }
        RequestClose?.Invoke();
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
