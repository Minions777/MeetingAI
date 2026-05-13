using Avalonia.Controls;
using Avalonia.Interactivity;
using MeetingAI.Client.ViewModels;
using MeetingAI.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingAI.Client.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SettingsViewModel>();

        if (DataContext is SettingsViewModel vm)
        {
            vm.RequestClose = () =>
            {
                Close(true);
            };
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void ProviderList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is ListBox listBox && listBox.SelectedItem is ProviderConfig config)
        {
            vm.EditProviderCommand.Execute(config);
            if (ApiKeyBox != null)
                ApiKeyBox.Text = vm.EditApiKey;
        }
    }
}
