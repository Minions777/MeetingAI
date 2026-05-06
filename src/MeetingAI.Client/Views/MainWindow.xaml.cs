using System.Windows;
using MeetingAI.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingAI.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }
}
