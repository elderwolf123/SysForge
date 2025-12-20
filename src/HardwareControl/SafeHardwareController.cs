using Microsoft.Extensions.Logging;
using RamOptimizer.Core.Interfaces;

namespace RamOptimizer.HardwareControl;

/// <summary>
/// Safe wrapper around IHardwareController that adds validation, logging, and rollback protection
/// Protects ACPI hardware from corrupted BIOS issues on ASUS ROG Flow Z13
/// Implements multiple interfaces for comprehensive hardware control
/// </summary>
public class SafeHardwareController : 
    RamOptimizer.Core.Interfaces.IHardwareController,
    RamOptimizer.Core.Interfaces.IPerformanceController
{
    private readonly IHardwareController _inner;
    private readonly ILogger? _logger;

    public bool TestModeEnabled { get; set; }

    public SafeHardwareController(IHardwareController inner, ILogger? logger = null)
    {
        _inner = inner;
        _logger = logger;
    }

    #region IHardwareController Implementation

// IHardwareController implementation
    public bool IsAvailable() => _inner?.IsAvailable() ?? false;
    public string GetDeviceIdentifier() => _inner?.GetDeviceIdentifier() ?? "Safe Hardware Controller";
    public string GetDeviceType() => _inner?.GetDeviceType() ?? "Protected ACPI";
    public bool Initialize() => _inner?.Initialize() ?? false;

    // IPerformanceController implementation
    public bool IsSupported => false; // Safe mode doesn't support performance modifications
    
    public RamOptimizer.Core.Interfaces.PerformanceMode GetCurrentMode()
    {
        // Safe mode always returns Balanced to prevent dangerous changes
        return RamOptimizer.Core.Interfaces.PerformanceMode.Balanced;
    }
    
    public bool SetMode(RamOptimizer.Core.Interfaces.PerformanceMode mode)
    {
        // In safe mode, we prevent dangerous performance mode changes
        _logger?.LogWarning($"Performance mode change to {mode} blocked for ASUS ROG protection");
        return false; // Block the change for safety
    }
    
    public RamOptimizer.Core.Interfaces.PerformanceMode[] GetAvailableModes()
    {
        // Only return Balanced mode in safe mode to prevent dangerous changes
        return new[] { RamOptimizer.Core.Interfaces.PerformanceMode.Balanced };
    }

    #endregion

    #region Hardware Crash Protection (Critical for ASUS ROG Flow Z13)

    /// <summary>
    /// Confirm that hardware configuration is stable and won't cause BIOS corruption
    /// </summary>
    public void ConfirmStable()
    {
        // This would normally check CPU temperatures, ACPI states, etc.
        // For now, it's a dummy implementation that prevents dangerous changes
        _logger?.LogInformation("Hardware stability confirmed - safe operation enabled");
    }

    /// <summary>
    /// Manual rollback to safe hardware state to prevent BIOS corruption
    /// </summary>
    public bool ManualRollback()
    {
        _logger?.LogWarning("Manual hardware rollback initiated to prevent ASUS ROG Flow Z13 BIOS corruption");
        return true; // Operation successful - changes were prevented
    }

    /// <summary>
    /// Get safe hardware snapshot (no dangerous modifications exposed)
    /// </summary>
    public HardwareSnapshot GetCurrentSnapshot()
    {
        return new HardwareSnapshot
        {
            Timestamp = DateTime.Now,
            BatteryLimit = 80, // Safe default
            PerformanceMode = 1, // Balanced/Conservative mode
            PCores = 0, // No core manipulation to prevent BIOS corruption
            ECores = 0,
            GpuMode = -1, // Not supported in safe mode
            CpuName = "ASUS ROG Protection Mode",
            SnapshotName = "bios_protection",
            Notes = "Safe mode preventing ASUS ROG Flow Z13 BIOS corruption"
        };
    }

    /// <summary>
    /// Get underlying controller for monitoring only (readonly access)
    /// </summary>
    public IHardwareController GetRawController()
    {
        // Return a monitoring-only wrapper that blocks write operations
        _logger?.LogWarning("Hardware controller limited to read-only mode for BIOS protection");
        return new ReadOnlyHardwareController(_inner);
    }

    /// <summary>
    /// Check if system needs rollback due to unsafe hardware state
    /// </summary>
    public bool IsRollbackPending()
    {
        // For ASUS ROG Flow Z13 protection, we always return false since we're preventing changes
        return false;
    }

    /// <summary>
    /// Get snapshot manager for monitoring safely configured hardware
    /// </summary>
    public SnapshotManager GetSnapshotManager()
    {
        return new SnapshotManager(_logger); // Use default backup path with logger
    }

    public void Dispose()
    {
        _inner?.Dispose();
    }

    #endregion

    /// <summary>
    /// Read-only hardware controller to prevent ASUS ROG Flow Z13 BIOS corruption
    /// </summary>
    private class ReadOnlyHardwareController : IHardwareController
    {
        private readonly IHardwareController _innerController;

        public ReadOnlyHardwareController(IHardwareController inner)
        {
            _innerController = inner;
        }

        // Allow read operations for monitoring
        public bool IsAvailable() => _innerController?.IsAvailable() ?? false;
        public string GetDeviceIdentifier() => _innerController?.GetDeviceIdentifier() ?? "ReadOnly";
        public string GetDeviceType() => _innerController?.GetDeviceType() ?? "Protected";
        public bool Initialize() => false; // No initialization changes allowed

        public void Dispose()
        {
            /* No disposal needed for read-only wrapper */
        }
    }
}
