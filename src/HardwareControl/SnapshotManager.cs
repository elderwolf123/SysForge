using Microsoft.Extensions.Logging;

namespace RamOptimizer.HardwareControl;

/// <summary>
/// Manages hardware configuration backups and provides rollback capabilities
/// Snapshots are saved to disk and can be restored after system instability
/// </summary>
public class SnapshotManager
{
    private readonly string _backupPath;
    private readonly ILogger? _logger;

    public SnapshotManager(string? backupPath = null, ILogger? logger = null)
    {
        _backupPath = backupPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "RamOptimizer", "Backups");
        _logger = logger;

        // Ensure backup directory exists
        Directory.CreateDirectory(_backupPath);
    }

    /// <summary>
    /// Capture and save current hardware configuration
    /// </summary>
    public bool CaptureAndSave(RamOptimizer.Core.Interfaces.IHardwareController controller, string name = "auto", string notes = "")
    {
        try
        {
            var snapshot = HardwareSnapshot.Capture(controller, name, notes);
            return SaveSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to capture and save snapshot: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save a snapshot to disk
    /// </summary>
    public bool SaveSnapshot(HardwareSnapshot snapshot)
    {
        try
        {
            var filename = GenerateFilename(snapshot.SnapshotName);
            var filepath = Path.Combine(_backupPath, filename);

            File.WriteAllText(filepath, snapshot.ToJson());
            _logger?.LogInformation($"Saved snapshot to {filepath}");

            // Also save as "latest" for quick access
            if (snapshot.SnapshotName != "factory" && snapshot.SnapshotName != "emergency")
            {
                var latestPath = Path.Combine(_backupPath, "snapshot_latest.json");
                File.WriteAllText(latestPath, snapshot.ToJson());
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to save snapshot: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load the most recent snapshot
    /// </summary>
    public HardwareSnapshot? LoadLatestSnapshot()
    {
        try
        {
            var latestPath = Path.Combine(_backupPath, "snapshot_latest.json");
            if (File.Exists(latestPath))
            {
                var json = File.ReadAllText(latestPath);
                return HardwareSnapshot.FromJson(json);
            }

            // Fallback: find most recent file
            var files = Directory.GetFiles(_backupPath, "snapshot_*.json")
                .Where(f => !f.EndsWith("latest.json"))
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            if (files.Any())
            {
                var json = File.ReadAllText(files.First());
                return HardwareSnapshot.FromJson(json);
            }

            _logger?.LogWarning("No snapshots found");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to load latest snapshot: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load a specific snapshot by name
    /// </summary>
    public HardwareSnapshot? LoadSnapshot(string name)
    {
        try
        {
            // Try exact name first
            var directPath = Path.Combine(_backupPath, $"snapshot_{name}.json");
            if (File.Exists(directPath))
            {
                var json = File.ReadAllText(directPath);
                return HardwareSnapshot.FromJson(json);
            }

            // Search by pattern
            var files = Directory.GetFiles(_backupPath, $"snapshot_{name}_*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            if (files.Any())
            {
                var json = File.ReadAllText(files.First());
                return HardwareSnapshot.FromJson(json);
            }

            _logger?.LogWarning($"Snapshot '{name}' not found");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to load snapshot '{name}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// List all available snapshots
    /// </summary>
    public List<HardwareSnapshot> ListSnapshots()
    {
        var snapshots = new List<HardwareSnapshot>();

        try
        {
            var files = Directory.GetFiles(_backupPath, "snapshot_*.json")
                .Where(f => !f.EndsWith("latest.json"))
                .OrderByDescending(f => File.GetLastWriteTime(f));

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var snapshot = HardwareSnapshot.FromJson(json);
                    if (snapshot != null)
                        snapshots.Add(snapshot);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Failed to load snapshot from {file}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to list snapshots: {ex.Message}");
        }

        return snapshots;
    }

    /// <summary>
    /// Delete old snapshots, keeping only the most recent N snapshots
    /// </summary>
    public int CleanupOldSnapshots(int keepCount = 10)
    {
        try
        {
            var files = Directory.GetFiles(_backupPath, "snapshot_*.json")
                .Where(f => !f.EndsWith("latest.json") &&
                           !f.Contains("factory") &&
                           !f.Contains("emergency"))
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Skip(keepCount)
                .ToList();

            int deletedCount = 0;
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Failed to delete {file}: {ex.Message}");
                }
            }

            if (deletedCount > 0)
                _logger?.LogInformation($"Cleaned up {deletedCount} old snapshots");

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to cleanup old snapshots: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Restore a snapshot to hardware
    /// </summary>
    public bool RestoreSnapshot(RamOptimizer.Core.Interfaces.IHardwareController controller, string name)
    {
        var snapshot = LoadSnapshot(name);
        if (snapshot == null)
            return false;

        return snapshot.ApplyTo(controller, _logger);
    }

    /// <summary>
    /// Restore the latest snapshot
    /// </summary>
    public bool RestoreLatest(RamOptimizer.Core.Interfaces.IHardwareController controller)
    {
        var snapshot = LoadLatestSnapshot();
        if (snapshot == null)
            return false;

        return snapshot.ApplyTo(controller, _logger);
    }

    /// <summary>
    /// Generate filename for snapshot
    /// </summary>
    private string GenerateFilename(string name)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"snapshot_{name}_{timestamp}.json";
    }

    /// <summary>
    /// Check if any snapshots exist
    /// </summary>
    public bool HasSnapshots()
    {
        return Directory.GetFiles(_backupPath, "snapshot_*.json").Any();
    }

    /// <summary>
    /// Get total number of snapshots
    /// </summary>
    public int GetSnapshotCount()
    {
        return Directory.GetFiles(_backupPath, "snapshot_*.json")
            .Where(f => !f.EndsWith("latest.json"))
            .Count();
    }
}
