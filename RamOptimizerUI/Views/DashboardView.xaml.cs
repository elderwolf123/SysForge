using System.Windows.Threading;
using RamOptimizer.Monitoring;
using RamOptimizer.Logging;
using RamOptimizerUI.Services;

namespace RamOptimizerUI.Views
{
    public partial class DashboardView : UserControl
    {
        private DispatcherTimer _hardwareUpdateTimer;

        public DashboardView()
        {
            InitializeComponent();
            
            // Subscribe to events
            if (ServiceLocator.PerformanceMonitor != null)
            {
                ServiceLocator.PerformanceMonitor.PerformanceMetricsUpdated += OnPerformanceMetricsUpdated;
            }

            if (ServiceLocator.Logger != null)
            {
                ServiceLocator.Logger.LogEntryAdded += OnLogEntryAdded;
            }

            // Start hardware update timer
            _hardwareUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _hardwareUpdateTimer.Tick += (s, e) => UpdateHardwareStatus();
            _hardwareUpdateTimer.Start();

            // Initial update
            UpdateHardwareStatus();
        }

        private void OnPerformanceMetricsUpdated(object? sender, PerformanceMetrics metrics)
        {
            Dispatcher.Invoke(() =>
            {
                if (CpuUsageText != null) CpuUsageText.Text = $"{metrics.CpuUsage:F1}%";
                if (CpuUsageBar != null) CpuUsageBar.Value = metrics.CpuUsage;
                
                if (RamUsageText != null) RamUsageText.Text = $"{metrics.AvailableMemoryMB:F0} MB Available";
                
                if (DiskUsageText != null) DiskUsageText.Text = $"{metrics.DiskUsage:F1}%";
                if (DiskUsageBar != null) DiskUsageBar.Value = metrics.DiskUsage;
            });
        }

        private void OnLogEntryAdded(object? sender, LogEntry e)
        {
            Dispatcher.Invoke(() =>
            {
                if (LogListBox != null)
                {
                    LogListBox.Items.Insert(0, $"[{e.Timestamp:HH:mm:ss}] {e.Level}: {e.Message}");
                    if (LogListBox.Items.Count > 100)
                    {
                        LogListBox.Items.RemoveAt(100);
                    }
                }
            });
        }

        private void UpdateHardwareStatus()
        {
            try
            {
                if (ServiceLocator.HardwareMonitor != null)
                {
                    var cpuTemp = ServiceLocator.HardwareMonitor.GetCpuTemperature();
                    var cpuFan = ServiceLocator.HardwareMonitor.GetCpuFanSpeed();

                    Dispatcher.Invoke(() =>
                    {
                        if (CpuTempText != null) CpuTempText.Text = cpuTemp > 0 ? $"{cpuTemp:F0}°C" : "--°C";
                        if (CpuFanText != null) CpuFanText.Text = cpuFan > 0 ? $"{cpuFan}%" : "--%";
                    });
                }

                if (ServiceLocator.BatteryManager != null)
                {
                    var chargeLimit = ServiceLocator.BatteryManager.GetChargeLimit();
                    Dispatcher.Invoke(() =>
                    {
                        if (BatteryLimitText != null) BatteryLimitText.Text = $"{chargeLimit}%";
                    });
                }
            }
            catch
            {
                // Ignore errors during update
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
            ServiceLocator.Logger?.LogInfo("Logs cleared");
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _hardwareUpdateTimer?.Stop();
            if (ServiceLocator.PerformanceMonitor != null)
            {
                ServiceLocator.PerformanceMonitor.PerformanceMetricsUpdated -= OnPerformanceMetricsUpdated;
            }
            if (ServiceLocator.Logger != null)
            {
                ServiceLocator.Logger.LogEntryAdded -= OnLogEntryAdded;
            }
        }
    }
}
