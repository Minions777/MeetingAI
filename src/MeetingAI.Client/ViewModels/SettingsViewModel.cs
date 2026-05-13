using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

    [RelayCommand]
    private void SaveAndClose()
    {
        RequestClose?.Invoke();
    }
}