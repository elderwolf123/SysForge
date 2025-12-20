using Microsoft.Extensions.Logging;

namespace RamOptimizer.HardwareControl;

/// <summary>
/// Detects system instability and automatically rolls back to last known good configuration
/// Uses boot counting and rollback flags to detect failed changes
/// </summary>
public class SafeModeRollback
{
    private readonly string _rollbackFlagPath;
    private readonly string _bootCountPath;
    private readonly string _lastChangePath;
    private readonly ILogger? _logger;

    private const int REQUIRED_SUCCESSFUL_BOOTS = 2;

    public SafeModeRollback(string? dataPath = null, ILogger? logger = null)
    {
        var basePath = dataPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "RamOptimizer", "Safety");

        Directory.CreateDirectory(basePath);

        _rollbackFlagPath = Path.Combine(basePath, "rollback_needed.flag");
        _bootCountPath = Path.Combine(basePath, "boot_count.txt");
        _lastChangePath = Path.Combine(basePath, "last_change.txt");
        _logger = logger;
    }

    /// <summary>
    /// Call this BEFORE making any ACPI changes to enable rollback protection
    /// </summary>
    public void SetRollbackFlag(string changeDescription = "")
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.WriteAllText(_rollbackFlagPath, timestamp);
            File.WriteAllText(_bootCountPath, "0");

            if (!string.IsNullOrEmpty(changeDescription))
            {
                File.WriteAllText(_lastChangePath, $"{timestamp} | {changeDescription}");
            }

            _logger?.LogInformation("Rollback flag set - protection enabled");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to set rollback flag: {ex.Message}");
        }
    }

    /// <summary>
    /// Call this AFTER successful boot AND user confirms system is stable
    /// </summary>
    public void ClearRollbackFlag()
    {
        try
        {
            if (File.Exists(_rollbackFlagPath))
                File.Delete(_rollbackFlagPath);

            if (File.Exists(_bootCountPath))
                File.Delete(_bootCountPath);

            _logger?.LogInformation("Rollback flag cleared - change confirmed stable");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to clear rollback flag: {ex.Message}");
        }
    }

    /// <summary>
    /// Call this on every application startup to check if rollback is needed
    /// Returns true if rollback was performed
    /// </summary>
    public bool CheckAndRollback(RamOptimizer.Core.Interfaces.IHardwareController controller, SnapshotManager snapshotManager)
    {
        // Bug #3 Fix: Add null checks for parameters
        if (controller == null)
        {
            _logger?.LogError("Cannot perform rollback: controller is null");
            return false;
        }

        if (snapshotManager == null)
        {
            _logger?.LogError("Cannot perform rollback: snapshotManager is null");
            return false;
        }

        if (!File.Exists(_rollbackFlagPath))
        {
            // No rollback needed
            return false;
        }

        try
        {
            // Increment boot count
            int bootCount = GetBootCount();
            bootCount++;
            File.WriteAllText(_bootCountPath, bootCount.ToString());

            _logger?.LogWarning($"Rollback flag detected. Boot count: {bootCount}/{REQUIRED_SUCCESSFUL_BOOTS}");

            // If we've booted successfully multiple times, the change is stable
            if (bootCount >= REQUIRED_SUCCESSFUL_BOOTS)
            {
                _logger?.LogInformation("Multiple successful boots detected - change appears stable");
                ClearRollbackFlag();
                return false;
            }

            // Rollback is needed
            _logger?.LogWarning("System instability detected - performing rollback");

            var lastChange = GetLastChangeDescription();
            if (!string.IsNullOrEmpty(lastChange))
            {
                _logger?.LogWarning($"Last change before instability: {lastChange}");
            }

            // Attempt to restore last known good configuration
            var snapshot = snapshotManager.LoadLatestSnapshot();
            if (snapshot != null)
            {
                _logger?.LogWarning($"Restoring snapshot: {snapshot}");
                bool success = snapshot.ApplyTo(controller, _logger);

                if (success)
                {
                    _logger?.LogWarning("Rollback completed successfully");
                    ClearRollbackFlag();
                    return true;
                }
                else
                {
                    _logger?.LogError("Rollback failed - manual intervention required");
                }
            }
            else
            {
                _logger?.LogError("No snapshot available for rollback - manual intervention required");
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Rollback check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if rollback flag is set (without performing rollback)
    /// </summary>
    public bool IsRollbackPending()
    {
        return File.Exists(_rollbackFlagPath);
    }

    /// <summary>
    /// Get current boot count
    /// </summary>
    public int GetBootCount()
    {
        try
        {
            if (File.Exists(_bootCountPath))
            {
                var content = File.ReadAllText(_bootCountPath);
                if (int.TryParse(content, out int count))
                    return count;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Failed to read boot count: {ex.Message}");
        }

        return 0;
    }

    /// <summary>
    /// Get description of last change that triggered rollback protection
    /// </summary>
    public string GetLastChangeDescription()
    {
        try
        {
            if (File.Exists(_lastChangePath))
                return File.ReadAllText(_lastChangePath);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Failed to read last change description: {ex.Message}");
        }

        return string.Empty;
    }

    /// <summary>
    /// Get timestamp when rollback flag was set
    /// </summary>
    public DateTime? GetRollbackFlagTimestamp()
    {
        try
        {
            if (File.Exists(_rollbackFlagPath))
            {
                var content = File.ReadAllText(_rollbackFlagPath);
                if (DateTime.TryParse(content, out DateTime timestamp))
                    return timestamp;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Failed to read rollback timestamp: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Force rollback flag (for testing or manual intervention)
    /// </summary>
    public void ForceRollback()
    {
        SetRollbackFlag("Forced rollback");
        File.WriteAllText(_bootCountPath, "10"); // High count to trigger immediate rollback
        _logger?.LogWarning("Forced rollback flag set");
    }

    /// <summary>
    /// Reset all rollback state (for testing or after manual fix)
    /// </summary>
    public void ResetRollbackState()
    {
        ClearRollbackFlag();
        if (File.Exists(_lastChangePath))
            File.Delete(_lastChangePath);

        _logger?.LogInformation("Rollback state reset");
    }
}
