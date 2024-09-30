using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Scopa.Editor; // Make sure to add reference to System.Windows.Forms
using Application = System.Windows.Application;

namespace TrayApp
{
    public partial class MainWindow : Window
    {
        private NotifyIcon trayIcon;
        private FileSystemWatcher watcher;
        private ToolStripMenuItem enableMenuItem;
        private string directoryToWatch = @"C:\Users\thele\Documents\Repos\cyberjunk\Assets\Maps"; // Default directory
        private string fileExtension = "*.map"; // Default extension to monitor

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
                Text = "File Monitor",
                Icon = SystemIcons.Application, // You can replace this with a custom icon
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

            // "Set Directory" menu item
            trayMenu.Items.Add("Set Directory", null, OnSetDirectory);

            // "Exit" menu item
            trayMenu.Items.Add("Exit", null, OnExit);

            // Attach the menu to the tray icon
            trayIcon.ContextMenuStrip = trayMenu;
        }

        private void InitializeFileSystemWatcher()
        {
            watcher = new FileSystemWatcher
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter = fileExtension,
                EnableRaisingEvents = false
            };
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            watcher.Deleted += OnFileDeleted;
        }

        // Event handler for toggling monitoring
        private void OnToggleEnabled(object sender, EventArgs e)
        {
            enableMenuItem.Checked = !enableMenuItem.Checked;
            watcher.EnableRaisingEvents = enableMenuItem.Checked;

            if (enableMenuItem.Checked)
            {
                watcher.Path = directoryToWatch;
                System.Windows.MessageBox.Show($"Monitoring enabled for: {directoryToWatch}", "File Monitor");
            }
            else
            {
                System.Windows.MessageBox.Show("Monitoring disabled.", "File Monitor");
            }
        }

        // Event handler for setting directory
        private void OnSetDirectory(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    directoryToWatch = folderDialog.SelectedPath;
                    watcher.Path = directoryToWatch;
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
            // Temporarily disable the watcher
            watcher.EnableRaisingEvents = false;

            try
            {
                var path = e.FullPath;

                if (fileExtension.Equals(Path.GetExtension(path)))
                {
                    UniqueNamePreprocessor.Parse(path); // This may modify the file
                    LogChange("Modified map: ", e.FullPath);
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
            LogChange("Renamed", e.FullPath);
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            LogChange("Deleted", e.FullPath);
        }

        // Method to log file changes
        private void LogChange(string changeType, string filePath)
        {
            System.Windows.MessageBox.Show($"{changeType}: {filePath}", "File Monitor");
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
