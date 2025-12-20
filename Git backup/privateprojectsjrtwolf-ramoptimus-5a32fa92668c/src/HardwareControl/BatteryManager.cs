namespace RamOptimizer.HardwareControl;

/// <summary>
/// Manages battery charge limit to preserve battery health
/// </summary>
public class BatteryManager
{
    private readonly AsusAcpiInterface _acpi;

    public BatteryManager(AsusAcpiInterface acpi)
    {
        _acpi = acpi ?? throw new ArgumentNullException(nameof(acpi));
    }

    /// <summary>
    /// Set battery charge limit (60-100%)
    /// Recommended: 80% for daily use to preserve battery health
    /// Note: ASUS expects the value with +36 offset
    /// </summary>
    public void SetChargeLimit(int limit)
    {
        if (limit < 60 || limit > 100)
            throw new ArgumentException("Charge limit must be between 60 and 100");

        // ASUS expects value + 36 offset
        _acpi.DeviceSet(AsusAcpiInterface.BatteryLimit, limit + 36);
    }

    /// <summary>
    /// Get current charge limit
    /// Note: ASUS returns the value in byte 2 (bits 16-23) with an offset of +36
    /// Example: 100% is stored as 0x88 (136), 80% as 0x74 (116)
    /// </summary>
    public int GetChargeLimit()
    {
        var rawValue = _acpi.DeviceGet(AsusAcpiInterface.BatteryLimit);
        // Extract byte 2 (bits 16-23) and subtract offset
        var encodedValue = (rawValue >> 16) & 0xFF;
        return encodedValue - 36;
    }

    /// <summary>
    /// Get recommended charge limit based on usage pattern
    /// </summary>
    public static int GetRecommendedLimit(UsagePattern pattern)
    {
        return pattern switch
        {
            UsagePattern.AlwaysPlugged => 60,  // Minimize battery wear
            UsagePattern.DailyUse => 80,       // Balance health and capacity
            UsagePattern.MaximumCapacity => 100, // Full capacity when needed
            _ => 80
        };
    }

    /// <summary>
    /// Get charge limit description
    /// </summary>
    public static string GetLimitDescription(int limit)
    {
        return limit switch
        {
            60 => "Best for always-plugged laptops - Maximizes battery lifespan",
            80 => "Recommended for daily use - Good balance",
            100 => "Full capacity - Use when you need maximum battery life",
            _ => $"{limit}% charge limit"
        };
    }
}

public enum UsagePattern
{
    AlwaysPlugged,
    DailyUse,
    MaximumCapacity
}
