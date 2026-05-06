using System.Windows;
using System.Windows.Controls;
using MeetingAI.Client.ViewModels;

namespace MeetingAI.Client.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        // No minimize for modal dialog
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        // No maximize for modal dialog
    }

    private void ProviderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is ListBox listBox && listBox.SelectedItem is ProviderConfig config)
        {
            vm.EditProviderCommand.Execute(config);
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
