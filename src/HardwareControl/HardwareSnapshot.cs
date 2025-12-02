using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.HardwareControl;

/// <summary>
/// Represents a snapshot of hardware configuration at a point in time
/// Used for backup and rollback purposes
/// </summary>
public class HardwareSnapshot
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("p_cores")]
    public int PCores { get; set; }

    [JsonPropertyName("e_cores")]
    public int ECores { get; set; }

    [JsonPropertyName("battery_limit")]
    public int BatteryLimit { get; set; }

    [JsonPropertyName("performance_mode")]
    public int PerformanceMode { get; set; }

    [JsonPropertyName("gpu_mode")]
    public int GpuMode { get; set; }

    [JsonPropertyName("cpu_name")]
    public string CpuName { get; set; } = string.Empty;

    [JsonPropertyName("snapshot_name")]
    public string SnapshotName { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Capture current hardware configuration
    /// </summary>
    public static HardwareSnapshot Capture(RamOptimizer.Core.Interfaces.IHardwareController controller, string name = "auto", string notes = "")
    {
        try
        {
            var snapshot = new HardwareSnapshot
            {
                SnapshotName = name,
                Notes = notes,
                CpuName = CoreManager.GetCpuInfo() // This might need to be moved to controller too
            };

            // Capture core configuration
            if (controller is RamOptimizer.Core.Interfaces.ICoreController coreController && coreController.IsSupported)
            {
                snapshot.PCores = coreController.GetCurrentPCores();
                snapshot.ECores = coreController.GetCurrentECores();
            }

            // Capture battery limit
            if (controller is RamOptimizer.Core.Interfaces.IBatteryController batteryController && batteryController.IsSupported)
            {
                snapshot.BatteryLimit = batteryController.GetChargeLimit();
            }

            // Capture performance mode
            if (controller is RamOptimizer.Core.Interfaces.IPerformanceController perfController && perfController.IsSupported)
            {
                snapshot.PerformanceMode = (int)perfController.GetCurrentMode();
            }

            // Capture GPU mode
            // Note: IGpuController is not yet defined in IHardwareController.cs, assuming it might be added or we skip it for now
            // For now, setting to -1 as placeholder or if we add IGpuController later
            snapshot.GpuMode = -1; 

            return snapshot;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to capture hardware snapshot: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Apply this snapshot to hardware
    /// </summary>
    public bool ApplyTo(RamOptimizer.Core.Interfaces.IHardwareController controller, Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation($"Restoring snapshot '{SnapshotName}' from {Timestamp}");

            bool success = true;

            // Restore core configuration
            if (controller is RamOptimizer.Core.Interfaces.ICoreController coreController && coreController.IsSupported)
            {
                bool result = coreController.SetCores(PCores, ECores);
                logger?.LogInformation($"Restored cores: P={PCores}, E={ECores}, Result={result}");
                if (!result) success = false;
            }

            // Restore battery limit
            if (controller is RamOptimizer.Core.Interfaces.IBatteryController batteryController && batteryController.IsSupported)
            {
                bool result = batteryController.SetChargeLimit(BatteryLimit);
                logger?.LogInformation($"Restored battery limit: {BatteryLimit}%, Result={result}");
            }

            // Restore performance mode
            if (controller is RamOptimizer.Core.Interfaces.IPerformanceController perfController && perfController.IsSupported)
            {
                bool result = perfController.SetMode((RamOptimizer.Core.Interfaces.PerformanceMode)PerformanceMode);
                logger?.LogInformation($"Restored performance mode: {PerformanceMode}, Result={result}");
            }

            // Restore GPU mode if supported
            // (Skipping GPU for now as interface is missing)

            return success;
        }
        catch (Exception ex)
        {
            logger?.LogError($"Failed to apply snapshot: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Serialize to JSON
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Deserialize from JSON
    /// </summary>
    public static HardwareSnapshot? FromJson(string json)
    {
        return JsonSerializer.Deserialize<HardwareSnapshot>(json);
    }

    public override string ToString()
    {
        return $"Snapshot '{SnapshotName}' from {Timestamp:yyyy-MM-dd HH:mm:ss}: " +
               $"P={PCores} E={ECores} Battery={BatteryLimit}% Perf={PerformanceMode}";
    }
}
