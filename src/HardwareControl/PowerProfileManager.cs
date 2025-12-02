using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RamOptimizer.ProcessManagement;

namespace RamOptimizer.HardwareControl
{
    /// <summary>
    /// Manages power profiles and applies settings
    /// </summary>
    public class PowerProfileManager
    {
        private readonly string _profilesPath;
        private readonly AsusAcpiInterface? _acpi;
        private readonly CoreManager? _coreManager;

        public PowerProfileManager(AsusAcpiInterface? acpi = null, CoreManager? coreManager = null)
        {
            _acpi = acpi;
            _coreManager = coreManager;
            
            // Store profiles in AppData
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _profilesPath = Path.Combine(appData, "RamOptimus", "profiles");
            Directory.CreateDirectory(_profilesPath);
        }

        /// <summary>
        /// Get all built-in preset profiles
        /// </summary>
        public List<PowerProfile> GetPresetProfiles()
        {
            var (maxP, maxE) = _coreManager?.GetMaxCores() ?? (0, 0);
            bool hasCores = maxP > 0 && maxE > 0;

            return new List<PowerProfile>
            {
                // Extreme Power Saver - Maximum battery life
                new PowerProfile
                {
                    Name = "Extreme Power Saver",
                    Description = "Maximum battery life with minimal performance. P-cores only, aggressive power saving.",
                    PowerPerformanceLevel = 0,
                    PCores = hasCores ? Math.Max(2, maxP / 2) : 0, // Half P-cores, minimum 2
                    ECores = 0, // No E-cores
                    IsPreset = true,
                    Settings = new PowerSettings
                    {
                        EnableCpuBoost = false,
                        CpuMaxFrequency = 50,
                        EnableGpuBoost = false,
                        GpuPowerLimit = 50,
                        DisplayBrightness = 40,
                        DisplayTimeout = 2,
                        SuspendBackgroundApps = true,
                        NetworkAdapterPowerSaving = true,
                        DiskSleepTimeout = 5,
                        UsbSelectiveSuspend = true,
                        PciExpressLinkStatePowerManagement = true
                    }
                },

                // Power Saver - Extended battery
                new PowerProfile
                {
                    Name = "Power Saver",
                    Description = "Extended battery life with acceptable performance. Reduced E-cores, conservative settings.",
                    PowerPerformanceLevel = 25,
                    PCores = hasCores ? maxP : 0,
                    ECores = hasCores ? maxE / 2 : 0, // Half E-cores
                    IsPreset = true,
                    Settings = new PowerSettings
                    {
                        EnableCpuBoost = false,
                        CpuMaxFrequency = 75,
                        EnableGpuBoost = false,
                        GpuPowerLimit = 75,
                        DisplayBrightness = 60,
                        DisplayTimeout = 5,
                        SuspendBackgroundApps = true,
                        NetworkAdapterPowerSaving = true,
                        DiskSleepTimeout = 10,
                        UsbSelectiveSuspend = true,
                        PciExpressLinkStatePowerManagement = true
                    }
                },

                // Balanced - Optimal mix
                new PowerProfile
                {
                    Name = "Balanced",
                    Description = "Optimal balance between performance and battery life. All cores, balanced settings.",
                    PowerPerformanceLevel = 50,
                    PCores = hasCores ? maxP : 0,
                    ECores = hasCores ? maxE : 0, // All cores
                    IsPreset = true,
                    Settings = new PowerSettings
                    {
                        EnableCpuBoost = true,
                        CpuMaxFrequency = 90,
                        EnableGpuBoost = true,
                        GpuPowerLimit = 90,
                        DisplayBrightness = 80,
                        DisplayTimeout = 10,
                        SuspendBackgroundApps = false,
                        NetworkAdapterPowerSaving = false,
                        DiskSleepTimeout = 20,
                        UsbSelectiveSuspend = false,
                        PciExpressLinkStatePowerManagement = false
                    }
                },

                // Performance - Prioritize speed
                new PowerProfile
                {
                    Name = "Performance",
                    Description = "High performance with moderate power consumption. All cores, performance settings.",
                    PowerPerformanceLevel = 75,
                    PCores = hasCores ? maxP : 0,
                    ECores = hasCores ? maxE : 0, // All cores
                    IsPreset = true,
                    Settings = new PowerSettings
                    {
                        EnableCpuBoost = true,
                        CpuMaxFrequency = 100,
                        EnableGpuBoost = true,
                        GpuPowerLimit = 100,
                        DisplayBrightness = 100,
                        DisplayTimeout = 15,
                        SuspendBackgroundApps = false,
                        NetworkAdapterPowerSaving = false,
                        DiskSleepTimeout = 0, // Never
                        UsbSelectiveSuspend = false,
                        PciExpressLinkStatePowerManagement = false
                    }
                },

                // Maximum Performance - No power saving
                new PowerProfile
                {
                    Name = "Maximum Performance",
                    Description = "Absolute maximum performance, no power saving. All cores, all limits removed.",
                    PowerPerformanceLevel = 100,
                    PCores = hasCores ? maxP : 0,
                    ECores = hasCores ? maxE : 0, // All cores
                    IsPreset = true,
                    Settings = new PowerSettings
                    {
                        EnableCpuBoost = true,
                        CpuMaxFrequency = 100,
                        EnableGpuBoost = true,
                        GpuPowerLimit = 100,
                        DisplayBrightness = 100,
                        DisplayTimeout = 0, // Never
                        SuspendBackgroundApps = false,
                        NetworkAdapterPowerSaving = false,
                        DiskSleepTimeout = 0, // Never
                        EnableHibernation = false,
                        UsbSelectiveSuspend = false,
                        PciExpressLinkStatePowerManagement = false
                    }
                }
            };
        }

        /// <summary>
        /// Get all custom user profiles
        /// </summary>
        public List<PowerProfile> GetCustomProfiles()
        {
            var profiles = new List<PowerProfile>();

            try
            {
                var files = Directory.GetFiles(_profilesPath, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var profile = JsonSerializer.Deserialize<PowerProfile>(json);
                        if (profile != null)
                        {
                            profile.IsPreset = false;
                            profiles.Add(profile);
                        }
                    }
                    catch
                    {
                        // Skip invalid profiles
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return profiles;
        }

        /// <summary>
        /// Get all profiles (presets + custom)
        /// </summary>
        public List<PowerProfile> GetAllProfiles()
        {
            var profiles = new List<PowerProfile>();
            profiles.AddRange(GetPresetProfiles());
            profiles.AddRange(GetCustomProfiles());
            return profiles;
        }

        /// <summary>
        /// Save a custom profile
        /// </summary>
        public bool SaveProfile(PowerProfile profile)
        {
            try
            {
                if (profile.IsPreset)
                    return false; // Cannot save over presets

                string fileName = SanitizeFileName(profile.Name) + ".json";
                string filePath = Path.Combine(_profilesPath, fileName);

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(profile, options);
                File.WriteAllText(filePath, json);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Delete a custom profile
        /// </summary>
        public bool DeleteProfile(string profileName)
        {
            try
            {
                string fileName = SanitizeFileName(profileName) + ".json";
                string filePath = Path.Combine(_profilesPath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch
            {
                // Ignore errors
            }

            return false;
        }

        /// <summary>
        /// Apply a power profile
        /// </summary>
        public bool ApplyProfile(PowerProfile profile)
        {
            bool success = true;

            try
            {
                // Apply P/E core configuration (requires restart)
                if (_coreManager != null && profile.PCores > 0)
                {
                    success &= _coreManager.SetCores(profile.PCores, profile.ECores);
                }

                // Apply Windows power plan based on performance level
                ApplyWindowsPowerPlan(profile.PowerPerformanceLevel);

                // Apply CPU settings
                ApplyCpuSettings(profile.Settings);

                // Apply GPU settings
                ApplyGpuSettings(profile.Settings);

                // Apply display settings
                ApplyDisplaySettings(profile.Settings);

                // Apply I/O priorities
                ApplyIOPriorities(profile.ProcessIOPriorities);

                // Apply other Windows power settings
                ApplyWindowsPowerSettings(profile.Settings);

                return success;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyWindowsPowerPlan(int performanceLevel)
        {
            try
            {
                string planGuid = performanceLevel switch
                {
                    <= 20 => "a1841308-3541-4fab-bc81-f71556f20b4a", // Power Saver
                    <= 60 => "381b4222-f694-41f0-9685-ff5bb260df2e", // Balanced
                    _ => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" // High Performance
                };

                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = $"/setactive {planGuid}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit();
            }
            catch
            {
                // Ignore errors
            }
        }

        private void ApplyCpuSettings(PowerSettings settings)
        {
            // CPU settings are primarily handled by Windows power plan
            // Additional CPU frequency limiting would require more advanced power management
        }

        private void ApplyGpuSettings(PowerSettings settings)
        {
            // GPU power limiting would require GPU-specific APIs (NVIDIA/AMD)
            // This is a placeholder for future implementation
        }

        private void ApplyDisplaySettings(PowerSettings settings)
        {
            try
            {
                // Set display timeout
                if (settings.DisplayTimeout > 0)
                {
                    var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = $"/change monitor-timeout-ac {settings.DisplayTimeout}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    process?.WaitForExit();

                    process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = $"/change monitor-timeout-dc {settings.DisplayTimeout}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    process?.WaitForExit();
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private void ApplyIOPriorities(List<ProcessIOPrioritySetting> priorities)
        {
            foreach (var setting in priorities)
            {
                try
                {
                    IOPriorityManager.SetProcessIOPriorityByName(setting.ProcessName, setting.Priority);
                }
                catch
                {
                    // Ignore errors
                }
            }
        }

        private void ApplyWindowsPowerSettings(PowerSettings settings)
        {
            try
            {
                // Disk sleep timeout
                if (settings.DiskSleepTimeout >= 0)
                {
                    var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = $"/change disk-timeout-ac {settings.DiskSleepTimeout}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    process?.WaitForExit();

                    process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = $"/change disk-timeout-dc {settings.DiskSleepTimeout}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    process?.WaitForExit();
                }

                // USB selective suspend
                SetPowerSetting("2a737441-1930-4402-8d77-b2bebba308a3", "48e6b7a6-50f5-4782-a5d4-53bb8f07e226", 
                    settings.UsbSelectiveSuspend ? 1 : 0);

                // PCI Express Link State Power Management
                SetPowerSetting("501a4d13-42af-4429-9fd1-a8218c268e20", "ee12f906-d277-404b-b6da-e5fa1a576df5",
                    settings.PciExpressLinkStatePowerManagement ? 2 : 0);
            }
            catch
            {
                // Ignore errors
            }
        }

        private void SetPowerSetting(string schemeGuid, string subgroupGuid, int value)
        {
            try
            {
                // Set for AC (plugged in)
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = $"/setacvalueindex SCHEME_CURRENT {subgroupGuid} {schemeGuid} {value}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit();

                // Set for DC (battery)
                process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = $"/setdcvalueindex SCHEME_CURRENT {subgroupGuid} {schemeGuid} {value}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit();

                // Apply changes
                process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/setactive SCHEME_CURRENT",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit();
            }
            catch
            {
                // Ignore errors
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}
