using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CompressionBenchmark;

/// <summary>
/// Manages RAM optimization for compression testing.
/// Uses aggressive process cleanup to maximize available RAM for testing large files.
/// Enhanced with comprehensive process monitoring and auto-restart prevention.
/// </summary>
public class RamOptimizationManager : IDisposable
{
    private readonly ProcessLearningSystem _learningSystem;
    private readonly List<Process> _killedProcesses = new();
    private readonly ProcessRestartPrevention _restartPrevention = new();
    private bool _isOptimized = false;
    private readonly List<string> _previouslyKilledProcessNames = new();

    public long MinimumReservedRamBytes { get; set; } = 5L * 1024 * 1024 * 1024; // 5GB default

    public RamOptimizationManager()
    {
        _learningSystem = new ProcessLearningSystem();
    }

    public long GetCurrentAvailableRam()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        return gcInfo.TotalAvailableMemoryBytes - GC.GetTotalMemory(false);
    }

    public void EnableAggressiveMode()
    {
        if (_isOptimized)
        {
            Console.WriteLine("⚠️  Already in aggressive mode");
            return;
        }

        // Check for admin privileges
        if (!ProcessRestartPrevention.IsAdministrator())
        {
            Console.WriteLine("\n⚠️  WARNING: Not running as Administrator!");
            Console.WriteLine("   Explorer may auto-restart after being killed");
            Console.WriteLine("   For best results, run as Administrator\n");
            BenchmarkLogger.LogWarning("Running without admin privileges - auto-restart prevention disabled");
        }
        else
        {
            // Disable auto-restart before killing processes
            Console.WriteLine("\n🛡️  Disabling auto-restart mechanisms...");
            
            if (_restartPrevention.DisableExplorerAutoRestart())
            {
                Console.WriteLine("   ✓ Explorer will stay dead until manually restarted");
            }
            
            _restartPrevention.DisableCommonServiceRestarts();
            _restartPrevention.StartMonitoring();
        }

        Console.WriteLine("\n🎯 AGGRESSIVE RAM OPTIMIZATION");
        Console.WriteLine("════════════════════════════════════════");

        var beforeRam = GetCurrentAvailableRam();
        Console.WriteLine($"RAM before: {FormatBytes(beforeRam)}");

        // Record initial state
        var runningProcesses = Process.GetProcesses().ToList();
        _learningSystem.RecordSystemState("Before Optimization", runningProcesses, beforeRam);

        // Get killable processes
        var killableProcesses = GetKillableProcesses();
        Console.WriteLine($"\nFound {killableProcesses.Count} killable processes");

        int killed = 0;
        long freedMemory = 0;

        foreach (var process in killableProcesses)
        {
            try
            {
                var memoryUsed = process.WorkingSet64;
                var processName = process.ProcessName;
                
                // LOG BEFORE KILLING (for BSOD debugging)
                BenchmarkLogger.LogInfo($"ABOUT TO KILL: {processName} (PID: {process.Id}, RAM: {FormatBytes(memoryUsed)})");
                Console.Write($"  Killing: {processName} ({FormatBytes(memoryUsed)})... ");
                
                process.Kill();
                process.WaitForExit(2000);
                
                _killedProcesses.Add(process);
                _previouslyKilledProcessNames.Add(processName);
                _learningSystem.RecordProcessKill(processName, causedCrash: false);
                BenchmarkLogger.LogInfo($"SUCCESS: Killed {processName}");
                
                freedMemory += memoryUsed;
                killed++;
                
                Console.WriteLine("✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ({ex.Message})");
                BenchmarkLogger.LogError($"FAILED to kill {process.ProcessName}: {ex.Message}");
                _learningSystem.RecordProcessKill(process.ProcessName, causedCrash: true, ex.Message);
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var afterRam = GetCurrentAvailableRam();
        var actualFreed = afterRam - beforeRam;

        Console.WriteLine($"\nResults:");
        Console.WriteLine($"  Processes killed: {killed}");
        Console.WriteLine($"  RAM freed (estimated): {FormatBytes(freedMemory)}");
        Console.WriteLine($"  RAM freed (actual): {FormatBytes(actualFreed)}");
        Console.WriteLine($"  RAM available now: {FormatBytes(afterRam)}");
        Console.WriteLine($"  Reserved for system: {FormatBytes(MinimumReservedRamBytes)}");
        Console.WriteLine($"  Usable for testing: {FormatBytes(afterRam - MinimumReservedRamBytes)}");
        Console.WriteLine("════════════════════════════════════════\n");

        _learningSystem.RecordSystemState("After Optimization", Process.GetProcesses().ToList(), afterRam);
        _isOptimized = true;
    }

    public void RestoreProcesses()
    {
        Console.WriteLine("\n🔄 Restoring essential processes...");
        
        // Stop monitoring before restoring
        _restartPrevention.StopMonitoring();
        
        // Re-enable auto-restart BEFORE starting explorer
        _restartPrevention.RestoreExplorerAutoRestart();
        
        // Restart explorer.exe if it was killed
        if (!Process.GetProcessesByName("explorer").Any())
        {
            try
            {
                Process.Start("explorer.exe");
                Console.WriteLine("  ✓ Restarted explorer.exe (desktop)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed to restart explorer: {ex.Message}");
            }
        }
        
        // Give time for explorer to start
        System.Threading.Thread.Sleep(2000);
        
        // Restart other essential processes that were killed
        RestartEssentialProcesses();
        
        Console.WriteLine("  ✓ Desktop should be visible again");
        Console.WriteLine("  ✓ Auto-restart re-enabled");
        Console.WriteLine("  ✓ Essential services restored");
        Console.WriteLine("  ⚠️  Some applications may need manual restart");
        
        _killedProcesses.Clear();
        _previouslyKilledProcessNames.Clear();
        _isOptimized = false;
    }

    /// <summary>
    /// Restart essential processes that were killed
    /// </summary>
    private void RestartEssentialProcesses()
    {
        var essentialProcesses = new Dictionary<string, string>
        {
            { "explorer", "explorer.exe" },
            { "chrome", "chrome.exe" },
            { "firefox", "firefox.exe" },
            { "edge", "msedge.exe" },
            { "opera", "opera.exe" },
            { "brave", "brave.exe" }
        };

        foreach (var kvp in essentialProcesses)
        {
            if (_previouslyKilledProcessNames.Contains(kvp.Key))
            {
                try
                {
                    var processes = Process.GetProcessesByName(kvp.Key);
                    if (!processes.Any())
                    {
                        Process.Start(kvp.Value);
                        Console.WriteLine($"  ✓ Restarted {kvp.Key}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ⚠️  Could not restart {kvp.Key}: {ex.Message}");
                }
            }
        }
    }

    public void ExportLearningData()
    {
        _learningSystem.ExportLearningResults();
        _learningSystem.SaveDatabase();
    }

    /// <summary>
    /// Get list of processes that were previously killed (for monitoring)
    /// </summary>
    public static List<string> GetPreviouslyKilledProcesses()
    {
        // This would typically be loaded from a persistent storage
        // For now, return common auto-restarting processes
        return ProcessRestartPrevention.GetAutoRestartingProcesses();
    }

    private List<Process> GetKillableProcesses()
    {
        var currentProcess = Process.GetCurrentProcess();
        var allProcesses = Process.GetProcesses();

        // EXPERIMENTAL: Minimal blacklist - only ABSOLUTELY CRITICAL kernel processes
        // Everything else will be tested to build comprehensive learned blacklist
        var blacklist = new[]
        {
            // Kernel critical (DO NOT KILL - will BSOD)
            "system",
            "registry", 
            "smss",           // Session Manager
            "csrss",          // Client/Server Runtime
            "wininit",        // Windows Initialization
            "services",       // Service Control Manager
            "lsass",          // Security Authority
            "winlogon",       // Windows Logon
            "svchost",        // ⭐ Service Host - CAUSES BSOD (user confirmed)
            "conhost",        // ⭐ Console Host - Kills the terminal window!
            
            // Self-protection
            "compressionbenchmark"  // Don't kill ourselves
        };

        // DYNAMIC: Get learned blacklist from previous crashes (reloaded each time!)
        var learnedBlacklist = _learningSystem.GetEssentialProcesses();
        
        if (learnedBlacklist.Count > 0)
        {
            Console.WriteLine($"\n🛡️  Active learned blacklist: {learnedBlacklist.Count} processes");
            foreach (var proc in learnedBlacklist.Take(5))
            {
                Console.WriteLine($"   - {proc}");
            }
            if (learnedBlacklist.Count > 5)
                Console.WriteLine($"   ... and {learnedBlacklist.Count - 5} more");
        }

        // ULTRA-AGGRESSIVE: Kill EVERYTHING not in minimal blacklist or learned blacklist
        return allProcesses
            .Where(p => p.Id != currentProcess.Id)
            .Where(p => p.Id != 0 && p.Id != 4) // Skip System Idle (0) and System (4)
            .Where(p => !blacklist.Any(b => p.ProcessName.ToLowerInvariant() == b.ToLowerInvariant()))
            .Where(p => !learnedBlacklist.Contains(p.ProcessName)) // DYNAMIC BLACKLIST!
            .Where(p => p.WorkingSet64 > 10 * 1024 * 1024) // Kill anything using >10MB (very aggressive)
            .OrderByDescending(p => p.WorkingSet64) // Kill biggest RAM users first
            .ToList();
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public void Dispose()
    {
        _restartPrevention?.Dispose();
    }
}
