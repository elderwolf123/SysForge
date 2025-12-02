using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RamOptimizer.HardwareControl
{
    /// <summary>
    /// Power profile with performance/power settings
    /// </summary>
    public class PowerProfile
    {
        /// <summary>
        /// Profile name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Profile description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Power/Performance slider (0-100)
        /// 0 = Maximum Power Saving
        /// 100 = Maximum Performance
        /// </summary>
        public int PowerPerformanceLevel { get; set; } = 50;

        /// <summary>
        /// P-Core count (0 = auto based on PowerPerformanceLevel)
        /// </summary>
        public int PCores { get; set; } = 0;

        /// <summary>
        /// E-Core count (0 = auto based on PowerPerformanceLevel)
        /// </summary>
        public int ECores { get; set; } = 0;

        /// <summary>
        /// Individual power settings
        /// </summary>
        public PowerSettings Settings { get; set; } = new PowerSettings();

        /// <summary>
        /// Process I/O priority overrides
        /// </summary>
        public List<ProcessIOPrioritySetting> ProcessIOPriorities { get; set; } = new List<ProcessIOPrioritySetting>();

        /// <summary>
        /// Whether this is a built-in preset (cannot be deleted)
        /// </summary>
        [JsonIgnore]
        public bool IsPreset { get; set; } = false;
    }

    /// <summary>
    /// Individual power settings with enable/disable toggles
    /// </summary>
    public class PowerSettings
    {
        // CPU Settings
        public bool EnableCpuBoost { get; set; } = true;
        public int CpuMaxFrequency { get; set; } = 100; // Percentage

        // GPU Settings
        public bool EnableGpuBoost { get; set; } = true;
        public int GpuPowerLimit { get; set; } = 100; // Percentage

        // Display Settings
        public int DisplayBrightness { get; set; } = 100; // Percentage
        public int DisplayTimeout { get; set; } = 10; // Minutes (0 = never)

        // Background Settings
        public bool SuspendBackgroundApps { get; set; } = false;
        public bool EnableFastStartup { get; set; } = true;

        // Network Settings
        public bool NetworkAdapterPowerSaving { get; set; } = false;

        // Disk Settings
        public int DiskSleepTimeout { get; set; } = 20; // Minutes (0 = never)
        public bool EnableHibernation { get; set; } = true;

        // USB Settings
        public bool UsbSelectiveSuspend { get; set; } = false;

        // PCI Express Settings
        public bool PciExpressLinkStatePowerManagement { get; set; } = false;
    }

    /// <summary>
    /// Process-specific I/O priority setting
    /// </summary>
    public class ProcessIOPrioritySetting
    {
        public string ProcessName { get; set; } = string.Empty;
        public ProcessManagement.IOPriority Priority { get; set; } = ProcessManagement.IOPriority.Normal;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Setting metadata for UI tooltips
    /// </summary>
    public static class PowerSettingDescriptions
    {
        public static readonly Dictionary<string, string> Descriptions = new()
        {
            ["EnableCpuBoost"] = "CPU Turbo Boost: Allows CPU to exceed base frequency for better performance. Disable to save power.",
            ["CpuMaxFrequency"] = "CPU Max Frequency: Limits maximum CPU frequency. Lower values save power but reduce performance.",
            ["EnableGpuBoost"] = "GPU Boost: Allows GPU to boost clock speeds. Disable for power saving.",
            ["GpuPowerLimit"] = "GPU Power Limit: Restricts GPU power consumption. Lower values extend battery life.",
            ["DisplayBrightness"] = "Display Brightness: Screen brightness level. Lower brightness significantly extends battery life.",
            ["DisplayTimeout"] = "Display Timeout: Time before screen turns off when idle. Shorter timeout saves power.",
            ["SuspendBackgroundApps"] = "Suspend Background Apps: Prevents background apps from running. Saves CPU and RAM but may delay notifications.",
            ["EnableFastStartup"] = "Fast Startup: Speeds up boot time by hibernating kernel. Minimal power impact.",
            ["NetworkAdapterPowerSaving"] = "Network Power Saving: Allows network adapter to sleep. May cause brief connection delays.",
            ["DiskSleepTimeout"] = "Disk Sleep Timeout: Time before hard drive spins down. Saves power but may cause brief delays when accessing files.",
            ["EnableHibernation"] = "Hibernation: Saves RAM to disk and powers off. Slower than sleep but uses no power.",
            ["UsbSelectiveSuspend"] = "USB Selective Suspend: Allows USB devices to sleep. May cause issues with some devices.",
            ["PciExpressLinkStatePowerManagement"] = "PCIe Link State Management: Reduces power to PCIe devices. May impact performance of high-speed devices."
        };
    }
}
