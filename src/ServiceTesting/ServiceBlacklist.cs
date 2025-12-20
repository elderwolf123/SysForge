namespace RamOptimizer.ServiceTesting;

/// <summary>
/// Reasons why a service was blacklisted
/// </summary>
public enum BlacklistReason
{
    None,
    HighRAMUsage,       // Uses excessive RAM when running
    HighCPUUsage,       // Uses excessive CPU
    HighIOUsage,        // Causes high disk I/O
    HighPowerUsage,     // Drains battery significantly
    CausesInstability,  // System crashes/freezes when stopped
    CausesBootFailure,  // System fails to boot when disabled
    Mixed               // Multiple issues
}

/// <summary>
/// Performance impact metrics for a service
/// </summary>
public class PerformanceImpact
{
    public long RAMDeltaMB { get; set; }        // RAM difference in MB
    public double CPUDeltaPercent { get; set; }  // CPU usage difference in %
    public double IODeltaMBps { get; set; }      // Disk I/O difference in MB/s
    public double PowerDeltaWatts { get; set; }  // Power consumption difference in Watts
    
    public bool IsSignificant()
    {
        return RAMDeltaMB > 100 || 
               CPUDeltaPercent > 5.0 || 
               IODeltaMBps > 10.0 || 
               PowerDeltaWatts > 2.0;
    }
}

/// <summary>
/// Entry in the service blacklist database
/// </summary>
public class ServiceBlacklistEntry
{
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BlacklistReason Reason { get; set; }
    public DateTime TestedOn { get; set; }
    public PerformanceImpact Impact { get; set; } = new();
    public List<string> EventLogErrors { get; set; } = new();
    public int DependentServicesCount { get; set; }
    public bool IsEssential { get; set; }
}

/// <summary>
/// Manages the service blacklist database
/// </summary>
public class ServiceBlacklist
{
    private const string BlacklistPath = @"C:\ProgramData\RamOptimizer\ServiceTest\blacklist.json";
    private Dictionary<string, ServiceBlacklistEntry> _entries = new();
    private readonly object _lock = new();

    public ServiceBlacklist()
    {
        EnsureDirectoryExists();
        Load();
    }

    private void EnsureDirectoryExists()
    {
        var dir = Path.GetDirectoryName(BlacklistPath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(BlacklistPath))
                {
                    var json = File.ReadAllText(BlacklistPath);
                    var entries = System.Text.Json.JsonSerializer.Deserialize<List<ServiceBlacklistEntry>>(json, new System.Text.Json.JsonSerializerOptions
                    {
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                    
                    _entries = entries?.ToDictionary(e => e.ServiceName, e => e) 
                        ?? new Dictionary<string, ServiceBlacklistEntry>();
                }
            }
            catch
            {
                _entries = new Dictionary<string, ServiceBlacklistEntry>();
            }
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(
                    _entries.Values.ToList(),
                    new System.Text.Json.JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }
                );
                
                File.WriteAllText(BlacklistPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save blacklist: {ex.Message}");
            }
        }
    }

    public void Add(ServiceBlacklistEntry entry)
    {
        lock (_lock)
        {
            _entries[entry.ServiceName] = entry;
            Save();
        }
    }

    public bool IsBlacklisted(string serviceName)
    {
        lock (_lock)
        {
            return _entries.ContainsKey(serviceName);
        }
    }

    public ServiceBlacklistEntry? Get(string serviceName)
    {
        lock (_lock)
        {
            return _entries.TryGetValue(serviceName, out var entry) ? entry : null;
        }
    }

    public List<ServiceBlacklistEntry> GetAll()
    {
        lock (_lock)
        {
            return _entries.Values.ToList();
        }
    }

    public void Remove(string serviceName)
    {
        lock (_lock)
        {
            _entries.Remove(serviceName);
            Save();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            Save();
        }
    }

    public int Count => _entries.Count;
}
