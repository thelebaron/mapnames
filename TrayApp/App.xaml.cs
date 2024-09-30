using System.Configuration;
using System.Data;
using System.Windows;

namespace TrayApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Start the application without showing any window
        //new MainWindow();
    }
}