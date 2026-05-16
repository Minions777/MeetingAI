using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MeetingAI.Client.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly PropertyChangedEventHandler _providerStatusHandler;
    private bool _disposed;

    public ProviderManagementViewModel Provider { get; }

    public Action? RequestClose { get; set; }

    public string StatusText => Provider.StatusText;

    public SettingsViewModel(ProviderManagementViewModel provider)
    {
        Provider = provider;
        _providerStatusHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(Provider.StatusText))
                OnPropertyChanged(nameof(StatusText));
        };
        Provider.PropertyChanged += _providerStatusHandler;
    }

    [RelayCommand]
    private void SaveAndClose()
    {
        RequestClose?.Invoke();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Provider.PropertyChanged -= _providerStatusHandler;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}