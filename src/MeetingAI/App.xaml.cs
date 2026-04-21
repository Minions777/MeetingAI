using System.Windows;

namespace MeetingAI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var configPath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "MeetingAI");
        System.IO.Directory.CreateDirectory(configPath);
    }
}