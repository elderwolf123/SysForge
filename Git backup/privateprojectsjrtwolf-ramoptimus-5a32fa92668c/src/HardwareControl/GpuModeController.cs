namespace RamOptimizer.HardwareControl;

/// <summary>
/// Manages GPU modes (Eco/Standard/Ultimate)
/// </summary>
public class GpuModeController
{
    private readonly AsusAcpiInterface _acpi;

    public GpuModeController(AsusAcpiInterface acpi)
    {
        _acpi = acpi ?? throw new ArgumentNullException(nameof(acpi));
    }

    /// <summary>
    /// Set GPU mode
    /// Note: Requires system restart to take effect
    /// </summary>
    public void SetMode(GpuMode mode)
    {
        _acpi.DeviceSet(AsusAcpiInterface.GPUMuxROG, (int)mode);
    }

    /// <summary>
    /// Get current GPU mode
    /// </summary>
    public GpuMode GetCurrentMode()
    {
        var mode = _acpi.DeviceGet(AsusAcpiInterface.GPUMuxROG);
        return (GpuMode)mode;
    }

    /// <summary>
    /// Get GPU mode name
    /// </summary>
    public static string GetModeName(GpuMode mode)
    {
        return mode switch
        {
            GpuMode.Eco => "Eco",
            GpuMode.Standard => "Standard",
            GpuMode.Ultimate => "Ultimate",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get GPU mode description
    /// </summary>
    public static string GetModeDescription(GpuMode mode)
    {
        return mode switch
        {
            GpuMode.Eco => "iGPU only - Best battery life, dGPU disabled",
            GpuMode.Standard => "Hybrid - iGPU drives display, dGPU available for apps",
            GpuMode.Ultimate => "dGPU drives display - Best performance, higher power",
            _ => ""
        };
    }

    /// <summary>
    /// Check if mode change requires restart
    /// </summary>
    public static bool RequiresRestart(GpuMode mode)
    {
        // GPU mode changes always require restart
        return true;
    }
}

public enum GpuMode
{
    Eco = 0,        // iGPU only
    Standard = 1,   // MS Hybrid (iGPU drives display)
    Ultimate = 2    // dGPU drives display
}
