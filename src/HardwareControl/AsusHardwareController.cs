using Microsoft.Extensions.Logging;
using RamOptimizer.Core.Interfaces;

namespace RamOptimizer.HardwareControl;

/// <summary>
/// Adapter class that implements IHardwareController for ASUS hardware via AsusAcpiInterface
/// This bridges the gap between the low-level ACPI interface and the generic hardware controller interface
/// </summary>
public class AsusHardwareController : IHardwareController, ICoreController, IBatteryController, IFanController, ITemperatureMonitor
{
    private readonly AsusAcpiInterface _acpi;
    private readonly ILogger? _logger;
    private readonly CoreManager _coreManager;
    private readonly BatteryManager _batteryManager;
    private readonly PerformanceModeManager _performanceManager;
    private bool _disposed = false;

    /// <summary>
    /// Enable DryRun/Test mode - no actual hardware changes will be made
    /// </summary>
    public bool DryRunMode { get; set; }

    public AsusHardwareController(AsusAcpiInterface? acpi = null, ILogger? logger = null)
    {
        _logger = logger;
        _acpi = acpi ?? new AsusAcpiInterface();
        _coreManager = new CoreManager(_acpi);
        _batteryManager = new BatteryManager(_acpi);
        _performanceManager = new PerformanceModeManager(_acpi);
        
        _logger?.LogInformation("AsusHardwareController initialized for {Device}", GetDeviceIdentifier());
    }

    #region IHardwareController Implementation

    public bool IsAvailable()
    {
        return AsusAcpiInterface.IsAvailable();
    }

    public string GetDeviceIdentifier()
    {
        return "ASUS ROG Flow Z13";
    }

    public string GetDeviceType()
    {
        return "ASUS ROG";
    }

    public bool Initialize()
    {
        try
        {
            if (!_acpi.IsConnected())
            {
                _logger?.LogError("ASUS ACPI interface not connected");
                return false;
            }

            _logger?.LogInformation("ASUS Hardware Controller initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize ASUS Hardware Controller");
            return false;
        }
    }

    #endregion

    #region ICoreController Implementation

    public bool IsSupported => true; // ASUS ROG supports core control

    public int GetMaxPCores()
    {
        try
        {
            var (maxP, _) = _coreManager.GetMaxCores();
            return maxP;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get max P-cores");
            return 0;
        }
    }

    public int GetMaxECores()
    {
        try
        {
            var (_, maxE) = _coreManager.GetMaxCores();
            return maxE;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get max E-cores");
            return 0;
        }
    }

    public int GetCurrentPCores()
    {
        try
        {
            var (pCores, _) = _coreManager.GetCurrentCores();
            return pCores;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get current P-cores");
            return 0;
        }
    }

    public int GetCurrentECores()
    {
        try
        {
            var (_, eCores) = _coreManager.GetCurrentCores();
            return eCores;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get current E-cores");
            return 0;
        }
    }

    public bool SetCores(int pCores, int eCores)
    {
        try
        {
            if (DryRunMode)
            {
                _logger?.LogWarning("[DRY RUN] Would set cores to P={PCores}, E={ECores}", pCores, eCores);
                return true;
            }

            // Validate configuration
            var (maxP, maxE) = _coreManager.GetMaxCores();
            var validation = AcpiSafetyValidator.ValidateCoreConfig(pCores, eCores, maxP, maxE, _logger);
            
            if (!validation.IsValid)
            {
                _logger?.LogError("Core configuration validation failed: {Error}", validation.ErrorMessage);
                return false;
            }

            // Format: E-cores in upper 16 bits, P-cores in lower 16 bits (matches CoreManager)
            int coreConfig = (eCores << 16) | pCores;
            _logger?.LogInformation("Setting core configuration: P={PCores}, E={ECores} (0x{Config:X8})", pCores, eCores, coreConfig);
            
            int result = _acpi.DeviceSet(AsusAcpiInterface.CORES_CPU, coreConfig);
            
            if (result != 1)
            {
                _logger?.LogError("Failed to set core configuration. ACPI returned: {Result}", result);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception while setting cores");
            return false;
        }
    }

    #endregion

    #region IBatteryController Implementation

    bool IBatteryController.IsSupported => true; // ASUS ROG supports battery control

    public int GetChargeLimit()
    {
        try
        {
            return _batteryManager.GetChargeLimit();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get battery charge limit");
            return 100; // Default to full charge
        }
    }

    public bool SetChargeLimit(int limitPercent)
    {
        try
        {
            if (DryRunMode)
            {
                _logger?.LogWarning("[DRY RUN] Would set battery limit to {Limit}%", limitPercent);
                return true;
            }

            var validation = AcpiSafetyValidator.ValidateBatteryLimit(limitPercent, _logger);
            if (!validation.IsValid)
            {
                _logger?.LogError("Battery limit validation failed: {Error}", validation.ErrorMessage);
                return false;
            }

            _logger?.LogInformation("Setting battery charge limit to {Limit}%", limitPercent);
            _batteryManager.SetChargeLimit(limitPercent);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set battery charge limit");
            return false;
        }
    }

    public int GetMinLimit() => 60; // ASUS ROG minimum is 60%

    public int GetMaxLimit() => 100; // ASUS ROG maximum is 100%

    #endregion

    #region Performance Controller Access (via property to avoid enum conflict)

    /// <summary>
    /// Access performance controller functionality (avoids enum naming conflict)
    /// </summary>
    public IPerformanceController PerformanceController => new AsusPerformanceControllerAdapter(this);

    private class AsusPerformanceControllerAdapter : IPerformanceController
    {
        private readonly AsusHardwareController _parent;

        public AsusPerformanceControllerAdapter(AsusHardwareController parent)
        {
            _parent = parent;
        }

        public bool IsSupported => true;

        public RamOptimizer.Core.Interfaces.PerformanceMode GetCurrentMode()
        {
            try
            {
                var asusMode = _parent._performanceManager.GetCurrentMode();
                return _parent.MapFromAsusMode(asusMode);
            }
            catch (Exception ex)
            {
                _parent._logger?.LogError(ex, "Failed to get current performance mode");
                return RamOptimizer.Core.Interfaces.PerformanceMode.Balanced;
            }
        }

        public bool SetMode(RamOptimizer.Core.Interfaces.PerformanceMode mode)
        {
            try
            {
                if (_parent.DryRunMode)
                {
                    _parent._logger?.LogWarning("[DRY RUN] Would set performance mode to {Mode}", mode);
                    return true;
                }

                var asusMode = _parent.MapToAsusMode(mode);
                int modeValue = (int)asusMode;
                
                var validation = AcpiSafetyValidator.ValidatePerformanceMode(modeValue, _parent._logger);
                
                if (!validation.IsValid)
                {
                    _parent._logger?.LogError("Performance mode validation failed: {Error}", validation.ErrorMessage);
                    return false;
                }

                _parent._logger?.LogInformation("Setting performance mode to {Mode} (ASUS mode: {AsusMode})", mode, asusMode);
                _parent._performanceManager.SetMode(asusMode);
                return true;
            }
            catch (Exception ex)
            {
                _parent._logger?.LogError(ex, "Failed to set performance mode");
                return false;
            }
        }

        public RamOptimizer.Core.Interfaces.PerformanceMode[] GetAvailableModes()
        {
            return new[] {
                RamOptimizer.Core.Interfaces.PerformanceMode.Silent,
                RamOptimizer.Core.Interfaces.PerformanceMode.Balanced,
                RamOptimizer.Core.Interfaces.PerformanceMode.Performance
            };
        }
    }

    #endregion

    #region IFanController Implementation

    bool IFanController.IsSupported => true; // ASUS ROG has fan monitoring

    public int GetCpuFanSpeed()
    {
        try
        {
            return _acpi.DeviceGet(AsusAcpiInterface.CPU_Fan);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get CPU fan speed");
            return 0;
        }
    }

    public int GetGpuFanSpeed()
    {
        try
        {
            return _acpi.DeviceGet(AsusAcpiInterface.GPU_Fan);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get GPU fan speed");
            return 0;
        }
    }

    public bool SetFanProfile(FanProfile profile)
    {
        if (DryRunMode)
        {
            _logger?.LogWarning("[DRY RUN] Would set fan profile to {Profile}", profile);
            return true;
        }

        _logger?.LogWarning("Fan profile control not yet implemented for ASUS ROG");
        return false;
    }

    #endregion

    #region ITemperatureMonitor Implementation

    bool ITemperatureMonitor.IsSupported => true; // ASUS ROG has temperature sensors

    public int GetCpuTemperature()
    {
        try
        {
            return _acpi.DeviceGet(AsusAcpiInterface.Temp_CPU);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get CPU temperature");
            return 0;
        }
    }

    public int GetGpuTemperature()
    {
        try
        {
            return _acpi.DeviceGet(AsusAcpiInterface.Temp_GPU);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get GPU temperature");
            return 0;
        }
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        if (!_disposed)
        {
            _acpi?.Dispose();
            _disposed = true;
            _logger?.LogInformation("AsusHardwareController disposed");
        }
        GC.SuppressFinalize(this);
    }

    #endregion

    /// <summary>
    /// Get the underlying ACPI interface (for advanced operations)
    /// </summary>
    public AsusAcpiInterface GetAcpiInterface() => _acpi;

    #region Performance Mode Mapping

    /// <summary>
    /// Map from ASUS PerformanceMode (local enum) to Core.Interfaces.PerformanceMode
    /// </summary>
    private RamOptimizer.Core.Interfaces.PerformanceMode MapFromAsusMode(RamOptimizer.HardwareControl.PerformanceMode asusMode)
    {
        return asusMode switch
        {
            RamOptimizer.HardwareControl.PerformanceMode.Silent => RamOptimizer.Core.Interfaces.PerformanceMode.Silent,
            RamOptimizer.HardwareControl.PerformanceMode.Balanced => RamOptimizer.Core.Interfaces.PerformanceMode.Balanced,
            RamOptimizer.HardwareControl.PerformanceMode.Turbo => RamOptimizer.Core.Interfaces.PerformanceMode.Performance,
            _ => RamOptimizer.Core.Interfaces.PerformanceMode.Balanced
        };
    }

    /// <summary>
    /// Map from Core.Interfaces.PerformanceMode to ASUS PerformanceMode (local enum)
    /// </summary>
    private RamOptimizer.HardwareControl.PerformanceMode MapToAsusMode(RamOptimizer.Core.Interfaces.PerformanceMode coreMode)
    {
        return coreMode switch
        {
            RamOptimizer.Core.Interfaces.PerformanceMode.Silent => RamOptimizer.HardwareControl.PerformanceMode.Silent,
            RamOptimizer.Core.Interfaces.PerformanceMode.Balanced => RamOptimizer.HardwareControl.PerformanceMode.Balanced,
            RamOptimizer.Core.Interfaces.PerformanceMode.Performance => RamOptimizer.HardwareControl.PerformanceMode.Turbo,
            RamOptimizer.Core.Interfaces.PerformanceMode.Turbo => RamOptimizer.HardwareControl.PerformanceMode.Turbo, // Map Turbo to Turbo as well
            _ => RamOptimizer.HardwareControl.PerformanceMode.Balanced
        };
    }

    #endregion
}