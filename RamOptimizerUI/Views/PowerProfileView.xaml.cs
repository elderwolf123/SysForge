using RamOptimizer.HardwareControl;
using RamOptimizer.ProcessManagement;
using RamOptimizerUI.Services;

namespace RamOptimizerUI.Views
{
    public partial class PowerProfileView : UserControl
    {
        private PowerProfileManager? _profileManager;
        private PowerProfile? _currentProfile;

        public PowerProfileView()
        {
            InitializeComponent();
            InitializeProfileManager();
            LoadProfiles();
            InitializeCoreSliders();
        }

        private void InitializeProfileManager()
        {
            try
            {
                _profileManager = new PowerProfileManager(
                    ServiceLocator.AcpiInterface,
                    ServiceLocator.CoreManager
                );
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Failed to initialize PowerProfileManager: {ex.Message}");
            }
        }

        private void InitializeCoreSliders()
        {
            try
            {
                int maxP = 6; // Default fallback
                int maxE = 8; // Default fallback
                int currentP = 4;
                int currentE = 4;

                if (ServiceLocator.CoreManager != null)
                {
                    var (detectedMaxP, detectedMaxE) = ServiceLocator.CoreManager.GetMaxCores();
                    if (detectedMaxP > 0)
                    {
                        maxP = detectedMaxP;
                        maxE = detectedMaxE;
                        
                        var (detectedCurrentP, detectedCurrentE) = ServiceLocator.CoreManager.GetCurrentCores();
                        if (detectedCurrentP > 0)
                        {
                            currentP = detectedCurrentP;
                            currentE = detectedCurrentE;
                        }
                    }
                }

                // Always populate ComboBoxes (with detected or fallback values)
                for (int i = 1; i <= maxP; i++)
                {
                    PCoreComboBox.Items.Add(i);
                }
                
                for (int i = 0; i <= maxE; i++)
                {
                    ECoreComboBox.Items.Add(i);
                }
                
                // Set current selection
                PCoreComboBox.SelectedItem = currentP;
                ECoreComboBox.SelectedItem = currentE;
                
                // Always show the card
                CoreConfigCard.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Core initialization error: {ex.Message}");
                // Still show card with defaults
                CoreConfigCard.Visibility = Visibility.Visible;
            }
        }

        private void LoadProfiles()
        {
            try
            {
                if (_profileManager == null) return;

                var profiles = _profileManager.GetAllProfiles();
                ProfileComboBox.ItemsSource = profiles;
                ProfileComboBox.DisplayMemberPath = "Name";

                // Select Balanced by default
                var balanced = profiles.FirstOrDefault(p => p.Name == "Balanced");
                if (balanced != null)
                {
                    ProfileComboBox.SelectedItem = balanced;
                }
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Failed to load profiles: {ex.Message}");
            }
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is PowerProfile profile)
            {
                _currentProfile = profile;
                DisplayProfileDetails(profile);
            }
        }

        private void DisplayProfileDetails(PowerProfile profile)
        {
            try
            {
                // Description
                ProfileDescriptionText.Text = profile.Description;

                // Power/Performance Level
                PowerLevelText.Text = profile.PowerPerformanceLevel.ToString();
                PowerLevelSlider.Value = profile.PowerPerformanceLevel;

                // P/E Core Configuration (auto-set but user can override)
                if (profile.PCores > 0 || profile.ECores > 0)
                {
                    PCoreComboBox.SelectedItem = profile.PCores;
                    ECoreComboBox.SelectedItem = profile.ECores;
                }

                // I/O Priority (auto-set based on power level)
                var ioPriority = GetIOPriorityFromPowerLevel(profile.PowerPerformanceLevel);
                IOPrioritySlider.Value = (int)ioPriority;
            }
            catch (Exception ex)
            {
                ServiceLocator.Logger?.LogError($"Failed to display profile details: {ex.Message}");
            }
        }

        private IOPriority GetIOPriorityFromPowerLevel(int powerLevel)
        {
            return powerLevel switch
            {
                <= 20 => IOPriority.Low,
                <= 50 => IOPriority.Normal,
                <= 75 => IOPriority.High,
                _ => IOPriority.High
            };
        }

        private void PCoreComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No need for separate display - ComboBox shows the value
        }

        private void ECoreComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No need for separate display - ComboBox shows the value
        }

        private void IOPrioritySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IOPriorityText != null)
            {
                var priority = (IOPriority)(int)e.NewValue;
                IOPriorityText.Text = priority switch
                {
                    IOPriority.VeryLow => "Very Low",
                    IOPriority.Low => "Low",
                    IOPriority.Normal => "Normal",
                    IOPriority.High => "High",
                    _ => "Normal"
                };
            }
        }

        private void ApplyProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentProfile == null || _profileManager == null)
                {
                    MessageBox.Show("Please select a profile first.", "No Profile Selected", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get custom values from ComboBoxes
                int customPCores = (int)(PCoreComboBox.SelectedItem ?? 4);
                int customECores = (int)(ECoreComboBox.SelectedItem ?? 4);
                var customIOPriority = (IOPriority)(int)IOPrioritySlider.Value;

                // Create a copy of the profile with custom settings
                var customProfile = new PowerProfile
                {
                    Name = _currentProfile.Name,
                    Description = _currentProfile.Description,
                    PowerPerformanceLevel = _currentProfile.PowerPerformanceLevel,
                    PCores = customPCores,
                    ECores = customECores,
                    Settings = _currentProfile.Settings,
                    ProcessIOPriorities = _currentProfile.ProcessIOPriorities
                };

                // Show confirmation
                var message = $"Applying profile '{customProfile.Name}' with:\n\n" +
                             $"P-Cores: {customPCores}\n" +
                             $"E-Cores: {customECores}\n" +
                             $"I/O Priority: {customIOPriority}\n\n";

                if (customPCores > 0 || customECores > 0)
                {
                    message += "⚠️ Core changes require a system restart to take effect.\n\n";
                }

                message += "Apply this profile?";

                var result = MessageBox.Show(message, "Confirm Profile Application",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Apply profile
                bool success = _profileManager.ApplyProfile(customProfile);

                // Apply I/O priority to current process
                IOPriorityManager.SetProcessIOPriority(System.Diagnostics.Process.GetCurrentProcess(), customIOPriority);

                if (success)
                {
                    var successMsg = $"Profile '{customProfile.Name}' applied successfully!\n\n" +
                                   $"✅ P/E Cores: {customPCores}P / {customECores}E\n" +
                                   $"✅ I/O Priority: {customIOPriority}\n\n";

                    if (customPCores > 0)
                    {
                        successMsg += "⚠️ Restart your system for core changes to take effect.";
                    }

                    MessageBox.Show(successMsg, "Profile Applied",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    ServiceLocator.Logger?.LogInfo($"Applied power profile: {customProfile.Name} (Custom: {customPCores}P/{customECores}E, I/O: {customIOPriority})");
                }
                else
                {
                    MessageBox.Show(
                        "Failed to apply some profile settings. Check logs for details.",
                        "Partial Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying profile: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ServiceLocator.Logger?.LogError($"Failed to apply profile: {ex.Message}");
            }
        }
    }
}
