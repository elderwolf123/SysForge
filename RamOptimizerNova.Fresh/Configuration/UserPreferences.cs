using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RamOptimizer.Configuration
{
    public class UserPreferences
    {
        public OptimizationPreferences Optimization { get; set; } = new OptimizationPreferences();
        public CompressionPreferences Compression { get; set; } = new CompressionPreferences();
        public PowerManagementPreferences PowerManagement { get; set; } = new PowerManagementPreferences();
        public MonitoringPreferences Monitoring { get; set; } = new MonitoringPreferences();
    }

    public class OptimizationPreferences
    {
        public bool EnableAggressiveOptimization { get; set; } = false;
        public int AggressionLevel { get; set; } = 1;
        public List<string> ExcludedProcesses { get; set; } = new List<string>();
        public bool AutoRestartExplorer { get; set; } = true;
        public int OptimizationInterval { get; set; } = 30; // seconds
    }

    public class CompressionPreferences
    {
        public bool EnableCompression { get; set; } = false;
        public string CompressionLevel { get; set; } = "Optimal";
        public List<string> FileTypesToCompress { get; set; } = new List<string> { ".txt", ".log", ".csv", ".json" };
        public long MinFileSizeToCompress { get; set; } = 1024 * 1024; // 1MB
    }

    public class PowerManagementPreferences
    {
        public bool EnablePowerManagement { get; set; } = false;
        public string PowerMode { get; set; } = "Balanced";
        public int BatteryThreshold { get; set; } = 20; // percentage
        public bool OptimizeForBattery { get; set; } = false;
    }

    public class MonitoringPreferences
    {
        public bool EnableRealTimeMonitoring { get; set; } = true;
        public int MonitoringInterval { get; set; } = 1000; // milliseconds
        public bool ShowNotifications { get; set; } = true;
        public bool LogToFile { get; set; } = true;
    }
}