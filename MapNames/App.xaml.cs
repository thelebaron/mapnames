using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace MapNames;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create and show the main window (which will hide itself and show tray icon)
        var mainWindow = new MainWindow();
        mainWindow.Show(); // This will be hidden immediately by the MainWindow constructor
    }


}