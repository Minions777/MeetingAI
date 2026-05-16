using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Client.ViewModels;

public partial class ProviderViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;

    [ObservableProperty] private ObservableCollection<ProviderConfig> _providers = new();
    [ObservableProperty] private ProviderConfig? _selectedProvider;

    public ProviderViewModel(IConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task LoadProvidersAsync()
    {
        await Task.Run(() =>
        {
            var settings = _configService.Load();

            Dispatcher.UIThread.Post(() =>
            {
                Providers.Clear();
                foreach (var provider in settings.Providers.Where(p => p.IsEnabled))
                    Providers.Add(provider);

                SelectedProvider = Providers.FirstOrDefault(p => p.Id == settings.DefaultProviderId)
                                  ?? Providers.FirstOrDefault();
            });
        });
    }

    [RelayCommand]
    public async Task ReloadProvidersAsync()
    {
        await LoadProvidersAsync();
        LoggerService.Info("Provider list refreshed");
    }

    [RelayCommand]
    private async Task SetAsDefaultAsync()
    {
        var provider = SelectedProvider;
        if (provider == null) return;

        await Task.Run(() =>
        {
            var settings = _configService.Load();
            settings.DefaultProviderId = provider.Id;
            _configService.Save(settings);
        });

        LoggerService.Info($"Default provider set to: {provider.Name}");
    }
}