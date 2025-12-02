using System.Windows.Threading;
using RamOptimizer.HardwareControl;
using RamOptimizerUI.Services;

namespace RamOptimizerUI.Views
{
    public partial class PerformanceView : UserControl
    {
        private DispatcherTimer _hardwareUpdateTimer;

        public PerformanceView()
        {
            InitializeComponent();
            
            // Update plugin info
            UpdatePluginInfo();
            
            //Initial update
            UpdateHardwareStatus();
            
            // Load core status asynchronously to avoid hanging UI
            _ = LoadCoreStatusAsync();

            // Start periodic updates
            _hardwareUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _hardwareUpdateTimer.Tick += (s, e) => 
            {
                UpdateHardwareStatus();
                // Core status usually doesn't change dynamically, but we can update it if needed
            };
            _hardwareUpdateTimer.Start();
        }

        private void UpdatePluginInfo()
        {
            try
            {
                // Update plugin name and device type from ServiceLocator
                if (PluginNameText != null)
                {
                    PluginNameText.Text = !string.IsNullOrEmpty(ServiceLocator.PluginName) 
                        ? ServiceLocator.PluginName 
                        : "ASUS ACPI Interface (Legacy)";
                }
                
                if (DeviceTypeText != null)
                {
                    DeviceTypeText.Text = !string.IsNullOrEmpty(ServiceLocator.DeviceType) 
                        ? ServiceLocator.DeviceType 
                        : (ServiceLocator.AcpiInterface != null ? "ASUS ROG Laptop" : "Not Available");
                }
                
                // Update feature support indicators
                bool hasAcpi = ServiceLocator.AcpiInterface != null;
                bool hasCoreManager = ServiceLocator.CoreManager != null && ServiceLocator.CoreManager.IsSupported();
                
                if (FeatureCoresText != null)
                    FeatureCoresText.Text = hasCoreManager ? "✓ CPU Core Control" : "✗ CPU Core Control (Not Supported)";
                
                if (FeatureBatteryText != null)
                    FeatureBatteryText.Text = ServiceLocator.BatteryManager != null ? "✓ Battery Charge Limit" : "✗ Battery Charge Limit (N/A)";
                
                if (FeaturePerformanceText != null)
                    FeaturePerformanceText.Text = ServiceLocator.PerfModeManager != null ? "✓ Performance Modes" : "✗ Performance Modes (N/A)";
                
                if (FeatureGpuText != null)
                    FeatureGpuText.Text = ServiceLocator.GpuModeController != null ? "✓ GPU Mode Switching" : "✗ GPU Mode Switching (N/A)";
                
                if (FeatureTempText != null)
                    FeatureTempText.Text = ServiceLocator.HardwareMonitor != null ? "✓ Temperature Monitoring" : "✗ Temperature Monitoring (N/A)";
                
                if (FeatureFanText != null)
                    FeatureFanText.Text = hasAcpi ? "✓ Fan Control" : "✗ Fan Control (N/A)";
                    
                // Change color for unsupported features
                var unsupportedColor = "#f44336"; // Red
                var supportedColor = "#4CAF50"; // Green
                
                if (!hasCoreManager && FeatureCoresText != null)
                    FeatureCoresText.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(unsupportedColor));
                
                if (ServiceLocator.BatteryManager == null && FeatureBatteryText != null)
                    FeatureBatteryText.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(unsupportedColor));
                    
                if (ServiceLocator.PerfModeManager == null && FeaturePerformanceText != null)
                    FeaturePerformanceText.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(unsupportedColor));
                    
                if (ServiceLocator.GpuModeController == null && FeatureGpuText != null)
                    FeatureGpuText.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(unsupportedColor));
                    
                if (ServiceLocator.HardwareMonitor == null && FeatureTempText != null)
                    FeatureTempText.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(unsupportedColor));
                    
                if (!hasAcpi && FeatureFanText != null)
                    FeatureFanText.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(unsupportedColor));
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Failed to update plugin info: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadCoreStatusAsync()
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    System.Threading.Thread.Sleep(100); // Small delay to let UI render
                    Dispatcher.Invoke(() => UpdateCoreStatus());
                }
                catch
                {
                    // Ignore errors
                }
            });
        }

        private void UpdateHardwareStatus()
        {
            try
            {
                if (ServiceLocator.PerfModeManager != null)
                {
                    var perfMode = ServiceLocator.PerfModeManager.GetCurrentMode();
                    Dispatcher.Invoke(() =>
                    {
                        if (CurrentPerfModeText != null) CurrentPerfModeText.Text = PerformanceModeManager.GetModeName(perfMode);
                    });
                }

                if (ServiceLocator.GpuModeController != null)
                {
                    var gpuMode = ServiceLocator.GpuModeController.GetCurrentMode();
                    Dispatcher.Invoke(() =>
                    {
                        if (CurrentGpuModeText != null) CurrentGpuModeText.Text = GpuModeController.GetModeName(gpuMode);
                    });
                }

                if (ServiceLocator.BatteryManager != null)
                {
                    var chargeLimit = ServiceLocator.BatteryManager.GetChargeLimit();
                    Dispatcher.Invoke(() =>
                    {
                        if (CurrentBatteryLimitText != null) CurrentBatteryLimitText.Text = $"{chargeLimit}%";
                    });
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private void UpdateCoreStatus()
        {
            try
            {
                if (ServiceLocator.CoreManager != null && ServiceLocator.CoreManager.IsSupported())
                {
                    var (maxP, maxE) = ServiceLocator.CoreManager.GetMaxCores();
                    var (currentP, currentE) = ServiceLocator.CoreManager.GetCurrentCores();
                    string cpuInfo = CoreManager.GetCpuInfo();
                    
                    Dispatcher.Invoke(() =>
                    {
                        if (CpuInfoText != null) CpuInfoText.Text = cpuInfo;
                        if (MaxCoresText != null) MaxCoresText.Text = $"{maxP} P-cores, {maxE} E-cores";
                        if (CurrentCoresText != null) CurrentCoresText.Text = $"{currentP}P / {currentE}E";
                        
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
            catch
            {
                // Ignore errors
            }
        }

        private void SetPerformanceMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string modeStr && int.TryParse(modeStr, out int mode))
            {
                try
                {
                    ServiceLocator.PerfModeManager?.SetMode((PerformanceMode)mode);
                    ServiceLocator.Logger?.LogInfo($"Performance mode set to {PerformanceModeManager.GetModeName((PerformanceMode)mode)}");
                    UpdateHardwareStatus();
                    MessageBox.Show($"Performance mode set to {PerformanceModeManager.GetModeName((PerformanceMode)mode)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ServiceLocator.Logger?.LogError($"Failed to set performance mode: {ex.Message}");
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
                    ServiceLocator.GpuModeController?.SetMode((GpuMode)mode);
                    ServiceLocator.Logger?.LogInfo($"GPU mode set to {GpuModeController.GetModeName((GpuMode)mode)}");
                    UpdateHardwareStatus();
                    MessageBox.Show($"GPU mode set to {GpuModeController.GetModeName((GpuMode)mode)}\n\n⚠️ RESTART REQUIRED for changes to take effect!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ServiceLocator.Logger?.LogError($"Failed to set GPU mode: {ex.Message}");
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Battery Limit
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
                if (BatteryLimitSlider == null || ServiceLocator.BatteryManager == null)
                {
                    MessageBox.Show("Battery limit control not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int newLimit = (int)BatteryLimitSlider.Value;
                ServiceLocator.BatteryManager.SetChargeLimit(newLimit);
                ServiceLocator.Logger?.LogInfo($"Battery charge limit set to {newLimit}%");
                UpdateHardwareStatus();
                
                MessageBox.Show($"Battery charge limit set to {newLimit}%\n\n✅ App will re-apply this limit on startup to ensure persistence.", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Failed to set battery limit: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Core Control
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
            if (sender is Button button && button.Tag is string preset && ServiceLocator.CoreManager != null)
            {
                var (maxP, maxE) = ServiceLocator.CoreManager.GetMaxCores();
                
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
                if (PCoreSlider == null || ECoreSlider == null || ServiceLocator.CoreManager == null)
                {
                    MessageBox.Show("Core control not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int pCores = (int)PCoreSlider.Value;
                int eCores = (int)ECoreSlider.Value;
                
                bool success = ServiceLocator.CoreManager.SetCores(pCores, eCores);
                
                if (success)
                {
                    ServiceLocator.Logger?.LogInfo($"P/E core configuration set to {pCores}P/{eCores}E cores");
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
                ServiceLocator.Logger?.LogError($"Failed to set core configuration: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Optimizers
        private async void OptimizeCpu_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceLocator.TargetProcesses.Count == 0)
            {
                MessageBox.Show("Please add at least one target process.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                foreach (var process in ServiceLocator.TargetProcesses)
                {
                    if (ServiceLocator.CpuOptimizer != null)
                        await ServiceLocator.CpuOptimizer.OptimizeCpuForTargetProcessAsync(process);
                }
                
                ServiceLocator.Logger?.LogInfo("CPU optimization completed for all target processes");
                MessageBox.Show("CPU optimized for target processes", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Failed to optimize CPU: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OptimizeGpu_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceLocator.TargetProcesses.Count == 0)
            {
                MessageBox.Show("Please add at least one target process.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                foreach (var process in ServiceLocator.TargetProcesses)
                {
                    if (ServiceLocator.GpuOptimizer != null)
                        await ServiceLocator.GpuOptimizer.OptimizeGpuForTargetProcessAsync(process);
                }
                
                ServiceLocator.Logger?.LogInfo("GPU optimization completed for all target processes");
                MessageBox.Show("GPU optimized for target processes", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Failed to optimize GPU: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _hardwareUpdateTimer?.Stop();
        }
    }
}
