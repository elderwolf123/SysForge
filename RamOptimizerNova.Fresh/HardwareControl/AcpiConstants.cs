namespace RamOptimizer.HardwareControl;

/// <summary>
/// ACPI timing and safety constants
/// </summary>
public static class AcpiConstants
{
    /// <summary>
    /// Delay after ACPI write before reading back for verification (milliseconds)
    /// </summary>
    public const int ACPI_WRITE_DELAY_MS = 100;

    /// <summary>
    /// Delay for ACPI verification operations (milliseconds)
    /// </summary>
    public const int ACPI_VERIFY_DELAY_MS = 200;

    /// <summary>
    /// Delay before auto-clearing battery limit rollback flag (milliseconds)
    /// Battery changes take effect immediately, so we can clear flag after short delay
    /// </summary>
    public const int BATTERY_CONFIRM_DELAY_MS = 5000;

    /// <summary>
    /// Required number of successful boots before clearing rollback flag automatically
    /// </summary>
    public const int REQUIRED_SUCCESSFUL_BOOTS = 2;
}
