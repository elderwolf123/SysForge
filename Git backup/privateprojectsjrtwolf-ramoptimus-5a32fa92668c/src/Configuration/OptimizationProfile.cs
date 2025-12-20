using System;
using System.Collections.Generic;

namespace RamOptimizer.Configuration
{
    public class OptimizationProfile
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public OptimizationSettings Settings { get; set; } = new OptimizationSettings();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class OptimizationSettings
    {
        public ProcessOptimizationSettings ProcessOptimization { get; set; } = new ProcessOptimizationSettings();
        public MemoryOptimizationSettings MemoryOptimization { get; set; } = new MemoryOptimizationSettings();
        public DiskOptimizationSettings DiskOptimization { get; set; } = new DiskOptimizationSettings();
        public NetworkOptimizationSettings NetworkOptimization { get; set; } = new NetworkOptimizationSettings();
    }

    public class ProcessOptimizationSettings
    {
        public bool EnableProcessTermination { get; set; } = true;
        public int AggressionLevel { get; set; } = 1;
        public List<string> CriticalProcesses { get; set; } = new List<string>();
        public List<string> ExcludedProcesses { get; set; } = new List<string>();
        public bool AutoRestartTerminatedProcesses { get; set; } = false;
    }

    public class MemoryOptimizationSettings
    {
        public bool EnableMemoryCompression { get; set; } = false;
        public bool EnableWorkingSetTrimming { get; set; } = false;
        public int WorkingSetTrimPercentage { get; set; } = 10;
        public bool EnablePageFileOptimization { get; set; } = false;
    }

    public class DiskOptimizationSettings
    {
        public bool EnableDiskCleanup { get; set; } = false;
        public bool EnableDiskDefragmentation { get; set; } = false;
        public List<string> CleanupDirectories { get; set; } = new List<string>();
        public long MinFileSizeForCleanup { get; set; } = 1024 * 1024; // 1MB
    }

    public class NetworkOptimizationSettings
    {
        public bool EnableNetworkOptimization { get; set; } = false;
        public int MaxConcurrentConnections { get; set; } = 10;
        public bool EnableBandwidthThrottling { get; set; } = false;
        public int BandwidthLimit { get; set; } = 100; // Mbps
    }
}