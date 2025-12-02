using System;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.Core.Interfaces
{
    /// <summary>
    /// Interface for hardware snapshot operations
    /// Device-agnostic - works with any hardware controller
    /// </summary>
    public interface IHardwareSnapshot
    {
        /// <summary>
        /// Unique identifier for this snapshot
        /// </summary>
        string Id { get; }

        /// <summary>
        /// When this snapshot was created
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Description of this snapshot
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Device identifier this snapshot is for
        /// </summary>
        string DeviceIdentifier { get; }

        /// <summary>
        /// Capture current hardware state
        /// </summary>
        void Capture(IHardwareController controller);

        /// <summary>
        /// Apply this snapshot to hardware
        /// </summary>
        bool ApplyTo(IHardwareController controller);

        /// <summary>
        /// Serialize to JSON
        /// </summary>
        string ToJson();

        /// <summary>
        /// Deserialize from JSON
        /// </summary>
        void FromJson(string json);
    }

    /// <summary>
    /// Interface for snapshot management
    /// </summary>
    public interface ISnapshotManager
    {
        /// <summary>
        /// Capture and save a new snapshot
        /// </summary>
        IHardwareSnapshot CaptureAndSave(
            IHardwareController controller,
            string name,
            string description);

        /// <summary>
        /// Load a snapshot by name
        /// </summary>
        IHardwareSnapshot LoadSnapshot(string name);

        /// <summary>
        /// Load the latest snapshot
        /// </summary>
        IHardwareSnapshot LoadLatestSnapshot();

        /// <summary>
        /// Restore a snapshot to hardware
        /// </summary>
        bool RestoreSnapshot(string name, IHardwareController controller);

        /// <summary>
        /// List all available snapshots
        /// </summary>
        string[] ListSnapshots();

        /// <summary>
        /// Delete a snapshot
        /// </summary>
        bool DeleteSnapshot(string name);

        /// <summary>
        /// Cleanup old snapshots (keep last N)
        /// </summary>
        void CleanupOldSnapshots(int keepCount);
    }

    /// <summary>
    /// Interface for validation of hardware changes
    /// </summary>
    public interface IHardwareValidator
    {
        /// <summary>
        /// Validate core configuration
        /// </summary>
        (bool IsValid, string ErrorMessage) ValidateCoreConfig(
            int pCores, int eCores, int maxP, int maxE);

        /// <summary>
        /// Validate battery limit
        /// </summary>
        (bool IsValid, string ErrorMessage) ValidateBatteryLimit(
            int limit, int min, int max);

        /// <summary>
        /// Validate performance mode
        /// </summary>
        (bool IsValid, string ErrorMessage) ValidatePerformanceMode(
            PerformanceMode mode, PerformanceMode[] availableModes);

        /// <summary>
        /// Check if a configuration is explicitly forbidden
        /// </summary>
        bool IsConfigurationForbidden(string configType, object value);
    }

    /// <summary>
    /// Interface for rollback protection
    /// </summary>
    public interface IRollbackProtection
    {
        /// <summary>
        /// Set rollback flag (change pending confirmation)
        /// </summary>
        void SetRollbackFlag(string changeDescription);

        /// <summary>
        /// Clear rollback flag (change confirmed stable)
        /// </summary>
        void ClearRollbackFlag();

        /// <summary>
        /// Check if rollback is pending
        /// </summary>
        bool IsRollbackPending();

        /// <summary>
        /// Get current boot count
        /// </summary>
        int GetBootCount();

        /// <summary>
        /// Increment boot count
        /// </summary>
        void IncrementBootCount();

        /// <summary>
        /// Check and perform rollback if needed
        /// </summary>
        bool CheckAndRollback(
            IHardwareController controller,
            ISnapshotManager snapshotManager,
            ILogger logger);
    }

    /// <summary>
    /// Interface for safe hardware operations
    /// Combines all safety features: validation, snapshots, rollback
    /// </summary>
    public interface ISafeHardwareController : IHardwareController
    {
        /// <summary>
        /// Test mode - simulates changes without writing
        /// </summary>
        bool TestModeEnabled { get; set; }

        /// <summary>
        /// Get the underlying hardware controller
        /// </summary>
        IHardwareController GetRawController();

        /// <summary>
        /// Get the validator
        /// </summary>
        IHardwareValidator GetValidator();

        /// <summary>
        /// Get the snapshot manager
        /// </summary>
        ISnapshotManager GetSnapshotManager();

        /// <summary>
        /// Get the rollback protection
        /// </summary>
        IRollbackProtection GetRollbackProtection();

        /// <summary>
        /// Confirm system is stable (clears rollback flag)
        /// </summary>
        void ConfirmStable();

        /// <summary>
        /// Manually trigger rollback
        /// </summary>
        bool ManualRollback();

        /// <summary>
        /// Get current snapshot of hardware state
        /// </summary>
        IHardwareSnapshot GetCurrentSnapshot();
    }
}
