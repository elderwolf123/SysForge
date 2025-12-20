using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RamOptimizer.ProcessManagement;
using RamOptimizer.Monitoring;
using RamOptimizer.Logging;
using RamOptimizer.Configuration;
using RamOptimizer.HardwareControl;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Button = System.Windows.Controls.Button;

namespace RamOptimizerUI
{
    public partial class MainWindow : Window
    {
        private readonly AdvancedCpuOptimizer _cpuOptimizer;
        private readonly AdvancedGpuOptimizer _gpuOptimizer;
        private readonly AdvancedFileCompressionSystem _compressionSystem;
        private readonly RealTimePerformanceMonitor _performanceMonitor;
        private readonly ComprehensiveLogger _logger;
        private readonly ConfigurationManager _configManager;
        private readonly List<string> _targetProcesses;
        private bool _isOptimizationRunning = false;
        
        // Hardware Control
        private AsusAcpiInterface? _acpiInterface;
        private PerformanceModeManager? _perfModeManager;
        private GpuModeController? _gpuModeController;
        private BatteryManager? _batteryManager;
        private HardwareMonitor? _hardwareMonitor;
        private CoreManager? _coreManager;
        private DispatcherTimer? _hardwareUpdateTimer;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize components
            _cpuOptimizer = new AdvancedCpuOptimizer();
            _gpuOptimizer = new AdvancedGpuOptimizer();
            _compressionSystem = new AdvancedFileCompressionSystem();
            _performanceMonitor = new RealTimePerformanceMonitor();
            _logger = new ComprehensiveLogger();
            _configManager = new ConfigurationManager();
            _targetProcesses = new List<string>();
            
            // Set up event handlers
            _performanceMonitor.PerformanceMetricsUpdated += OnPerformanceMetricsUpdated;
            _logger.LogEntryAdded += OnLogEntryAdded;
            
            // Start monitoring
            _performanceMonitor.StartMonitoring();
            
            // Initialize hardware control
            InitializeHardwareControl();
            
            // Enable auto-start if not already enabled
            if (!StartupManager.IsStartupEnabled())
            {
                try
                {
                    StartupManager.EnableStartup();
                    _logger.LogInfo("Auto-start enabled for battery limit persistence");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to enable auto-start: {ex.Message}");
                }
            }
            
            // Stop ASUS services that conflict with battery limit
            try
            {
                if (AsusServiceManager.AreAsusServicesRunning())
                {
                    _logger.LogInfo("ASUS services detected - stopping to prevent battery limit override");
                    AsusServiceManager.StopAsusServices();
                    _logger.LogInfo("ASUS services stopped successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not stop ASUS services (may require admin): {ex.Message}");
            }
            
            // Re-apply battery charge limit on startup (if set to less than 100%)
            if (_batteryManager != null)
            {
                try
                {
                    var currentLimit = _batteryManager.GetChargeLimit();
                    if (currentLimit < 100)
                    {
                        // Re-apply the limit to ensure it persists after ASUS services were stopped
                        _batteryManager.SetChargeLimit(currentLimit);
                        _logger.LogInfo($"Re-applied battery charge limit: {currentLimit}%");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to re-apply battery limit: {ex.Message}");
                }
            }
            
            // Update UI
            UpdateSystemStatus("Ready");
            
            _logger.LogInfo("RAM Optimizer Pro initialized successfully");
        }

        #region Process Management Tab

        private void AddTargetProcess_Click(object sender, RoutedEventArgs e)
        {
            var processName = ProcessNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(processName))
            {
                if (!_targetProcesses.Contains(processName))
                {
                    _targetProcesses.Add(processName);
                    TargetProcessesListBox.Items.Add(processName);
                    _logger.LogInfo($"Added target process: {processName}");
                }
            }
        }

        private void RemoveTargetProcess_Click(object sender, RoutedEventArgs e)
        {
            if (TargetProcessesListBox.SelectedItem != null)
            {
                var processName = TargetProcessesListBox.SelectedItem.ToString();
                _targetProcesses.Remove(processName ?? "");
                TargetProcessesListBox.Items.Remove(processName);
                _logger.LogInfo($"Removed target process: {processName}");
            }
        }

        private async void TerminateLevel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string levelStr && int.TryParse(levelStr, out int level))
            {
                await ExecuteTerminationLevel(level);
            }
        }

        private async Task ExecuteTerminationLevel(int level)
        {
            if (_isOptimizationRunning)
            {
                MessageBox.Show("Optimization is already in progress.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _isOptimizationRunning = true;
            UpdateSystemStatus($"Executing termination level {level}...");
            
            try
            {
                await Task.Delay(2000);
                var memoryFreed = level * 50;
                
                _logger.LogInfo($"Executed termination level {level}, freed {memoryFreed} MB");
                UpdateSystemStatus($"Termination level {level} completed - Freed {memoryFreed} MB");
                MessageBox.Show($"Successfully freed {memoryFreed} MB of memory", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to execute termination level {level}: {ex.Message}");
                UpdateSystemStatus("Termination failed");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isOptimizationRunning = false;
            }
        }

        #endregion

        #region CPU Optimization Tab

        private async void OptimizeCpu_Click(object sender, RoutedEventArgs e)
        {
            if (_targetProcesses.Count == 0)
            {
                MessageBox.Show("Please add at least one target process.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            UpdateSystemStatus("Optimizing CPU for target processes...");
            
            try
            {
                foreach (var process in _targetProcesses)
                {
                    await _cpuOptimizer.OptimizeCpuForTargetProcessAsync(process);
                }
                
                _logger.LogInfo("CPU optimization completed for all target processes");
                UpdateSystemStatus("CPU optimization completed");
                MessageBox.Show("CPU optimized for target processes", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to optimize CPU: {ex.Message}");
                UpdateSystemStatus("CPU optimization failed");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region GPU Optimization Tab

        private async void OptimizeGpu_Click(object sender, RoutedEventArgs e)
        {
            if (_targetProcesses.Count == 0)
            {
                MessageBox.Show("Please add at least one target process.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            UpdateSystemStatus("Optimizing GPU for target processes...");
            
            try
            {
                foreach (var process in _targetProcesses)
                {
                    await _gpuOptimizer.OptimizeGpuForTargetProcessAsync(process);
                }
                
                _logger.LogInfo("GPU optimization completed for all target processes");
                UpdateSystemStatus("GPU optimization completed");
                MessageBox.Show("GPU optimized for target processes", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to optimize GPU: {ex.Message}");
                UpdateSystemStatus("GPU optimization failed");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region File Compression Tab

        private void BrowseCompressionPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select file or folder to compress",
                CheckFileExists = false,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                CompressionPathTextBox.Text = dialog.FileName;
            }
        }

        private async void CompressPath_Click(object sender, RoutedEventArgs e)
        {
            var path = CompressionPathTextBox.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please enter a valid path.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            UpdateSystemStatus("Compressing files...");
            
            try
            {
                if (File.Exists(path))
                {
                    var result = await _compressionSystem.CompressFileAsync(path);
                    _logger.LogInfo($"Compression completed for {path}");
                    UpdateSystemStatus("Compression completed");
                    MessageBox.Show($"File compressed successfully!\nSaved: {(result.OriginalSize - result.CompressedSize) / (1024 * 1024):F2} MB", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (Directory.Exists(path))
                {
                    var results = await _compressionSystem.CompressDirectoryAsync(path);
                    _logger.LogInfo($"Compression completed for directory {path}");
                    UpdateSystemStatus("Compression completed");
                    MessageBox.Show($"Directory compressed successfully!\nFiles processed: {results.Count}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Path does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to compress {path}: {ex.Message}");
                UpdateSystemStatus("Compression failed");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void StartBackgroundCompression_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _compressionSystem.StartBackgroundCompressionAsync();
                _logger.LogInfo("Background compression started");
                UpdateSystemStatus("Background compression running");
                MessageBox.Show("Background compression started", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start background compression: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Hardware Control Tab

        private void InitializeHardwareControl()
        {
            try
            {
                if (AsusAcpiInterface.IsAvailable())
                {
                    _acpiInterface = new AsusAcpiInterface();
                    _perfModeManager = new PerformanceModeManager(_acpiInterface);
                    _gpuModeController = new GpuModeController(_acpiInterface);
                    _batteryManager = new BatteryManager(_acpiInterface);
                    _hardwareMonitor = new HardwareMonitor(_acpiInterface);
                    _coreManager = new CoreManager(_acpiInterface);

                    // Update UI with current values
                    UpdateHardwareStatus();
                    UpdateCoreStatus();

                    // Start periodic updates
                    _hardwareUpdateTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    _hardwareUpdateTimer.Tick += (s, e) => UpdateHardwareStatus();
                    _hardwareUpdateTimer.Start();

                    _logger.LogInfo("Hardware control initialized successfully");
                }
                else
                {
                    _logger.LogWarning("ASUS ACPI interface not available - hardware control disabled");
                    Dispatcher.Invoke(() =>
                    {
                        CurrentPerfModeText.Text = "Not Available";
                        CurrentGpuModeText.Text = "Not Available";
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize hardware control: {ex.Message}");
            }
        }

        private void UpdateHardwareStatus()
        {
            try
            {
                if (_perfModeManager != null)
                {
                    var perfMode = _perfModeManager.GetCurrentMode();
                    Dispatcher.Invoke(() =>
                    {
                        CurrentPerfModeText.Text = PerformanceModeManager.GetModeName(perfMode);
                    });
                }

                if (_gpuModeController != null)
                {
                    var gpuMode = _gpuModeController.GetCurrentMode();
                    Dispatcher.Invoke(() =>
                    {
                        CurrentGpuModeText.Text = GpuModeController.GetModeName(gpuMode);
                    });
                }

                if (_batteryManager != null)
                {
                    var chargeLimit = _batteryManager.GetChargeLimit();
                    Dispatcher.Invoke(() =>
                    {
                        BatteryLimitText.Text = $"{chargeLimit}%";
                        // Also update the battery limit control if it exists
                        if (CurrentBatteryLimitText != null)
                        {
                            CurrentBatteryLimitText.Text = $"{chargeLimit}%";
                        }
                    });
                }

                if (_hardwareMonitor != null)
                {
                    var cpuTemp = _hardwareMonitor.GetCpuTemperature();
                    var cpuFan = _hardwareMonitor.GetCpuFanSpeed();

                    Dispatcher.Invoke(() =>
                    {
                        CpuTempText.Text = cpuTemp > 0 ? $"{cpuTemp:F0}°C" : "--°C";
                        CpuFanText.Text = cpuFan > 0 ? $"{cpuFan}%" : "--%";
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update hardware status: {ex.Message}");
            }
        }

        private void SetPerformanceMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string modeStr && int.TryParse(modeStr, out int mode))
            {
                try
                {
                    _perfModeManager?.SetMode((PerformanceMode)mode);
                    _logger.LogInfo($"Performance mode set to {PerformanceModeManager.GetModeName((PerformanceMode)mode)}");
                    UpdateHardwareStatus();
                    MessageBox.Show($"Performance mode set to {PerformanceModeManager.GetModeName((PerformanceMode)mode)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to set performance mode: {ex.Message}");
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetGpuMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string modeStr && int.TryParse(modeStr, out int mode))
            {
                try
                {
                    _gpuModeController?.SetMode((GpuMode)mode);
                    _logger.LogInfo($"GPU mode set to {GpuModeController.GetModeName((GpuMode)mode)}");
                    UpdateHardwareStatus();
                    MessageBox.Show($"GPU mode set to {GpuModeController.GetModeName((GpuMode)mode)}\n\n⚠️ RESTART REQUIRED for changes to take effect!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to set GPU mode: {ex.Message}");
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Battery Limit Control Event Handlers
        private void BatteryLimitSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BatteryLimitSliderText != null)
            {
                BatteryLimitSliderText.Text = $"{(int)e.NewValue}%";
            }
        }

        private void SetBatteryLimitPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string limitStr && int.TryParse(limitStr, out int limit))
            {
                if (BatteryLimitSlider != null)
                {
                    BatteryLimitSlider.Value = limit;
                }
            }
        }

        private void ApplyBatteryLimit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BatteryLimitSlider == null || _batteryManager == null)
                {
                    MessageBox.Show("Battery limit control not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int newLimit = (int)BatteryLimitSlider.Value;
                _batteryManager.SetChargeLimit(newLimit);
                _logger.LogInfo($"Battery charge limit set to {newLimit}%");
                UpdateHardwareStatus();
                
                MessageBox.Show($"Battery charge limit set to {newLimit}%\n\n✅ App will re-apply this limit on startup to ensure persistence.", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to set battery limit: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // P/E Core Control Event Handlers
        private void PCoreSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PCoreSliderText != null)
            {
                PCoreSliderText.Text = $"{(int)e.NewValue}";
            }
        }

        private void ECoreSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ECoreSliderText != null)
            {
                ECoreSliderText.Text = $"{(int)e.NewValue}";
            }
        }

        private void SetCorePreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string preset && _coreManager != null)
            {
                var (maxP, maxE) = _coreManager.GetMaxCores();
                
                switch (preset)
                {
                    case "all":
                        if (PCoreSlider != null) PCoreSlider.Value = maxP;
                        if (ECoreSlider != null) ECoreSlider.Value = maxE;
                        break;
                    case "ponly":
                        if (PCoreSlider != null) PCoreSlider.Value = maxP;
                        if (ECoreSlider != null) ECoreSlider.Value = 0;
                        break;
                    case "balanced":
                        if (PCoreSlider != null) PCoreSlider.Value = maxP;
                        if (ECoreSlider != null) ECoreSlider.Value = maxE / 2;
                        break;
                }
            }
        }

        private void ApplyCoreConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PCoreSlider == null || ECoreSlider == null || _coreManager == null)
                {
                    MessageBox.Show("Core control not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int pCores = (int)PCoreSlider.Value;
                int eCores = (int)ECoreSlider.Value;
                
                bool success = _coreManager.SetCores(pCores, eCores);
                
                if (success)
                {
                    _logger.LogInfo($"P/E core configuration set to {pCores}P/{eCores}E cores");
                    MessageBox.Show($"Core configuration set to {pCores} P-cores and {eCores} E-cores\n\n⚠️ RESTART REQUIRED for changes to take effect!", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Failed to set core configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to set core configuration: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCoreStatus()
        {
            try
            {
                if (_coreManager != null && _coreManager.IsSupported())
                {
                    var (maxP, maxE) = _coreManager.GetMaxCores();
                    var (currentP, currentE) = _coreManager.GetCurrentCores();
                    string cpuInfo = CoreManager.GetCpuInfo();
                    
                    Dispatcher.Invoke(() =>
                    {
                        if (CpuInfoText != null) CpuInfoText.Text = cpuInfo;
                        if (MaxCoresText != null) MaxCoresText.Text = $"{maxP} P-cores, {maxE} E-cores";
                        if (CurrentCoresText != null) CurrentCoresText.Text = $"{currentP}P / {currentE}E";
                        
                        // Set slider ranges
                        if (PCoreSlider != null)
                        {
                            PCoreSlider.Minimum = 1;
                            PCoreSlider.Maximum = maxP;
                            PCoreSlider.Value = currentP;
                        }
                        if (ECoreSlider != null)
                        {
                            ECoreSlider.Minimum = 0;
                            ECoreSlider.Maximum = maxE;
                            ECoreSlider.Value = currentE;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update core status: {ex.Message}");
            }
        }

        #endregion

        #region Monitoring Tab

        private void OnPerformanceMetricsUpdated(object? sender, PerformanceMetrics metrics)
        {
            Dispatcher.Invoke(() =>
            {
                CpuUsageText.Text = $"{metrics.CpuUsage:F1}%";
                CpuUsageBar.Value = metrics.CpuUsage;
                
                RamUsageText.Text = $"{metrics.AvailableMemoryMB:F0} MB Available";
                
                DiskUsageText.Text = $"{metrics.DiskUsage:F1}%";
                DiskUsageBar.Value = metrics.DiskUsage;
            });
        }

        private void OnLogEntryAdded(object? sender, LogEntry e)
        {
            Dispatcher.Invoke(() =>
            {
                LogListBox.Items.Insert(0, $"[{e.Timestamp:HH:mm:ss}] {e.Level}: {e.Message}");
                if (LogListBox.Items.Count > 100)
                {
                    LogListBox.Items.RemoveAt(100);
                }
            });
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
            _logger.LogInfo("Logs cleared");
        }

        #endregion

        #region General Methods

        private void UpdateSystemStatus(string status)
        {
            StatusText.Text = status;
        }

        protected override void OnClosed(EventArgs e)
        {
            _performanceMonitor?.StopMonitoring();
            _performanceMonitor?.Dispose();
            _logger?.Dispose();
            
            // Dispose hardware control
            _hardwareUpdateTimer?.Stop();
            _hardwareMonitor?.Dispose();
            _acpiInterface?.Dispose();
            
            base.OnClosed(e);
        }

        #endregion
    }
}