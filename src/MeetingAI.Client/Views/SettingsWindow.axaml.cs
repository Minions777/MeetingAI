using Avalonia.Controls;
using Avalonia.Interactivity;
using MeetingAI.Client.ViewModels;
using MeetingAI.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingAI.Client.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
        : this(App.Services.GetRequiredService<SettingsViewModel>())
    {
    }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose = () =>
        {
            Close(true);
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is IDisposable disposable)
            disposable.Dispose();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void ProviderList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is ListBox listBox && listBox.SelectedItem is ProviderConfig config)
        {
            vm.Provider.EditProviderCommand.Execute(config);
            if (ApiKeyBox != null)
                ApiKeyBox.Text = vm.Provider.EditApiKey;
        }
    }
}
