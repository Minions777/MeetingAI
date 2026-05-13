using CommunityToolkit.Mvvm.ComponentModel;

namespace MeetingAI.Client.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public ProviderManagementViewModel Provider { get; }

    public Action? RequestClose { get; set; }

    public string StatusText => Provider.StatusText;

    public SettingsViewModel(ProviderManagementViewModel provider)
    {
        Provider = provider;
        Provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Provider.StatusText))
                OnPropertyChanged(nameof(StatusText));
        };
    }

    public System.Collections.ObjectModel.ObservableCollection<MeetingAI.Shared.Configuration.ProviderConfig> Providers => Provider.Providers;
    public MeetingAI.Shared.Configuration.ProviderConfig? SelectedProvider => Provider.SelectedProvider;
    public bool IsEditing => Provider.IsEditing;
    public System.Collections.ObjectModel.ObservableCollection<MeetingAI.Shared.Configuration.ProviderPreset> AvailablePresets => Provider.AvailablePresets;
    public MeetingAI.Shared.Configuration.ProviderPreset? SelectedPreset => Provider.SelectedPreset;
    public System.Collections.ObjectModel.ObservableCollection<string> AvailableChatModels => Provider.AvailableChatModels;
    public System.Collections.ObjectModel.ObservableCollection<string> AvailableWhisperModels => Provider.AvailableWhisperModels;
    public bool IsCustomUrl => Provider.IsCustomUrl;
    public string EditName => Provider.EditName;
    public MeetingAI.Shared.Configuration.AIProviderType EditProviderType => Provider.EditProviderType;
    public string EditApiKey
    {
        get => Provider.EditApiKey;
        set => Provider.EditApiKey = value;
    }
    public string EditBaseUrl => Provider.EditBaseUrl;
    public string EditModel => Provider.EditModel;
    public string EditWhisperModel => Provider.EditWhisperModel;
    public int EditMaxTokens => Provider.EditMaxTokens;
    public double EditTemperature => Provider.EditTemperature;
    public bool EditIsEnabled => Provider.EditIsEnabled;
    public System.Collections.Generic.IReadOnlyList<MeetingAI.Shared.Configuration.AIProviderType> AvailableProviderTypes => Provider.AvailableProviderTypes;

    public CommunityToolkit.Mvvm.Input.IRelayCommand<MeetingAI.Shared.Configuration.ProviderConfig?> EditProviderCommand => Provider.EditProviderCommand;

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void SaveAndClose()
    {
        RequestClose?.Invoke();
    }
}