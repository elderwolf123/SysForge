using Microsoft.Extensions.Logging;
using RamOptimizer.Core.Interfaces;
using RamOptimizer.HardwareControl;

namespace RamOptimizer.Plugins.Asus;

/// <summary>
/// Hardware controller implementation for ASUS ROG laptops
/// Wraps the legacy AsusAcpiInterface
/// </summary>
public class AsusHardwareController : IHardwareController, ICoreController, IBatteryController, IPerformanceController
{
    private readonly AsusAcpiInterface _acpi;
    private readonly CoreManager _coreManager;
    private readonly ILogger? _logger;
    private bool _disposed;

    public AsusHardwareController(ILogger? logger = null)
    {
        _logger = logger;
        _acpi = new AsusAcpiInterface();
        // Bug #2 Fix: Cache CoreManager to avoid repeated creation
        _coreManager = new CoreManager(_acpi);
    }

    #region IHardwareController

    public bool IsAvailable()
    {
        // Bug #3 Fix: Constructor already initializes, just check connection status
        return _acpi.IsConnected();
    }

    public string GetDeviceIdentifier()
    {
        // Ideally we would query the BIOS or ACPI for the exact model
        return "ASUS ROG Device";
    }

    public string GetDeviceType()
    {
        return "ASUS ROG Laptop";
    }

    public bool Initialize()
    {
        // Already initialized in constructor - just return connection status
        return _acpi.IsConnected();
    }

    #endregion

    #region ICoreController

    public bool IsSupported => true;

    public int GetMaxPCores()
    {
        var (maxP, _) = _coreManager.GetMaxCores();
        return maxP;
    }

    public int GetMaxECores()
    {
        var (_, maxE) = _coreManager.GetMaxCores();
        return maxE;
    }

    public int GetCurrentPCores()
    {
        var (p, _) = _coreManager.GetCurrentCores();
        return p;
    }

    public int GetCurrentECores()
    {
        var (_, e) = _coreManager.GetCurrentCores();
        return e;
    }

    public bool SetCores(int pCores, int eCores)
    {
        int config = (pCores << 8) | eCores;
        return _acpi.DeviceSet(AsusAcpiInterface.CORES_CPU, config) == 1;
    }

    #endregion

    #region IBatteryController

    bool IBatteryController.IsSupported => true;

    public int GetChargeLimit()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsusHardwareController));
            
        try
        {
            int val = _acpi.DeviceGet(AsusAcpiInterface.BatteryLimit);
            int limit = ((val >> 16) & 0xFF) - 36;
            
            // Bug #5 Fix: Sanity check result
            if (limit < 60 || limit > 100)
            {
                _logger?.LogWarning($"Invalid battery limit from ACPI: {limit}, using default 80%");
                return 80;
            }
            
            return limit;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to get charge limit: {ex.Message}");
            return 80; // Default
        }
    }

    public bool SetChargeLimit(int limitPercent)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsusHardwareController));
            
        return _acpi.DeviceSet(AsusAcpiInterface.BatteryLimit, limitPercent + 36) == 1;
    }

    public int GetMinLimit() => 60;
    public int GetMaxLimit() => 100;

    #endregion

    #region IPerformanceController

    bool IPerformanceController.IsSupported => true;

    public PerformanceMode GetCurrentMode()
    {
        int mode = _acpi.DeviceGet(AsusAcpiInterface.PerformanceMode);
        return (PerformanceMode)mode;
    }

    public bool SetMode(PerformanceMode mode)
    {
        return _acpi.DeviceSet(AsusAcpiInterface.PerformanceMode, (int)mode) == 1;
    }

    public PerformanceMode[] GetAvailableModes()
    {
        return new[] 
        { 
            PerformanceMode.Silent, 
            PerformanceMode.Balanced, 
            PerformanceMode.Performance,
            PerformanceMode.Turbo 
        };
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _acpi.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
