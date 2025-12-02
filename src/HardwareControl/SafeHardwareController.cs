using Microsoft.Extensions.Logging;
using RamOptimizer.Core.Interfaces;

namespace RamOptimizer.HardwareControl;

/// <summary>
/// Safe wrapper around IHardwareController that adds validation, logging, and rollback protection
/// Replaces the legacy SafeAcpiInterface
/// </summary>
public class SafeHardwareController : IHardwareController, ICoreController, IBatteryController, IPerformanceController
{
    private readonly IHardwareController _inner;
    private readonly SnapshotManager _snapshotManager;
    private readonly SafeModeRollback _rollback;
    private readonly ILogger? _logger;
    private bool _disposed;

    public bool TestModeEnabled { get; set; }

    public SafeHardwareController(IHardwareController inner, ILogger? logger = null)
    {
        _inner = inner;
        _logger = logger;
        _snapshotManager = new SnapshotManager(logger: logger);
        _rollback = new SafeModeRollback(logger: logger);

        // Check for pending rollback on initialization
        if (_rollback.IsRollbackPending())
        {
            _logger?.LogWarning("Pending rollback detected on initialization");
            bool rolledBack = _rollback.CheckAndRollback(_inner, _snapshotManager);
            if (rolledBack)
            {
                _logger?.LogWarning("System was rolled back to previous configuration");
            }
        }
    }

    #region IHardwareController Implementation

    public bool IsAvailable() => _inner.IsAvailable();
    public string GetDeviceIdentifier() => _inner.GetDeviceIdentifier();
    public string GetDeviceType() => _inner.GetDeviceType();
    public bool Initialize() => _inner.Initialize();

    #endregion

    #region ICoreController Implementation

    public bool IsSupported => (_inner as ICoreController)?.IsSupported ?? false;

    public int GetMaxPCores() => (_inner as ICoreController)?.GetMaxPCores() ?? 0;
    public int GetMaxECores() => (_inner as ICoreController)?.GetMaxECores() ?? 0;
    public int GetCurrentPCores() => (_inner as ICoreController)?.GetCurrentPCores() ?? 0;
    public int GetCurrentECores() => (_inner as ICoreController)?.GetCurrentECores() ?? 0;

    public bool SetCores(int pCores, int eCores)
    {
        return SetCores(pCores, eCores, null);
    }

    /// <summary>
    /// Safely set P/E core configuration with full validation and rollback protection
    /// </summary>
    public bool SetCores(int pCores, int eCores, string? changeDescription = null)
    {
        // Bug #6 Fix: Check if disposed
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SafeHardwareController));
        }

        var coreCtrl = _inner as ICoreController;
        if (coreCtrl == null || !coreCtrl.IsSupported)
        {
            _logger?.LogError("Core control not supported on this device");
            return false;
        }

        try
        {
            int maxP = coreCtrl.GetMaxPCores();
            int maxE = coreCtrl.GetMaxECores();

            // Validate configuration
            var validation = AcpiSafetyValidator.ValidateCoreConfig(pCores, eCores, maxP, maxE, _logger);
            if (!validation.IsValid)
            {
                _logger?.LogError($"Core configuration validation failed: {validation.ErrorMessage}");
                return false;
            }

            var description = changeDescription ?? $"Core change: P={pCores}, E={eCores}";

            // Bug #7 Fix: Don't set rollback flag in test mode
            if (TestModeEnabled)
            {
                _logger?.LogWarning($"[TEST MODE] Would set cores to P={pCores}, E={eCores}");
                return true;
            }

            // Bug #5 Fix: Set rollback flag BEFORE capturing snapshot
            _rollback.SetRollbackFlag(description);

            // Capture snapshot before change
            _logger?.LogInformation($"Capturing snapshot before change: {description}");
            _snapshotManager.CaptureAndSave(_inner, "before_core_change", description);

            // Get current configuration for rollback
            int origP = coreCtrl.GetCurrentPCores();
            int origE = coreCtrl.GetCurrentECores();

            // Apply change
            _logger?.LogInformation($"Setting core configuration: P={pCores}, E={eCores}");
            bool result = coreCtrl.SetCores(pCores, eCores);

            if (!result)
            {
                _logger?.LogError($"Device SetCores failed");
                _rollback.ClearRollbackFlag(); // Clear flag since we didn't actually change anything
                return false;
            }

            // Verify write
            // Bug #4 Fix: Use constant instead of magic number
            System.Threading.Thread.Sleep(AcpiConstants.ACPI_WRITE_DELAY_MS);
            int newP = coreCtrl.GetCurrentPCores();
            int newE = coreCtrl.GetCurrentECores();

            if (newP != pCores || newE != eCores)
            {
                _logger?.LogError($"Verification failed. Expected P={pCores},E={eCores}, Got P={newP},E={newE}");

                // Attempt immediate rollback
                _logger?.LogWarning("Attempting immediate rollback");
                coreCtrl.SetCores(origP, origE);
                _rollback.ClearRollbackFlag();
                return false;
            }

            _logger?.LogInformation($"Core configuration applied successfully. Reboot required to take effect.");
            _logger?.LogWarning("IMPORTANT: After reboot, confirm system stability. If system is unstable, automatic rollback will occur.");

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to set cores: {ex.Message}");
            _rollback.ClearRollbackFlag();
            return false;
        }
    }

    #endregion

    #region IBatteryController Implementation

    // Explicit implementation to avoid ambiguity if multiple interfaces have same property name (unlikely here but good practice)
    bool IBatteryController.IsSupported => (_inner as IBatteryController)?.IsSupported ?? false;

    public int GetChargeLimit() => (_inner as IBatteryController)?.GetChargeLimit() ?? 0;
    public int GetMinLimit() => (_inner as IBatteryController)?.GetMinLimit() ?? 0;
    public int GetMaxLimit() => (_inner as IBatteryController)?.GetMaxLimit() ?? 0;

    public bool SetChargeLimit(int limitPercent)
    {
        return SetChargeLimit(limitPercent, null);
    }

    public bool SetChargeLimit(int limit, string? changeDescription = null)
    {
        // Bug #6 Fix: Check if disposed
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SafeHardwareController));
        }

        var batCtrl = _inner as IBatteryController;
        if (batCtrl == null || !batCtrl.IsSupported)
        {
            _logger?.LogError("Battery control not supported");
            return false;
        }

        try
        {
            // Validate
            var validation = AcpiSafetyValidator.ValidateBatteryLimit(limit, _logger);
            if (!validation.IsValid)
            {
                _logger?.LogError($"Battery limit validation failed: {validation.ErrorMessage}");
                return false;
            }

            var description = changeDescription ?? $"Battery limit change: {limit}%";

            // Bug #7 Fix: Don't set rollback flag in test mode
            if (TestModeEnabled)
            {
                _logger?.LogWarning($"[TEST MODE] Would set battery limit to {limit}%");
                return true;
            }

            // Set rollback flag BEFORE capturing snapshot
            _rollback.SetRollbackFlag(description);

            // Capture snapshot
            _snapshotManager.CaptureAndSave(_inner, "before_battery_change", description);

            // Apply change
            _logger?.LogInformation($"Setting battery limit to {limit}%");
            bool result = batCtrl.SetChargeLimit(limit);

            if (!result)
            {
                _logger?.LogError("Failed to set battery limit");
                _rollback.ClearRollbackFlag();
                return false;
            }

            // Verify
            // Bug #4 Fix: Use constant instead of magic number
            System.Threading.Thread.Sleep(AcpiConstants.ACPI_WRITE_DELAY_MS);
            int newLimit = batCtrl.GetChargeLimit();

            if (newLimit != limit)
            {
                _logger?.LogWarning($"Battery limit verification mismatch. Expected {limit}%, Got {newLimit}%");
                // Battery limit mismatches are less critical, may be due to reading delay
            }

            _logger?.LogInformation($"Battery limit set successfully to {limit}%");

            // Bug #1 Fix: Safe background task for clearing rollback flag
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(AcpiConstants.BATTERY_CONFIRM_DELAY_MS);
                    if (!_disposed)
                    {
                        _rollback.ClearRollbackFlag();
                        _logger?.LogInformation("Battery rollback flag auto-cleared after successful application");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Failed to clear battery rollback flag: {ex.Message}");
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to set battery limit: {ex.Message}");
            _rollback.ClearRollbackFlag();
            return false;
        }
    }

    #endregion

    #region IPerformanceController Implementation

    bool IPerformanceController.IsSupported => (_inner as IPerformanceController)?.IsSupported ?? false;

    public PerformanceMode GetCurrentMode() => (_inner as IPerformanceController)?.GetCurrentMode() ?? PerformanceMode.Balanced;
    public PerformanceMode[] GetAvailableModes() => (_inner as IPerformanceController)?.GetAvailableModes() ?? Array.Empty<PerformanceMode>();

    public bool SetMode(PerformanceMode mode)
    {
        return SetPerformanceMode((int)mode, null);
    }

    public bool SetPerformanceMode(int modeInt, string? changeDescription = null)
    {
        // Bug #6 Fix: Check if disposed
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SafeHardwareController));
        }

        var perfCtrl = _inner as IPerformanceController;
        if (perfCtrl == null || !perfCtrl.IsSupported)
        {
            _logger?.LogError("Performance control not supported");
            return false;
        }

        try
        {
            // Validate
            var validation = AcpiSafetyValidator.ValidatePerformanceMode(modeInt, _logger);
            if (!validation.IsValid)
            {
                _logger?.LogError($"Performance mode validation failed: {validation.ErrorMessage}");
                return false;
            }

            if (TestModeEnabled)
            {
                _logger?.LogWarning($"[TEST MODE] Would set performance mode to {modeInt}");
                return true;
            }

            // Capture snapshot
            var description = changeDescription ?? $"Performance mode change: {modeInt}";
            _snapshotManager.CaptureAndSave(_inner, "before_perf_change", description);

            // Performance mode changes are low risk, no rollback flag needed
            _logger?.LogInformation($"Setting performance mode to {modeInt}");
            // Bug #2 Fix: Add verification for performance mode
            bool result = perfCtrl.SetMode((PerformanceMode)modeInt);
            
            if (!result)
            {
                _logger?.LogError("Failed to set performance mode");
                return false;
            }

            // Verify the change
            System.Threading.Thread.Sleep(AcpiConstants.ACPI_WRITE_DELAY_MS);
            var currentMode = perfCtrl.GetCurrentMode();
            if ((int)currentMode != modeInt)
            {
                _logger?.LogWarning($"Performance mode verification mismatch. Expected {modeInt}, got {(int)currentMode}");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to set performance mode: {ex.Message}");
            return false;
        }
    }

    #endregion

    /// <summary>
    /// Confirm that the current configuration is stable (clears rollback flag)
    /// </summary>
    public void ConfirmStable()
    {
        _rollback.ClearRollbackFlag();
        _snapshotManager.CaptureAndSave(_inner, "stable", "User confirmed stable");
        _logger?.LogInformation("Configuration confirmed stable");
    }

    /// <summary>
    /// Manually trigger rollback to last known good configuration
    /// </summary>
    public bool ManualRollback()
    {
        _logger?.LogWarning("Manual rollback requested");
        return _rollback.CheckAndRollback(_inner, _snapshotManager);
    }

    /// <summary>
    /// Get current hardware status
    /// </summary>
    public HardwareSnapshot GetCurrentSnapshot()
    {
        return HardwareSnapshot.Capture(_inner, "current");
    }

    /// <summary>
    /// Get the underlying controller (use with caution)
    /// </summary>
    public IHardwareController GetRawController()
    {
        _logger?.LogWarning("Direct controller access - safety features bypassed");
        return _inner;
    }

    /// <summary>
    /// Check if rollback is pending
    /// </summary>
    public bool IsRollbackPending()
    {
        return _rollback.IsRollbackPending();
    }

    /// <summary>
    /// Get snapshot manager for advanced operations
    /// </summary>
    public SnapshotManager GetSnapshotManager()
    {
        return _snapshotManager;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _inner.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
