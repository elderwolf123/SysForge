using System.Collections.Generic;
using RamOptimizer.ProcessManagement;
using RamOptimizer.Monitoring;
using RamOptimizer.Logging;
using RamOptimizer.Configuration;
using RamOptimizer.HardwareControl;
using RamOptimizer.Core.Interfaces;
using RamOptimizer.Core.Plugins;

namespace RamOptimizerUI.Services
{
    public static class ServiceLocator
    {
        public static AdvancedCpuOptimizer CpuOptimizer { get; set; }
        public static AdvancedGpuOptimizer GpuOptimizer { get; set; }
        public static AdvancedFileCompressionSystem CompressionSystem { get; set; }
        public static RealTimePerformanceMonitor PerformanceMonitor { get; set; }
        public static ComprehensiveLogger Logger { get; set; }
        public static ConfigurationManager ConfigManager { get; set; }
        
        // Hardware Control (Legacy)
        public static AsusAcpiInterface AcpiInterface { get; set; }
        public static PerformanceModeManager PerfModeManager { get; set; }
        public static GpuModeController GpuModeController { get; set; }
        public static BatteryManager BatteryManager { get; set; }
        public static HardwareMonitor HardwareMonitor { get; set; }
        public static CoreManager CoreManager { get; set; }
        
        // New Plugin Architecture  
        public static IHardwarePlugin CurrentPlugin { get; set; }
        public static SafeHardwareController SafeController { get; set; }
        public static string PluginName { get; set; }
        public static string DeviceType { get; set; }

        // Shared State
        public static List<string> TargetProcesses { get; set; } = new List<string>();
    }
}
