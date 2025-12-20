using System;
using System.Diagnostics;
using System.Management;

namespace RamOptimizerNova.Services;

public class SystemMetricsService
{
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _ramCounter;
    
    public SystemMetricsService()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        
        // Initialize counters
        _cpuCounter.NextValue();
        _ramCounter.NextValue();
    }
    
    public float GetCpuUsage()
    {
        try
        {
            return _cpuCounter.NextValue();
        }
        catch
        {
            return 0;
        }
    }
    
    public (float usedGB, float totalGB, float percentage) GetMemoryUsage()
    {
        try
        {
            var availableMB = _ramCounter.NextValue();
            var totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0);
            var availableGB = availableMB / 1024.0f;
            var usedGB = (float)(totalMemory - availableGB);
            var percentage = (usedGB / (float)totalMemory) * 100;
            
            return (usedGB, (float)totalMemory, percentage);
        }
        catch
        {
            return (0, 16, 0);
        }
    }
    
    public (long freeBytes, long totalBytes, float percentage) GetStorageInfo()
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.Name == "C:\\");
            if (drive != null)
            {
                var freeGB = drive.AvailableFreeSp ace / (1024.0 * 1024.0 * 1024.0);
                var totalGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                var usedGB = totalGB - freeGB;
                var percentage = (float)((usedGB / totalGB) * 100);
                
                return (drive.AvailableFreeSpace, drive.TotalSize, percentage);
            }
            return (0, 0, 0);
        }
        catch
        {
            return (0, 0, 0);
        }
    }
    
    public float GetGpuUsage()
    {
        // GPU monitoring requires more complex code or GPU-specific libraries
        // Placeholder for now
        return 0;
    }
    
    public void Dispose()
    {
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
    }
}
