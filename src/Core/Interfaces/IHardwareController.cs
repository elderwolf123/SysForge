using System;

namespace RamOptimizer.Core.Interfaces
{
    /// <summary>
    /// Base interface for all hardware control operations
    /// Device-agnostic - works for any laptop/desktop
    /// </summary>
    public interface IHardwareController : IDisposable
    {
        /// <summary>
        /// Check if this controller is available on the current system
        /// </summary>
        bool IsAvailable();

        /// <summary>
        /// Get the manufacturer/device identifier
        /// </summary>
        string GetDeviceIdentifier();

        /// <summary>
        /// Get the device type (e.g., "ASUS ROG", "Generic Windows", etc.)
        /// </summary>
        string GetDeviceType();

        /// <summary>
        /// Initialize the controller
        /// </summary>
        bool Initialize();
    }

    /// <summary>
    /// Interface for CPU core control
    /// </summary>
    public interface ICoreController
    {
        /// <summary>
        /// Check if core control is supported
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Get maximum number of P-cores
        /// </summary>
        int GetMaxPCores();

        /// <summary>
        /// Get maximum number of E-cores
        /// </summary>
        int GetMaxECores();

        /// <summary>
        /// Get currently active P-cores
        /// </summary>
        int GetCurrentPCores();

        /// <summary>
        /// Get currently active E-cores
        /// </summary>
        int GetCurrentECores();

        /// <summary>
        /// Set core configuration
        /// </summary>
        bool SetCores(int pCores, int eCores);
    }

    /// <summary>
    /// Interface for battery management
    /// </summary>
    public interface IBatteryController
    {
        /// <summary>
        /// Check if battery control is supported
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Get current charge limit (percentage)
        /// </summary>
        int GetChargeLimit();

        /// <summary>
        /// Set charge limit (percentage)
        /// </summary>
        bool SetChargeLimit(int limitPercent);

        /// <summary>
        /// Get minimum allowed limit
        /// </summary>
        int GetMinLimit();

        /// <summary>
        /// Get maximum allowed limit
        /// </summary>
        int GetMaxLimit();
    }

    /// <summary>
    /// Interface for performance mode control
    /// </summary>
    public interface IPerformanceController
    {
        /// <summary>
        /// Check if performance control is supported
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Get current performance mode
        /// </summary>
        PerformanceMode GetCurrentMode();

        /// <summary>
        /// Set performance mode
        /// </summary>
        bool SetMode(PerformanceMode mode);

        /// <summary>
        /// Get available performance modes
        /// </summary>
        PerformanceMode[] GetAvailableModes();
    }

    /// <summary>
    /// Interface for fan control
    /// </summary>
    public interface IFanController
    {
        /// <summary>
        /// Check if fan control is supported
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Get CPU fan speed (RPM)
        /// </summary>
        int GetCpuFanSpeed();

        /// <summary>
        /// Get GPU fan speed (RPM)
        /// </summary>
        int GetGpuFanSpeed();

        /// <summary>
        /// Set fan profile
        /// </summary>
        bool SetFanProfile(FanProfile profile);
    }

    /// <summary>
    /// Interface for temperature monitoring
    /// </summary>
    public interface ITemperatureMonitor
    {
        /// <summary>
        /// Check if temperature monitoring is supported
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Get CPU temperature (Celsius)
        /// </summary>
        int GetCpuTemperature();

        /// <summary>
        /// Get GPU temperature (Celsius)
        /// </summary>
        int GetGpuTemperature();
    }

    /// <summary>
    /// Enumeration of performance modes
    /// </summary>
    public enum PerformanceMode
    {
        Silent = 0,
        Balanced = 1,
        Performance = 2,
        Turbo = 3
    }

    /// <summary>
    /// Enumeration of fan profiles
    /// </summary>
    public enum FanProfile
    {
        Silent,
        Balanced,
        Performance,
        Turbo,
        Manual
    }
}
