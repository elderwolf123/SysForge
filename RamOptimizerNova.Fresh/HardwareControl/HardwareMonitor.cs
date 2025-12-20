using System.Diagnostics;

namespace RamOptimizer.HardwareControl;

/// <summary>
/// Hardware monitoring for temperature, fan speeds, and power
/// Uses ACPI interface for ASUS-specific sensors
/// </summary>
public class HardwareMonitor : IDisposable
{
    private readonly AsusAcpiInterface? _acpiInterface;
    private readonly PerformanceCounter? _cpuPowerCounter;
    private bool _disposed = false;

    public HardwareMonitor(AsusAcpiInterface? acpiInterface = null)
    {
        _acpiInterface = acpiInterface;
        
        try
        {
            // Initialize performance counter for CPU usage
            _cpuPowerCounter = new PerformanceCounter("Processor Information", "% Processor Time", "_Total", true);
        }
        catch
        {
            // Performance counters may not be available on all systems
        }
    }

    /// <summary>
    /// Get CPU temperature in Celsius using ACPI
    /// </summary>
    public float GetCpuTemperature()
    {
        try
        {
            if (_acpiInterface != null)
            {
                // ACPI returns temperature directly in Celsius
                var temp = _acpiInterface.DeviceGet(AsusAcpiInterface.Temp_CPU);
                if (temp > 0 && temp < 150) // Sanity check (0-150°C)
                {
                    return temp;
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return 0;
    }

    /// <summary>
    /// Get GPU temperature in Celsius using ACPI
    /// </summary>
    public float GetGpuTemperature()
    {
        try
        {
            if (_acpiInterface != null)
            {
                // ACPI returns temperature directly in Celsius
                var temp = _acpiInterface.DeviceGet(AsusAcpiInterface.Temp_GPU);
                if (temp > 0 && temp < 150) // Sanity check (0-150°C)
                {
                    return temp;
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return 0;
    }

    /// <summary>
    /// Get CPU fan speed percentage using ACPI
    /// Implements G-Helper's GetFan() logic
    /// </summary>
    public int GetCpuFanSpeed()
    {
        try
        {
            if (_acpiInterface != null)
            {
                int fan = _acpiInterface.DeviceGet(AsusAcpiInterface.CPU_Fan);
                
                // G-Helper's logic: if negative, add 65536 back
                // This is because DeviceGet subtracts 65536 from raw ACPI value
                if (fan < 0)
                {
                    fan += 65536;
                    // Sanity check: fan should be 0-100 (in hundreds of RPM)
                    if (fan <= 0 || fan > 100)
                        return 0;
                }
                
                // Fan value is in hundreds of RPM, but we want percentage
                // G-Helper displays it as RPM * 100, but for percentage we just return the value
                return fan;
            }
        }
        catch
        {
            // Ignore errors
        }

        return 0;
    }

    /// <summary>
    /// Get GPU fan speed percentage using ACPI
    /// </summary>
    public int GetGpuFanSpeed()
    {
        try
        {
            if (_acpiInterface != null)
            {
                int fan = _acpiInterface.DeviceGet(AsusAcpiInterface.GPU_Fan);
                
                // Same logic as CPU fan
                if (fan < 0)
                {
                    fan += 65536;
                    if (fan <= 0 || fan > 100)
                        return 0;
                }
                
                return fan;
            }
        }
        catch
        {
            // Ignore errors
        }

        return 0;
    }

    /// <summary>
    /// Get CPU usage percentage
    /// </summary>
    public float GetCpuUsage()
    {
        try
        {
            if (_cpuPowerCounter != null)
            {
                return _cpuPowerCounter.NextValue();
            }
        }
        catch
        {
            // Ignore errors
        }

        return 0;
    }

    /// <summary>
    /// Get system power information
    /// </summary>
    public PowerInfo GetPowerInfo()
    {
        var info = new PowerInfo();

        try
        {
            var status = System.Windows.Forms.SystemInformation.PowerStatus;
            info.IsPluggedIn = status.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;
            info.BatteryPercentage = (int)(status.BatteryLifePercent * 100);
            info.BatteryLifeRemaining = status.BatteryLifeRemaining;
        }
        catch
        {
            // Ignore errors
        }

        return info;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cpuPowerCounter?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

public class PowerInfo
{
    public bool IsPluggedIn { get; set; }
    public int BatteryPercentage { get; set; }
    public int BatteryLifeRemaining { get; set; } // in seconds
}
