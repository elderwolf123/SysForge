using Microsoft.Extensions.Logging;

namespace RamOptimizer.HardwareControl;

/// <summary>
/// Safe wrapper around AsusAcpiInterface that adds validation, logging, and rollback protection
/// Use this instead of calling AsusAcpiInterface directly
/// </summary>
[Obsolete("Use SafeHardwareController instead. This class is deprecated and will be removed in future versions.")]
public class SafeAcpiInterface : IDisposable
{
    private readonly AsusAcpiInterface _acpi;
    private readonly SnapshotManager _snapshotManager;
    private readonly SafeModeRollback _rollback;
    private readonly ILogger? _logger;
    private bool _disposed;

    public bool TestModeEnabled { get; set; }

    public SafeAcpiInterface(ILogger? logger = null)
    {
        _logger = logger;
        _acpi = new AsusAcpiInterface();
        _snapshotManager = new SnapshotManager(logger: logger);
        _rollback = new SafeModeRollback(logger: logger);

        // Check for pending rollback on initialization
        if (_rollback.IsRollbackPending())
        {
            _logger?.LogWarning("Pending rollback detected on initialization");
            bool rolledBack = _rollback.CheckAndRollback(_acpi, _snapshotManager);
            if (rolledBack)
            {
                _logger?.LogWarning("System was rolled back to previous configuration");
            }
        }
    }

    /// <summary>
    /// Safely set P/E core configuration with full validation and rollback protection
    /// </summary>
    public bool SetCores(int pCores, int eCores, string? changeDescription = null)
    {
        try
        {
            var coreManager = new CoreManager(_acpi);
            var (maxP, maxE) = coreManager.GetMaxCores();

            // Validate configuration
            var validation = AcpiSafetyValidator.ValidateCoreConfig(pCores, eCores, maxP, maxE, _logger);
            if (!validation.IsValid)
            {
                _logger?.LogError($"Core configuration validation failed: {validation.ErrorMessage}");
                return false;
            }

            // Capture snapshot before change
            var description = changeDescription ?? $"Core change: P={pCores}, E={eCores}";
            _logger?.LogInformation($"Capturing snapshot before change: {description}");
            _snapshotManager.CaptureAndSave(_acpi, "before_core_change", description);

            // Set rollback flag
            _rollback.SetRollbackFlag(description);

            if (TestModeEnabled)
            {
                _logger?.LogWarning($"[TEST MODE] Would set cores to P={pCores}, E={eCores}");
                return true;
            }

            // Get current configuration for rollback
            var (origP, origE) = coreManager.GetCurrentCores();

            // Apply change
            int coreConfig = (pCores << 8) | eCores;
            _logger?.LogInformation($"Setting core configuration: 0x{coreConfig:X4}");
            int result = _acpi.DeviceSet(AsusAcpiInterface.CORES_CPU, coreConfig);

            if (result != 1)
            {
                _logger?.LogError($"ACPI DeviceSet failed with result: {result}");
                _rollback.ClearRollbackFlag(); // Clear flag since we didn't actually change anything
                return false;
            }

            // Verify write
            System.Threading.Thread.Sleep(100);
            var (newP, newE) = coreManager.GetCurrentCores();

            if (newP != pCores || newE != eCores)
            {
                _logger?.LogError($"Verification failed. Expected P={pCores},E={eCores}, Got P={newP},E={newE}");

                // Attempt immediate rollback
                _logger?.LogWarning("Attempting immediate rollback");
                _acpi.DeviceSet(AsusAcpiInterface.CORES_CPU, (origP << 8) | origE);
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

    /// <summary>
    /// Safely set battery charge limit with validation
    /// </summary>
    public bool SetBatteryLimit(int limit, string? changeDescription = null)
    {
        try
        {
            // Validate
            var validation = AcpiSafetyValidator.ValidateBatteryLimit(limit, _logger);
            if (!validation.IsValid)
            {
                _logger?.LogError($"Battery limit validation failed: {validation.ErrorMessage}");
                return false;
            }

            // Capture snapshot
            var description = changeDescription ?? $"Battery limit change: {limit}%";
            _snapshotManager.CaptureAndSave(_acpi, "before_battery_change", description);

            // Set rollback flag (less critical than core changes, but still protected)
            _rollback.SetRollbackFlag(description);

            if (TestModeEnabled)
            {
                _logger?.LogWarning($"[TEST MODE] Would set battery limit to {limit}%");
                return true;
            }

            // Apply change
            _logger?.LogInformation($"Setting battery limit to {limit}%");
            var batteryManager = new BatteryManager(_acpi);
            batteryManager.SetChargeLimit(limit);

            // Verify
            System.Threading.Thread.Sleep(100);
            int newLimit = batteryManager.GetChargeLimit();

            if (newLimit != limit)
            {
                _logger?.LogWarning($"Battery limit verification mismatch. Expected {limit}%, Got {newLimit}%");
                // Battery limit mismatches are less critical, may be due to reading delay
            }

            _logger?.LogInformation($"Battery limit set successfully to {limit}%");

            // Clear rollback flag after short delay (battery changes take effect immediately)
            Task.Delay(5000).ContinueWith(_ => _rollback.ClearRollbackFlag());

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to set battery limit: {ex.Message}");
            _rollback.ClearRollbackFlag();
            return false;
        }
    }

    /// <summary>
    /// Safely set performance mode with validation
    /// </summary>
    public bool SetPerformanceMode(int mode, string? changeDescription = null)
    {
        try
        {
            // Validate
            var validation = AcpiSafetyValidator.ValidatePerformanceMode(mode, _logger);
            if (!validation.IsValid)
            {
                _logger?.LogError($"Performance mode validation failed: {validation.ErrorMessage}");
                return false;
            }

            if (TestModeEnabled)
            {
                _logger?.LogWarning($"[TEST MODE] Would set performance mode to {mode}");
                return true;
            }

            // Capture snapshot
            var description = changeDescription ?? $"Performance mode change: {mode}";
            _snapshotManager.CaptureAndSave(_acpi, "before_perf_change", description);

            // Performance mode changes are low risk, no rollback flag needed
            _logger?.LogInformation($"Setting performance mode to {mode}");
            _acpi.DeviceSet(AsusAcpiInterface.PerformanceMode, mode);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to set performance mode: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Confirm that the current configuration is stable (clears rollback flag)
    /// </summary>
    public void ConfirmStable()
    {
        _rollback.ClearRollbackFlag();
        _snapshotManager.CaptureAndSave(_acpi, "stable", "User confirmed stable");
        _logger?.LogInformation("Configuration confirmed stable");
    }

    /// <summary>
    /// Manually trigger rollback to last known good configuration
    /// </summary>
    public bool ManualRollback()
    {
        _logger?.LogWarning("Manual rollback requested");
        return _rollback.CheckAndRollback(_acpi, _snapshotManager);
    }

    /// <summary>
    /// Get current hardware status
    /// </summary>
    public HardwareSnapshot GetCurrentSnapshot()
    {
        return HardwareSnapshot.Capture(_acpi, "current");
    }

    /// <summary>
    /// Get the underlying ACPI interface (use with caution)
    /// </summary>
    public AsusAcpiInterface GetRawInterface()
    {
        _logger?.LogWarning("Direct ACPI interface access - safety features bypassed");
        return _acpi;
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
            _acpi?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
