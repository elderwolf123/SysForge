using System.Diagnostics;
using System.Management;

namespace RamOptimizer.ServiceTesting;

/// <summary>
/// System performance metrics snapshot
/// </summary>
public class SystemMetrics
{
    public long TotalRAMUsageMB { get; set; }
    public double CPUUsagePercent { get; set; }
    public double DiskIORate { get; set; }        // MB/s
    public double PowerDrawWatts { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Monitors system performance metrics during testing
/// </summary>
public class MetricsMonitor : IDisposable
{
    private PerformanceCounter? _ramCounter;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _diskReadCounter;
    private PerformanceCounter? _diskWriteCounter;
    private bool _disposed;

    public MetricsMonitor()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
                _diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
                
                // Initial read to initialize counters
                _ramCounter.NextValue();
                _cpuCounter.NextValue();
                _diskReadCounter.NextValue();
                _diskWriteCounter.NextValue();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize performance counters: {ex.Message}");
        }
    }

    /// <summary>
    /// Capture baseline metrics over specified duration
    /// </summary>
    public async Task<SystemMetrics> CaptureBaseline(int durationSeconds = 10)
    {
        var samples = new List<SystemMetrics>();
        var sampleInterval = TimeSpan.FromSeconds(1);
        var sampleCount = durationSeconds;

        for (int i = 0; i < sampleCount; i++)
        {
            samples.Add(CaptureSingle());
            await Task.Delay(sampleInterval);
        }

        return AverageMetrics(samples);
    }

    /// <summary>
    /// Monitor metrics during a test period
    /// </summary>
    public async Task<SystemMetrics> MonitorDuring(int durationSeconds = 30)
    {
        return await CaptureBaseline(durationSeconds);
    }

    /// <summary>
    /// Capture a single metrics snapshot
    /// </summary>
    public SystemMetrics CaptureSingle()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return new SystemMetrics { Timestamp = DateTime.Now };

            var totalRAM = GetTotalRAM();
            var availableRAM = _ramCounter?.NextValue() ?? 0;
            var usedRAM = totalRAM - (long)availableRAM;

            var diskRead = _diskReadCounter?.NextValue() ?? 0;
            var diskWrite = _diskWriteCounter?.NextValue() ?? 0;
            var diskIO = (diskRead + diskWrite) / (1024 * 1024); // Convert to MB/s

            return new SystemMetrics
            {
                TotalRAMUsageMB = usedRAM,
                CPUUsagePercent = _cpuCounter?.NextValue() ?? 0,
                DiskIORate = diskIO,
                PowerDrawWatts = EstimatePowerDraw(),
                Timestamp = DateTime.Now
            };
        }
        catch
        {
            return new SystemMetrics { Timestamp = DateTime.Now };
        }
    }

    private SystemMetrics AverageMetrics(List<SystemMetrics> samples)
    {
        if (samples.Count == 0)
            return new SystemMetrics { Timestamp = DateTime.Now };

        return new SystemMetrics
        {
            TotalRAMUsageMB = (long)samples.Average(s => s.TotalRAMUsageMB),
            CPUUsagePercent = samples.Average(s => s.CPUUsagePercent),
            DiskIORate = samples.Average(s => s.DiskIORate),
            PowerDrawWatts = samples.Average(s => s.PowerDrawWatts),
            Timestamp = DateTime.Now
        };
    }

    private long GetTotalRAM()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                foreach (var item in searcher.Get())
                {
                    // TotalVisibleMemorySize is in KB
                    var kb = Convert.ToUInt64(item["TotalVisibleMemorySize"]);
                    return (long)(kb / 1024); // Convert to MB
                }
            }
            return 16384; // Default
        }
        catch
        {
            return 16384; // Default to 16GB if can't detect
        }
    }

    private double EstimatePowerDraw()
    {
        // Rough estimation based on CPU usage
        // Real power monitoring would require additional hardware/APIs
        var cpuUsage = _cpuCounter?.NextValue() ?? 0;
        return (cpuUsage / 100.0) * 45.0; // Estimate: 0-45W based on CPU
    }

    /// <summary>
    /// Calculate performance impact between baseline and test metrics
    /// </summary>
    public static PerformanceImpact CalculateImpact(SystemMetrics baseline, SystemMetrics test)
    {
        return new PerformanceImpact
        {
            RAMDeltaMB = test.TotalRAMUsageMB - baseline.TotalRAMUsageMB,
            CPUDeltaPercent = test.CPUUsagePercent - baseline.CPUUsagePercent,
            IODeltaMBps = test.DiskIORate - baseline.DiskIORate,
            PowerDeltaWatts = test.PowerDrawWatts - baseline.PowerDrawWatts
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        _ramCounter?.Dispose();
        _cpuCounter?.Dispose();
        _diskReadCounter?.Dispose();
        _diskWriteCounter?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
