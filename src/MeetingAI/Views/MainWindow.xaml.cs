using System.Windows;
using MeetingAI.ViewModels;

namespace MeetingAI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Closing += (s, e) => (DataContext as MainViewModel)?.Dispose();
    }
}