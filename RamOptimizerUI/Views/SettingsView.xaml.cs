using RamOptimizer.HardwareControl;
using RamOptimizerUI.Services;

namespace RamOptimizerUI.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly StartupManager _startupManager;
        private readonly AsusServiceManager _asusServiceManager;
        private bool _isInitialized = false;

        public SettingsView()
        {
            InitializeComponent();
            
            // Initialize managers directly since they are lightweight
            _startupManager = new StartupManager("RamOptimus", System.Reflection.Assembly.GetExecutingAssembly().Location);
            _asusServiceManager = new AsusServiceManager(ServiceLocator.Logger);

            LoadSettings();
            _isInitialized = true;
        }

        private void LoadSettings()
        {
            try
            {
                // Load Startup State
                RunOnStartupCheckBox.IsChecked = _startupManager.IsRunOnStartupEnabled();

                // Load ASUS Service State (This is a bit tricky as it's a runtime state, 
                // but we can check if they are currently running to infer preference or just default to unchecked)
                // For now, we'll leave it unchecked by default unless we persist this setting elsewhere.
                // TODO: Persist this setting in a config file.
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Error loading settings: {ex.Message}");
            }
        }

        private void RunOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            try
            {
                _startupManager.SetRunOnStartup(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to enable startup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                RunOnStartupCheckBox.IsChecked = false;
            }
        }

        private void RunOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            try
            {
                _startupManager.SetRunOnStartup(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to disable startup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                RunOnStartupCheckBox.IsChecked = true;
            }
        }

        private async void DisableAsusServices_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            try
            {
                await _asusServiceManager.StopAsusServicesAsync();
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Failed to stop ASUS services: {ex.Message}");
            }
        }

        private async void DisableAsusServices_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            try
            {
                await _asusServiceManager.StartAsusServicesAsync();
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Failed to start ASUS services: {ex.Message}");
            }
        }
    }
}
