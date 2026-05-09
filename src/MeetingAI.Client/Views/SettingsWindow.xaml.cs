using System.Windows;
using System.Windows.Controls;
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

        if (DataContext is ViewModels.SettingsViewModel vm)
        {
            vm.RequestClose = () =>
            {
                DialogResult = true;
                Close();
            };
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ProviderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is ListBox listBox && listBox.SelectedItem is ProviderConfig config)
        {
            vm.EditProviderCommand.Execute(config);
            ApiKeyBox.Password = vm.EditApiKey;
        }
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.EditApiKey = ApiKeyBox.Password;
        }
    }
}
