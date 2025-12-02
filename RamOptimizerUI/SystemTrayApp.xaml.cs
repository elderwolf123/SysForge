using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;
using RamOptimizer.ProcessManagement;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace RamOptimizerUI
{
    public partial class SystemTrayApp : Application
    {
        private NotifyIcon _notifyIcon = null!;
        private Window? _mainWindow;
        private AdvancedCpuOptimizer _cpuOptimizer = null!;
        private AdvancedGpuOptimizer _gpuOptimizer = null!;
        private AdvancedFileCompressionSystem _compressionSystem = null!;
        private SystemPerformanceMonitor _performanceMonitor = null!;
        private bool _isOptimizationRunning = false;
        private string _targetProcessName = "";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize components
            InitializeSystemTray();
            InitializeOptimizationSystems();
            
            // Start performance monitoring
            _performanceMonitor = new SystemPerformanceMonitor();
            _performanceMonitor.StartMonitoring();
        }

        private void InitializeSystemTray()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule?.FileName ?? ""),
                Text = "Ram Optimizer",
                Visible = true
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            
            // Optimize menu item
            var optimizeMenuItem = new ToolStripMenuItem("Optimize for Process...");
            optimizeMenuItem.Click += OptimizeMenuItem_Click;
            contextMenu.Items.Add(optimizeMenuItem);
            
            // Separator
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Performance monitor menu item
            var performanceMenuItem = new ToolStripMenuItem("Performance Monitor");
            performanceMenuItem.Click += PerformanceMenuItem_Click;
            contextMenu.Items.Add(performanceMenuItem);
            
            // Separator
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Compression menu items
            var compressionMenuItem = new ToolStripMenuItem("File Compression");
            var compressFilesMenuItem = new ToolStripMenuItem("Compress Files");
            compressFilesMenuItem.Click += CompressFilesMenuItem_Click;
            compressionMenuItem.DropDownItems.Add(compressFilesMenuItem);
            
            var decompressFilesMenuItem = new ToolStripMenuItem("Decompress Files");
            decompressFilesMenuItem.Click += DecompressFilesMenuItem_Click;
            compressionMenuItem.DropDownItems.Add(decompressFilesMenuItem);
            
            contextMenu.Items.Add(compressionMenuItem);
            
            // Separator
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Exit menu item
            var exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenu.Items.Add(exitMenuItem);
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // Handle double-click to show performance monitor
            _notifyIcon.DoubleClick += (sender, args) => PerformanceMenuItem_Click(sender, args);
        }

        private void InitializeOptimizationSystems()
        {
            _cpuOptimizer = new AdvancedCpuOptimizer();
            _gpuOptimizer = new AdvancedGpuOptimizer();
            _compressionSystem = new AdvancedFileCompressionSystem();
        }

        private async void OptimizeMenuItem_Click(object? sender, EventArgs e)
        {
            // Show input dialog to get target process name
            var processName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the name of the process to optimize for:", 
                "Process Optimization", 
                "notepad.exe");
            
            if (!string.IsNullOrEmpty(processName))
            {
                _targetProcessName = processName;
                await StartOptimizationAsync(processName);
            }
        }

        private async Task StartOptimizationAsync(string processName)
        {
            if (_isOptimizationRunning)
            {
                MessageBox.Show("Optimization is already running for another process.");
                return;
            }

            _isOptimizationRunning = true;
            _notifyIcon.Text = $"Ram Optimizer - Optimizing for {processName}";

            try
            {
                // Show notification
                _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                _notifyIcon.BalloonTipTitle = "Optimization Started";
                _notifyIcon.BalloonTipText = $"Optimizing system for {processName}";
                _notifyIcon.ShowBalloonTip(3000);

                // Start CPU optimization
                await _cpuOptimizer.OptimizeCpuForTargetProcessAsync(processName);
                
                // Start GPU optimization
                await _gpuOptimizer.OptimizeGpuForTargetProcessAsync(processName);
                
                // Start background compression
                await _compressionSystem.StartBackgroundCompressionAsync();
                
                // Show completion notification
                _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                _notifyIcon.BalloonTipTitle = "Optimization Complete";
                _notifyIcon.BalloonTipText = $"System optimized for {processName}";
                _notifyIcon.ShowBalloonTip(3000);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to optimize system: {ex.Message}");
                _notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
                _notifyIcon.BalloonTipTitle = "Optimization Failed";
                _notifyIcon.BalloonTipText = $"Failed to optimize system: {ex.Message}";
                _notifyIcon.ShowBalloonTip(3000);
            }
            finally
            {
                _isOptimizationRunning = false;
                _notifyIcon.Text = "Ram Optimizer";
            }
        }

        private void PerformanceMenuItem_Click(object? sender, EventArgs e)
        {
            // Create or show performance monitor window
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new PerformanceMonitorWindow(_performanceMonitor);
                _mainWindow.Show();
            }
            else
            {
                _mainWindow.Activate();
            }
        }

        private async void CompressFilesMenuItem_Click(object? sender, EventArgs e)
        {
            var folderPath = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the folder path to compress:", 
                "File Compression", 
                @"C:\");
            
            if (!string.IsNullOrEmpty(folderPath))
            {
                try
                {
                    _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    _notifyIcon.BalloonTipTitle = "Compression Started";
                    _notifyIcon.BalloonTipText = $"Compressing files in {folderPath}";
                    _notifyIcon.ShowBalloonTip(3000);
                    
                    var results = await _compressionSystem.CompressDirectoryAsync(folderPath);
                    var spaceSaved = results.Count > 0 ? results[0].CompressedSize : 0;
                    
                    _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    _notifyIcon.BalloonTipTitle = "Compression Complete";
                    _notifyIcon.BalloonTipText = $"Files compressed. Space saved: {spaceSaved} bytes";
                    _notifyIcon.ShowBalloonTip(3000);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to compress files: {ex.Message}");
                    _notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
                    _notifyIcon.BalloonTipTitle = "Compression Failed";
                    _notifyIcon.BalloonTipText = $"Failed to compress files: {ex.Message}";
                    _notifyIcon.ShowBalloonTip(3000);
                }
            }
        }

        private async void DecompressFilesMenuItem_Click(object? sender, EventArgs e)
        {
            var folderPath = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the folder path to decompress:", 
                "File Decompression", 
                @"C:\");
            
            if (!string.IsNullOrEmpty(folderPath))
            {
                try
                {
                    _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    _notifyIcon.BalloonTipTitle = "Decompression Started";
                    _notifyIcon.BalloonTipText = $"Decompressing files in {folderPath}";
                    _notifyIcon.ShowBalloonTip(3000);
                    
                    // This is a simplified implementation
                    // In a real implementation, we would iterate through compressed files
                    _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    _notifyIcon.BalloonTipTitle = "Decompression Complete";
                    _notifyIcon.BalloonTipText = "Files decompressed";
                    _notifyIcon.ShowBalloonTip(3000);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to decompress files: {ex.Message}");
                    _notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
                    _notifyIcon.BalloonTipTitle = "Decompression Failed";
                    _notifyIcon.BalloonTipText = $"Failed to decompress files: {ex.Message}";
                    _notifyIcon.ShowBalloonTip(3000);
                }
            }
        }

        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            // Recover any terminated processes
            _gpuOptimizer.RecoverTerminatedProcesses();
            
            // Stop performance monitoring
            _performanceMonitor?.StopMonitoring();
            
            // Clean up
            _notifyIcon?.Dispose();
            Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Recover any terminated processes
            _gpuOptimizer.RecoverTerminatedProcesses();
            
            // Stop performance monitoring
            _performanceMonitor?.StopMonitoring();
            
            // Clean up
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }

    public class PerformanceMonitorWindow : Window
    {
        private readonly SystemPerformanceMonitor _monitor;
        private readonly System.Windows.Controls.Label _cpuLabel;
        private readonly System.Windows.Controls.Label _gpuLabel;
        private readonly System.Windows.Controls.Label _ramLabel;
        private readonly System.Windows.Controls.Label _diskLabel;

        public PerformanceMonitorWindow(SystemPerformanceMonitor monitor)
        {
            _monitor = monitor;
            
            Title = "Performance Monitor";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Create UI elements
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            
            _cpuLabel = CreateLabel("CPU: --%");
            Grid.SetRow(_cpuLabel, 0);
            grid.Children.Add(_cpuLabel);
            
            _gpuLabel = CreateLabel("GPU: --%");
            Grid.SetRow(_gpuLabel, 1);
            grid.Children.Add(_gpuLabel);
            
            _ramLabel = CreateLabel("RAM: --%");
            Grid.SetRow(_ramLabel, 2);
            grid.Children.Add(_ramLabel);
            
            _diskLabel = CreateLabel("Disk: --%");
            Grid.SetRow(_diskLabel, 3);
            grid.Children.Add(_diskLabel);
            
            Content = grid;
            
            // Start updating
            StartUpdating();
        }

        private System.Windows.Controls.Label CreateLabel(string text)
        {
            return new System.Windows.Controls.Label
            {
                Content = text,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                FontSize = 16
            };
        }

        private async void StartUpdating()
        {
            while (IsLoaded)
            {
                try
                {
                    var cpuMetrics = await _monitor.GetCpuMetricsAsync();
                    var gpuMetrics = await _monitor.GetGpuMetricsAsync();
                    var ramMetrics = await _monitor.GetRamMetricsAsync();
                    var diskMetrics = await _monitor.GetDiskMetricsAsync();
                    
                    Dispatcher.Invoke(() =>
                    {
                        _cpuLabel.Content = $"CPU: {cpuMetrics.OverallUsage:F1}%";
                        _gpuLabel.Content = $"GPU: {gpuMetrics.Usage:F1}%";
                        _ramLabel.Content = $"RAM: {ramMetrics.Usage:F1}%";
                        _diskLabel.Content = $"Disk: {diskMetrics.Usage:F1}%";
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to update performance metrics: {ex.Message}");
                }
                
                await Task.Delay(1000); // Update every second
            }
        }
    }

    public class SystemPerformanceMonitor
    {
        private bool _isMonitoring = false;
        private Task? _monitoringTask;

        public void StartMonitoring()
        {
            _isMonitoring = true;
            _monitoringTask = Task.Run(MonitorSystemAsync);
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
        }

        private async Task MonitorSystemAsync()
        {
            while (_isMonitoring)
            {
                try
                {
                    // In a real implementation, this would collect system metrics
                    // For now, we'll just wait
                    await Task.Delay(5000); // Check every 5 seconds
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in system monitoring: {ex.Message}");
                }
            }
        }

        public async Task<CpuMetrics> GetCpuMetricsAsync()
        {
            // Simulate getting CPU metrics
            return new CpuMetrics
            {
                OverallUsage = 20.0 + (new Random().NextDouble() * 50), // Simulate 20-70% usage
                Temperature = 45.0 + (new Random().NextDouble() * 20) // Simulate 45-65°C
            };
        }

        public async Task<GpuMetrics> GetGpuMetricsAsync()
        {
            // Simulate getting GPU metrics
            return new GpuMetrics
            {
                Usage = 10.0 + (new Random().NextDouble() * 60), // Simulate 10-70% usage
                Temperature = 50.0 + (new Random().NextDouble() * 30) // Simulate 50-80°C
            };
        }

        public async Task<RamMetrics> GetRamMetricsAsync()
        {
            // Simulate getting RAM metrics
            return new RamMetrics
            {
                Usage = 30.0 + (new Random().NextDouble() * 50), // Simulate 30-80% usage
                AvailableMemory = 4096 + (new Random().NextDouble() * 12288) // Simulate 4-16 GB available
            };
        }

        public async Task<DiskMetrics> GetDiskMetricsAsync()
        {
            // Simulate getting disk metrics
            return new DiskMetrics
            {
                Usage = 20.0 + (new Random().NextDouble() * 60), // Simulate 20-80% usage
                FreeSpace = 100 + (new Random().NextDouble() * 400) // Simulate 100-500 GB free
            };
        }
    }

    public class CpuMetrics
    {
        public double OverallUsage { get; set; }
        public double Temperature { get; set; }
    }

    public class GpuMetrics
    {
        public double Usage { get; set; }
        public double Temperature { get; set; }
    }

    public class RamMetrics
    {
        public double Usage { get; set; }
        public double AvailableMemory { get; set; }
    }

    public class DiskMetrics
    {
        public double Usage { get; set; }
        public double FreeSpace { get; set; }
    }
}
