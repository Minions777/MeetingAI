using System.Windows;
using MeetingAI.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingAI.Client.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SettingsViewModel>();
    }
}
