namespace RamOptimizer.ServiceTesting;

/// <summary>
/// Test checkpoint data for persistence across reboots
/// </summary>
public class TestCheckpoint
{
    public List<string> CompletedServices { get; set; } = new();
    public List<string> SkippedServices { get; set; } = new();
    public string? CurrentService { get; set; }
    public int TotalServices { get; set; }
    public DateTime LastUpdate { get; set; }
    public bool TestInProgress { get; set; }
    public int RebootCount { get; set; }
    public DateTime TestStarted { get; set; }
}

/// <summary>
/// Manages test progress checkpoints for recovery
/// </summary>
public class CheckpointManager
{
    private const string CheckpointPath = @"C:\ProgramData\RamOptimizer\ServiceTest\checkpoint.json";
    private const string RegistryPath = @"SOFTWARE\RamOptimizer\ServiceTest";
    private TestCheckpoint _checkpoint = new();
    private readonly object _lock = new();

    public CheckpointManager()
    {
        EnsureDirectoryExists();
        Load();
    }

    private void EnsureDirectoryExists()
    {
        var dir = Path.GetDirectoryName(CheckpointPath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public void StartTest(int totalServices)
    {
        lock (_lock)
        {
            _checkpoint = new TestCheckpoint
            {
                TotalServices = totalServices,
                TestInProgress = true,
                TestStarted = DateTime.Now,
                LastUpdate = DateTime.Now
            };
            
            Save();
            SaveToRegistry();
        }
    }

    public void UpdateProgress(string serviceName, bool completed)
    {
        lock (_lock)
        {
            if (completed)
            {
                if (!_checkpoint.CompletedServices.Contains(serviceName))
                {
                    _checkpoint.CompletedServices.Add(serviceName);
                }
            }
            else
            {
                if (!_checkpoint.SkippedServices.Contains(serviceName))
                {
                    _checkpoint.SkippedServices.Add(serviceName);
                }
            }
            
            _checkpoint.CurrentService = null;
            _checkpoint.LastUpdate = DateTime.Now;
            
            // Save every update for maximum safety
            Save();
        }
    }

    public void SetCurrentService(string serviceName)
    {
        lock (_lock)
        {
            _checkpoint.CurrentService = serviceName;
            _checkpoint.LastUpdate = DateTime.Now;
            Save();
        }
    }

    public void EndTest()
    {
        lock (_lock)
        {
            _checkpoint.TestInProgress = false;
            _checkpoint.CurrentService = null;
            _checkpoint.LastUpdate = DateTime.Now;
            
            Save();
            ClearRegistry();
        }
    }

    public void IncrementRebootCount()
    {
        lock (_lock)
        {
            _checkpoint.RebootCount++;
            _checkpoint.LastUpdate = DateTime.Now;
            Save();
            SaveToRegistry();
        }
    }

    public TestCheckpoint GetCheckpoint()
    {
        lock (_lock)
        {
            return _checkpoint;
        }
    }

    public bool HasPendingTest()
    {
        lock (_lock)
        {
            return _checkpoint.TestInProgress;
        }
    }

    public bool WasServiceTested(string serviceName)
    {
        lock (_lock)
        {
            return _checkpoint.CompletedServices.Contains(serviceName) ||
                   _checkpoint.SkippedServices.Contains(serviceName);
        }
    }

    private void Load()
    {
        lock (_lock)
        {
            try
            {
                // Try file first
                if (File.Exists(CheckpointPath))
                {
                    var json = File.ReadAllText(CheckpointPath);
                    var loaded = System.Text.Json.JsonSerializer.Deserialize<TestCheckpoint>(json, new System.Text.Json.JsonSerializerOptions
                    {
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                    if (loaded != null)
                    {
                        _checkpoint = loaded;
                        return;
                    }
                }
                
                // Fallback to registry
                LoadFromRegistry();
            }
            catch
            {
                _checkpoint = new TestCheckpoint();
            }
        }
    }

    private void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(
                    _checkpoint,
                    new System.Text.Json.JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }
                );
                
                File.WriteAllText(CheckpointPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save checkpoint: {ex.Message}");
            }
        }
    }

    private void SaveToRegistry()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(RegistryPath);
            if (key != null)
            {
                key.SetValue("TestInProgress", _checkpoint.TestInProgress ? 1 : 0);
                key.SetValue("CurrentService", _checkpoint.CurrentService ?? "");
                key.SetValue("CompletedCount", _checkpoint.CompletedServices.Count);
                key.SetValue("RebootCount", _checkpoint.RebootCount);
            }
        }
        catch
        {
            // Registry access may fail in non-admin mode
        }
    }

    private void LoadFromRegistry()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryPath);
            if (key != null)
            {
                var inProgress = (int?)key.GetValue("TestInProgress");
                if (inProgress == 1)
                {
                    _checkpoint.TestInProgress = true;
                    _checkpoint.CurrentService = key.GetValue("CurrentService") as string;
                    _checkpoint.RebootCount = (int?)key.GetValue("RebootCount") ?? 0;
                }
            }
        }
        catch
        {
            // Ignore registry errors
        }
    }

    private void ClearRegistry()
    {
        try
        {
            Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(RegistryPath, false);
        }
        catch
        {
            // Ignore errors
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _checkpoint = new TestCheckpoint();
            Save();
            ClearRegistry();
            
            // Also delete checkpoint file
            try
            {
                if (File.Exists(CheckpointPath))
                {
                    File.Delete(CheckpointPath);
                }
            }
            catch
            {
                // Ignore
            }
        }
    }
}
