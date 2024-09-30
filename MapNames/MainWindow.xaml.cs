using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Scopa.Editor; // Make sure to add reference to System.Windows.Forms
using Application = System.Windows.Application;

namespace MapNames
{
    public partial class MainWindow : Window
    {
        private NotifyIcon        trayIcon;
        private FileSystemWatcher watcher;
        private ToolStripMenuItem enableMenuItem;
        private string            directoryToWatch = @"C:\Users\thele\Documents\Repos\cyberjunk\Assets\Maps"; // Default directory
        private string            fileWildcard     = "*";
        private string            fileExtension    = ".map"; // Default extension to monitor
        private ToolStripMenuItem setDirectoryMenuItem;      // Define a menu item to set the directory

        public MainWindow()
        {
            // Initialize the tray icon without showing the window
            InitializeComponent();
            HideWindow();

            // Initialize the tray icon and context menu
            InitializeTrayIcon();

            // Initialize FileSystemWatcher but do not start it yet
            InitializeFileSystemWatcher();
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Text    = "File Monitor",
                //Icon    = SystemIcons.Application, // You can replace this with a custom icon
                Icon    = new Icon("EntityPrefab.ico"),  // Load your custom icon file
                Visible = true
            };

            // Create the context menu for the tray icon
            ContextMenuStrip trayMenu = new ContextMenuStrip();

            // "Enable" menu item (to toggle monitoring)
            enableMenuItem = new ToolStripMenuItem("Enabled", null, OnToggleEnabled)
            {
                Checked = false // Initially disabled
            };
            trayMenu.Items.Add(enableMenuItem);

            // Initialize the Set Directory menu item
            setDirectoryMenuItem = new ToolStripMenuItem($"Set Directory: {directoryToWatch}", null, OnSetDirectory);
            trayMenu.Items.Add(setDirectoryMenuItem);

            // "Exit" menu item
            trayMenu.Items.Add("Exit", null, OnExit);

            // Attach the menu to the tray icon
            trayIcon.ContextMenuStrip = trayMenu;

            //System.Windows.MessageBox.Show($"Directory set to: {directoryToWatch}", "File Monitor");
            //LogChange2("Watching: ", directoryToWatch);
        }

        private void InitializeFileSystemWatcher()
        {
            watcher = new FileSystemWatcher
            {
                NotifyFilter        = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter              = fileWildcard + fileExtension,
                EnableRaisingEvents = false
            };
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            watcher.Deleted += OnFileDeleted;

            watcher.Path = directoryToWatch; // Set the watcher path
        }

        // Event handler for toggling monitoring
        private void OnToggleEnabled(object sender, EventArgs e)
        {
            // Check if the directory to watch is valid before enabling
            if (string.IsNullOrWhiteSpace(directoryToWatch) || !Directory.Exists(directoryToWatch))
            {
                System.Windows.MessageBox.Show("Please set a valid directory before enabling monitoring.", "Invalid Directory: " + directoryToWatch, MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                enableMenuItem.Checked = false; // Ensure it stays unchecked
                return;                         // Exit the method to prevent enabling the watcher
            }

            enableMenuItem.Checked      = !enableMenuItem.Checked;
            watcher.EnableRaisingEvents = enableMenuItem.Checked;

            if (enableMenuItem.Checked)
            {
                watcher.Path = directoryToWatch;
                LogChange2("Watching: ", directoryToWatch);
                //System.Windows.MessageBox.Show($"Monitoring enabled for: {directoryToWatch}", "File Monitor");
            }
            else
            {
                //System.Windows.MessageBox.Show("Monitoring disabled.", "File Monitor");
            }
        }

        // Event handler for setting directory
        private void OnSetDirectory(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    directoryToWatch          = folderDialog.SelectedPath;            // Update the path
                    watcher.Path              = directoryToWatch;                     // Set the watcher path
                    setDirectoryMenuItem.Text = $"Set Directory: {directoryToWatch}"; // Update menu item text
                    System.Windows.MessageBox.Show($"Directory set to: {directoryToWatch}", "File Monitor");
                }
            }
        }

        // Event handler for exiting the application
        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Current.Shutdown();
        }

        // FileSystemWatcher event handlers
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            var path = e.FullPath;
            // Temporarily disable the watcher
            watcher.EnableRaisingEvents = false;

            try
            {
                if (fileExtension.Equals(Path.GetExtension(path)))
                {
                    UniqueNamePreprocessor.Parse(path); // This may modify the file
                    UniqueNamePreprocessor.RenameDuplicateUnityNames(path);
                    LogChange2("Modified map: ", e.FullPath);
                }
            }
            finally
            {
                // Re-enable the watcher after processing
                watcher.EnableRaisingEvents = true;
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            LogChange2("Renamed", e.FullPath);
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            LogChange2("Deleted", e.FullPath);
        }

        // Method to log file changes
        private void LogChange(string changeType, string filePath)
        {
            System.Windows.MessageBox.Show($"{changeType}: {filePath}", "File Monitor");
        }

        // Method to log file changes and send notifications

        // Method to log file changes and send notifications
        private void LogChange2(string changeType, string filePath)
        {
            new ToastContentBuilder()
                .SetToastDuration(ToastDuration.Short)
                .AddText(changeType)
                .AddText(filePath)
                .AddAudio(new ToastAudio{Silent = true})
                .Show(); // Not seeing the Show() method? Make sure you have version 7.0,
            
            /*
            // Create the toast notification from the XML
            var toastNotification = new ToastNotification(toastContent.GetXml());
            // Creates a silent audio element

            // Show the toast notification
            ToastNotificationManager.CreateToastNotifier().Show(toastNotification);*/
        }
    


    // Method to hide the window
        private void HideWindow()
        {
            this.ShowInTaskbar = false;
            this.WindowState = WindowState.Minimized;
            this.Hide();
        }
    }
}
