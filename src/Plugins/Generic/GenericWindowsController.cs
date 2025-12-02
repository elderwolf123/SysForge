using System;
using System.Management;
using RamOptimizer.Core.Interfaces;

namespace RamOptimizer.Plugins.Generic;

/// <summary>
/// Generic Windows hardware controller using WMI
/// Provides read-only monitoring for non-ASUS systems
/// </summary>
public class GenericWindowsController : IHardwareController, ICoreController, IBatteryController
{
    private bool _disposed;

    #region IHardwareController

    public bool IsAvailable()
    {
        try
        {
            // Check if WMI is available
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            var results = searcher.Get();
            return results.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public string GetDeviceIdentifier()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Model FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                return obj["Model"]?.ToString() ?? "Unknown";
            }
        }
        catch { }
        return "Unknown";
    }

    public string GetDeviceType()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                string manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                return $"{manufacturer} Device";
            }
        }
        catch { }
        return "Generic Windows Device";
    }

    public bool Initialize()
    {
        return IsAvailable();
    }

    #endregion

    #region ICoreController (Read-Only)

    public bool IsSupported => true;

    public int GetMaxPCores()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                return Convert.ToInt32(obj["NumberOfCores"]);
            }
        }
        catch { }
        return 0;
    }

    public int GetMaxECores()
    {
        // WMI doesn't distinguish between P and E cores
        // Return 0 for generic systems
        return 0;
    }

    public int GetCurrentPCores()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT NumberOfLogicalProcessors FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                // Best approximation for generic systems
                return Convert.ToInt32(obj["NumberOfLogicalProcessors"]) / 2;
            }
        }
        catch { }
        return 0;
    }

    public int GetCurrentECores()
    {
        // WMI doesn't distinguish between P and E cores
        return 0;
    }

    public bool SetCores(int pCores, int eCores)
    {
        // Generic Windows controller is READ-ONLY
        throw new NotSupportedException("Core control is not supported on generic Windows devices. This requires manufacturer-specific drivers.");
    }

    #endregion

    #region IBatteryController (Read-Only)

    bool IBatteryController.IsSupported => true;

    public int GetChargeLimit()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining FROM Win32_Battery");
            foreach (var obj in searcher.Get())
            {
                return Convert.ToInt32(obj["EstimatedChargeRemaining"]);
            }
        }
        catch { }
        return 100; // Default to 100% if unable to read
    }

    public bool SetChargeLimit(int limitPercent)
    {
        // Generic Windows controller is READ-ONLY
        throw new NotSupportedException("Battery limit control is not supported on generic Windows devices. This requires manufacturer-specific drivers.");
    }

    public int GetMinLimit() => 60;
    public int GetMaxLimit() => 100;

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
