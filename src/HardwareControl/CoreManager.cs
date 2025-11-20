namespace RamOptimizer.HardwareControl;

/// <summary>
/// Manages Intel P-core and E-core configuration
/// Requires 13th Gen+ Intel processors (Alder Lake or newer)
/// Changes require system restart to take effect
/// </summary>
public class CoreManager
{
    private readonly AsusAcpiInterface _acpiInterface;

    public CoreManager(AsusAcpiInterface acpiInterface)
    {
        _acpiInterface = acpiInterface;
    }

    /// <summary>
    /// Get maximum available P-cores and E-cores
    /// </summary>
    public (int PCores, int ECores) GetMaxCores()
    {
        try
        {
            int maxCores = _acpiInterface.DeviceGet(AsusAcpiInterface.CORES_MAX);
            
            // Format: 0x0[E-cores]0[P-cores]
            int pCores = maxCores & 0xFFFF;        // Lower 16 bits
            int eCores = (maxCores >> 16) & 0xFFFF; // Upper 16 bits
            
            return (pCores, eCores);
        }
        catch
        {
            return (0, 0);
        }
    }

    /// <summary>
    /// Get current enabled P-cores and E-cores
    /// </summary>
    public (int PCores, int ECores) GetCurrentCores()
    {
        try
        {
            int currentCores = _acpiInterface.DeviceGet(AsusAcpiInterface.CORES_CPU);
            
            // Format: 0x0[E-cores]0[P-cores]
            int pCores = currentCores & 0xFFFF;        // Lower 16 bits
            int eCores = (currentCores >> 16) & 0xFFFF; // Upper 16 bits
            
            return (pCores, eCores);
        }
        catch
        {
            return (0, 0);
        }
    }

    /// <summary>
    /// Set enabled P-cores and E-cores
    /// Requires system restart to take effect
    /// </summary>
    public bool SetCores(int pCores, int eCores)
    {
        try
        {
            var (maxP, maxE) = GetMaxCores();
            
            // Validate inputs
            if (pCores < 1 || pCores > maxP)
                throw new ArgumentException($"P-cores must be between 1 and {maxP}");
            
            if (eCores < 0 || eCores > maxE)
                throw new ArgumentException($"E-cores must be between 0 and {maxE}");
            
            // Format: 0x0[E-cores]0[P-cores]
            int coreConfig = (eCores << 16) | pCores;
            
            int result = _acpiInterface.DeviceSet(AsusAcpiInterface.CORES_CPU, coreConfig);
            return result == 1; // 1 = success
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if P/E core control is supported on this system
    /// </summary>
    public bool IsSupported()
    {
        var (maxP, maxE) = GetMaxCores();
        // If we have both P and E cores, it's supported
        return maxP > 0 && maxE > 0;
    }

    /// <summary>
    /// Get CPU information to determine if it's 13th Gen+ Intel
    /// </summary>
    public static string GetCpuInfo()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                return obj["Name"]?.ToString() ?? "Unknown CPU";
            }
        }
        catch
        {
            return "Unknown CPU";
        }
        
        return "Unknown CPU";
    }

    /// <summary>
    /// Detect if CPU is 13th Gen+ Intel (supports P/E cores)
    /// </summary>
    public static bool IsCpuSupported()
    {
        string cpuName = GetCpuInfo().ToLower();
        
        // Check for Intel 13th Gen+ (Raptor Lake, Meteor Lake, etc.)
        // These have model numbers like i7-13700H, i9-14900K, etc.
        if (cpuName.Contains("intel"))
        {
            // Extract generation number (e.g., "13" from "i7-13700H")
            var match = System.Text.RegularExpressions.Regex.Match(cpuName, @"i[3579]-(\d{2})\d{3}");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int gen))
            {
                return gen >= 13; // 13th Gen or newer
            }
        }
        
        return false;
    }
}
