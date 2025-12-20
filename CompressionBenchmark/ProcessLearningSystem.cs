using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CompressionBenchmark;

/// <summary>
/// Learns which processes are essential vs killable for system stability.
/// Exports results to help optimize the main RAM optimizer program.
/// </summary>
public class ProcessLearningSystem
{
    private const string DatabasePath = "process_blacklist_learning.json";
    private ProcessLearningDatabase _database;

    public ProcessLearningSystem()
    {
        _database = LoadDatabase();
    }

    public void RecordSystemState(string phase, List<Process> runningProcesses, long availableRam)
    {
        var snapshot = new ProcessSnapshot
        {
            Timestamp = DateTime.Now,
            Phase = phase,
            AvailableRamBytes = availableRam,
            RunningProcesses = runningProcesses.Select(p => new ProcessInfo
            {
                Name = p.ProcessName,
                Id = p.Id,
                MemoryMB = p.WorkingSet64 / 1024 / 1024,
                IsSystemProcess = IsSystemProcess(p)
            }).ToList()
        };

        _database.Snapshots.Add(snapshot);
    }

    public void RecordProcessKill(string processName, bool causedCrash, string? crashReason = null)
    {
        if (!_database.ProcessKillResults.ContainsKey(processName))
        {
            _database.ProcessKillResults[processName] = new ProcessKillResult
            {
                ProcessName = processName,
                TimesKilled = 0,
                TimesCausedCrash = 0,
                SafeToKill = true
            };
        }

        var result = _database.ProcessKillResults[processName];
        result.TimesKilled++;
        
        if (causedCrash)
        {
            result.TimesCausedCrash++;
            result.CrashReasons.Add(crashReason ?? "Unknown");
            
            // IMMEDIATE BLACKLIST: Mark as unsafe on FIRST crash
            result.SafeToKill = false;
            result.RecommendedAction = "BLACKLIST - Caused system crash";
            
            Console.WriteLine($"\n⚠️  Process '{processName}' caused crash - BLACKLISTED");
            Console.WriteLine($"   Reason: {crashReason ?? "Unknown"}");
            BenchmarkLogger.LogWarning($"Process {processName} blacklisted after crash: {crashReason}");
            
            // Auto-save learning database immediately
            SaveDatabase();
        }
        else
        {
            result.LastSuccessfulKill = DateTime.Now;
            result.RecommendedAction = "Safe to kill";
        }
    }

    public List<string> GetEssentialProcesses()
    {
        return _database.ProcessKillResults
            .Where(kvp => !kvp.Value.SafeToKill)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public List<string> GetSafeToKillProcesses()
    {
        return _database.ProcessKillResults
            .Where(kvp => kvp.Value.SafeToKill && kvp.Value.TimesKilled >= 3)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public void ExportLearningResults(string outputPath = "process_blacklist_recommendations.json")
    {
        var recommendations = new
        {
            GeneratedAt = DateTime.Now,
            SystemInfo = new
            {
                MachineName = Environment.MachineName,
                OS = Environment.OSVersion.ToString()
            },
            Statistics = new
            {
                TotalProcessesTested = _database.ProcessKillResults.Count,
                EssentialProcesses = _database.ProcessKillResults.Count(p => !p.Value.SafeToKill),
                SafeToKillProcesses = _database.ProcessKillResults.Count(p => p.Value.SafeToKill),
                TotalSnapshots = _database.Snapshots.Count
            },
            EssentialProcessBlacklist = GetEssentialProcesses().OrderBy(p => p).ToList(),
            SafeToKillWhitelist = GetSafeToKillProcesses().OrderBy(p => p).ToList(),
            DetailedResults = _database.ProcessKillResults.Values
                .OrderByDescending(p => p.TimesCausedCrash)
                .Select(p => new
                {
                    p.ProcessName,
                    p.TimesKilled,
                    p.TimesCausedCrash,
                    CrashRate = p.TimesKilled > 0 ? (p.TimesCausedCrash / (double)p.TimesKilled * 100) : 0,
                    p.SafeToKill,
                    p.RecommendedAction,
                    p.CrashReasons
                })
        };

        var json = JsonSerializer.Serialize(recommendations, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        File.WriteAllText(outputPath, json);
        
        Console.WriteLine($"\n📊 Process Learning Results Exported: {outputPath}");
        Console.WriteLine($"   Essential (must not kill): {GetEssentialProcesses().Count}");
        Console.WriteLine($"   Safe to kill: {GetSafeToKillProcesses().Count}");
    }

    public void SaveDatabase()
    {
        var json = JsonSerializer.Serialize(_database, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        File.WriteAllText(DatabasePath, json);
    }

    private ProcessLearningDatabase LoadDatabase()
    {
        if (File.Exists(DatabasePath))
        {
            try
            {
                var json = File.ReadAllText(DatabasePath);
                return JsonSerializer.Deserialize<ProcessLearningDatabase>(json) 
                    ?? new ProcessLearningDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load process learning database: {ex.Message}. Creating new one.");
                return new ProcessLearningDatabase();
            }
        }
        return new ProcessLearningDatabase();
    }

    private bool IsSystemProcess(Process process)
    {
        var systemProcesses = new[] { "System", "svchost", "services", "csrss", "lsass", "winlogon", "explorer" };
        return systemProcesses.Any(sp => process.ProcessName.ToLowerInvariant().Contains(sp.ToLowerInvariant()));
    }
}

public class ProcessLearningDatabase
{
    public List<ProcessSnapshot> Snapshots { get; set; } = new();
    public Dictionary<string, ProcessKillResult> ProcessKillResults { get; set; } = new();
}

public class ProcessSnapshot
{
    public DateTime Timestamp { get; set; }
    public string Phase { get; set; } = "";
    public long AvailableRamBytes { get; set; }
    public List<ProcessInfo> RunningProcesses { get; set; } = new();
}

public class ProcessInfo
{
    public string Name { get; set; } = "";
    public int Id { get; set; }
    public long MemoryMB { get; set; }
    public bool IsSystemProcess { get; set; }
}

public class ProcessKillResult
{
    public string ProcessName { get; set; } = "";
    public int TimesKilled { get; set; }
    public int TimesCausedCrash { get; set; }
    public bool SafeToKill { get; set; }
    public string RecommendedAction { get; set; } = "";
    public DateTime? LastSuccessfulKill { get; set; }
    public List<string> CrashReasons { get; set; } = new();
}
