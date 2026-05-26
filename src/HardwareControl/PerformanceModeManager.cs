namespace RamOptimizer.HardwareControl;

/// <summary>
/// Manages ASUS performance modes and power profiles
/// </summary>
public class PerformanceModeManager
{
    private readonly AsusAcpiInterface _acpi;

    public PerformanceModeManager(AsusAcpiInterface acpi)
    {
        _acpi = acpi ?? throw new ArgumentNullException(nameof(acpi));
    }

    /// <summary>
    /// Set performance mode and corresponding Windows power plan
    /// </summary>
    public void SetMode(PerformanceMode mode)
    {
        // Set ASUS performance mode via ACPI
        _acpi.DeviceSet(AsusAcpiInterface.PerformanceMode, (int)mode);

        // Set corresponding Windows power plan
        SetWindowsPowerPlan(mode);
    }

    /// <summary>
    /// Get current performance mode
    /// </summary>
    public PerformanceMode GetCurrentMode()
    {
        var mode = _acpi.DeviceGet(AsusAcpiInterface.PerformanceMode);
        return (PerformanceMode)mode;
    }

    /// <summary>
    /// Set Windows power plan to match performance mode
    /// </summary>
    private void SetWindowsPowerPlan(PerformanceMode mode)
    {
        try
        {
            var planGuid = mode switch
            {
                PerformanceMode.Silent => "a1841308-3541-4fab-bc81-f71556f20b4a", // Power Saver
                PerformanceMode.Balanced => "381b4222-f694-41f0-9685-ff5bb260df2e", // Balanced
                PerformanceMode.Turbo => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", // High Performance
                _ => "381b4222-f694-41f0-9685-ff5bb260df2e" // Default to Balanced
            };

            // Use absolute path for powercfg.exe to prevent Path Hijacking / LPE
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "powercfg.exe"),
                    Arguments = $"/setactive {planGuid}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
        }
        catch
        {
            // Ignore errors - Windows power plan is optional
        }
    }

    /// <summary>
    /// Get performance mode name
    /// </summary>
    public static string GetModeName(PerformanceMode mode)
    {
        return mode switch
        {
            PerformanceMode.Silent => "Silent",
            PerformanceMode.Balanced => "Balanced",
            PerformanceMode.Turbo => "Turbo",
            PerformanceMode.Manual => "Manual",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get performance mode description
    /// </summary>
    public static string GetModeDescription(PerformanceMode mode)
    {
        return mode switch
        {
            PerformanceMode.Silent => "Quiet operation, lower power consumption, reduced fan noise",
            PerformanceMode.Balanced => "Balanced performance and efficiency, moderate fan speeds",
            PerformanceMode.Turbo => "Maximum performance, higher power consumption, louder fans",
            PerformanceMode.Manual => "Custom performance profile with manual settings",
            _ => ""
        };
    }
}

public enum PerformanceMode
{
    Balanced = 0,
    Silent = 1,
    Turbo = 3,
    Manual = 4  // Custom performance profile
}
