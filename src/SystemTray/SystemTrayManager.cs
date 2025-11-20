using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace RamOptimizerUI
{
    public class SystemTrayManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private bool _disposed = false;

        public event EventHandler OptimizationRequested;
        public event EventHandler ExitRequested;

        public SystemTrayManager()
        {
            InitializeSystemTray();
        }

        private void InitializeSystemTray()
        {
            try
            {
                // Create context menu
                _contextMenu = new ContextMenuStrip();

                // Add menu items
                var optimizeItem = new ToolStripMenuItem("Optimize Now");
                optimizeItem.Click += (sender, e) => OptimizationRequested?.Invoke(this, EventArgs.Empty);

                var separator1 = new ToolStripSeparator();

                var processManagementItem = new ToolStripMenuItem("Process Management");
                processManagementItem.Click += (sender, e) => OptimizationRequested?.Invoke(this, EventArgs.Empty);

                var cpuOptimizationItem = new ToolStripMenuItem("CPU Optimization");
                cpuOptimizationItem.Click += (sender, e) => OptimizationRequested?.Invoke(this, EventArgs.Empty);

                var gpuOptimizationItem = new ToolStripMenuItem("GPU Optimization");
                gpuOptimizationItem.Click += (sender, e) => OptimizationRequested?.Invoke(this, EventArgs.Empty);

                var fileCompressionItem = new ToolStripMenuItem("File Compression");
                fileCompressionItem.Click += (sender, e) => OptimizationRequested?.Invoke(this, EventArgs.Empty);

                var separator2 = new ToolStripSeparator();

                var exitItem = new ToolStripMenuItem("Exit");
                exitItem.Click += (sender, e) => ExitRequested?.Invoke(this, EventArgs.Empty);

                // Add items to menu
                _contextMenu.Items.Add(optimizeItem);
                _contextMenu.Items.Add(separator1);
                _contextMenu.Items.Add(processManagementItem);
                _contextMenu.Items.Add(cpuOptimizationItem);
                _contextMenu.Items.Add(gpuOptimizationItem);
                _contextMenu.Items.Add(fileCompressionItem);
                _contextMenu.Items.Add(separator2);
                _contextMenu.Items.Add(exitItem);

                // Create notify icon
                _notifyIcon = new NotifyIcon
                {
                    // Use the application's icon
                    Icon = Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly()?.Location ?? "RamOptimizerUI.exe"),
                    ContextMenuStrip = _contextMenu,
                    Visible = true,
                    Text = "RAM Optimizer Pro"
                };

                // Handle double click to show optimization window
                _notifyIcon.DoubleClick += (sender, e) => OptimizationRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // If system tray is not available, we'll just not show it
                System.Diagnostics.Debug.WriteLine($"Failed to initialize system tray: {ex.Message}");
            }
        }

        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try
            {
                _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
            }
        }

        public void UpdateText(string text)
        {
            try
            {
                _notifyIcon.Text = text.Length > 63 ? text.Substring(0, 63) : text; // Max 63 chars
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update system tray text: {ex.Message}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _notifyIcon?.Dispose();
                    _contextMenu?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}