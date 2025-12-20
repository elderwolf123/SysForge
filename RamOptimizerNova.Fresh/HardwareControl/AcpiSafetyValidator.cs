using Microsoft.Extensions.Logging;

namespace RamOptimizer.HardwareControl;

/// <summary>
/// Validates ACPI operations before they are executed to prevent dangerous configurations
/// </summary>
public static class AcpiSafetyValidator
{
    // Minimum safe values to prevent boot failures
    public const int MIN_SAFE_P_CORES = 2;  // Always keep at least 2 P-cores
    public const int MIN_SAFE_TOTAL_CORES = 4;  // Minimum total cores for Windows

    // Known dangerous configurations that can prevent boot
    private static readonly int[] FORBIDDEN_CORE_CONFIGS = new[]
    {
        0x0000,  // No cores enabled - CRITICAL
        0x0001,  // Only 1 core total
        0x0100,  // Only 1 P-core
    };

    /// <summary>
    /// Validate core configuration before applying
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateCoreConfig(
        int pCores, int eCores, int maxP, int maxE, ILogger? logger = null)
    {
        // Check P-cores range
        if (pCores < 1 || pCores > maxP)
        {
            var msg = $"P-cores must be between 1 and {maxP}. Requested: {pCores}";
            logger?.LogError(msg);
            return (false, msg);
        }

        // Check E-cores range
        if (eCores < 0 || eCores > maxE)
        {
            var msg = $"E-cores must be between 0 and {maxE}. Requested: {eCores}";
            logger?.LogError(msg);
            return (false, msg);
        }

        // Ensure minimum P-cores for system stability
        if (pCores < MIN_SAFE_P_CORES)
        {
            var msg = $"Must have at least {MIN_SAFE_P_CORES} P-cores for system stability. Requested: {pCores}";
            logger?.LogError(msg);
            return (false, msg);
        }

        // Ensure minimum total cores
        int totalCores = pCores + eCores;
        if (totalCores < MIN_SAFE_TOTAL_CORES)
        {
            var msg = $"Must have at least {MIN_SAFE_TOTAL_CORES} total cores. Requested: {totalCores}";
            logger?.LogError(msg);
            return (false, msg);
        }

        // Total cores sanity check
        if (totalCores > 32)
        {
            var msg = $"Total cores exceeds maximum (32). Requested: {totalCores}";
            logger?.LogError(msg);
            return (false, msg);
        }

        // Check forbidden configurations
        int coreConfig = (pCores << 8) | eCores;
        if (IsConfigurationForbidden(coreConfig))
        {
            var msg = $"Configuration 0x{coreConfig:X4} is known to be dangerous and is forbidden";
            logger?.LogError(msg);
            return (false, msg);
        }

        logger?.LogInformation($"Core configuration validated: P={pCores}, E={eCores} (0x{coreConfig:X4})");
        return (true, string.Empty);
    }

    /// <summary>
    /// Validate battery limit before applying
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateBatteryLimit(int limit, ILogger? logger = null)
    {
        // ASUS supports 60-100% range
        if (limit < 60 || limit > 100)
        {
            var msg = $"Battery limit must be between 60 and 100. Requested: {limit}";
            logger?.LogError(msg);
            return (false, msg);
        }

        logger?.LogInformation($"Battery limit validated: {limit}%");
        return (true, string.Empty);
    }

    /// <summary>
    /// Check if a core configuration is in the forbidden list
    /// </summary>
    public static bool IsConfigurationForbidden(int coreConfig)
    {
        return FORBIDDEN_CORE_CONFIGS.Contains(coreConfig);
    }

    /// <summary>
    /// Validate performance mode
    /// Values from G-Helper: 0 = Silent, 1 = Performance, 2 = Turbo
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidatePerformanceMode(int mode, ILogger? logger = null)
    {
        if (mode < 0 || mode > 2)
        {
            var msg = $"Performance mode must be between 0 and 2. Requested: {mode}";
            logger?.LogError(msg);
            return (false, msg);
        }

        logger?.LogInformation($"Performance mode validated: {mode}");
        return (true, string.Empty);
    }

    /// <summary>
    /// Validate GPU mode
    /// Values: 0 = iGPU only, 1 = dGPU (MUX)
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateGpuMode(int mode, ILogger? logger = null)
    {
        if (mode < 0 || mode > 1)
        {
            var msg = $"GPU mode must be 0 (iGPU) or 1 (dGPU). Requested: {mode}";
            logger?.LogError(msg);
            return (false, msg);
        }

        logger?.LogInformation($"GPU mode validated: {mode}");
        return (true, string.Empty);
    }
}
