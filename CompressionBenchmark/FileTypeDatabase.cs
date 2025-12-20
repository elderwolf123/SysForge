using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CompressionBenchmark;

/// <summary>
/// Tracks file types and compression test results across all drives.
/// Builds a database of what has been tested vs what needs testing.
/// </summary>
public class FileTypeDatabase
{
    private const string DatabasePath = "compression_database.json";
    private Dictionary<string, FileTypeEntry> _database;

    public FileTypeDatabase()
    {
        _database = LoadDatabase();
    }

    public void AddDiscoveredFileType(string extension, string? samplePath = null, long fileSize = 0)
    {
        extension = extension.ToLowerInvariant();
        
        if (!_database.ContainsKey(extension))
        {
            _database[extension] = new FileTypeEntry
            {
                Extension = extension,
                FirstSeen = DateTime.Now,
                SamplePaths = samplePath != null ? new List<string> { samplePath } : new(),
                TestStatus = TestStatus.NotTested
            };
            Console.WriteLine($"🆕 Discovered new file type: {extension}");
        }
        else if (samplePath != null && !_database[extension].SamplePaths.Contains(samplePath))
        {
            // Add additional samples for size variety
            _database[extension].SamplePaths.Add(samplePath);
        }
    }

    public void MarkAsTested(string extension, CompressionResults results)
    {
        extension = extension.ToLowerInvariant();
        
        if (_database.TryGetValue(extension, out var entry))
        {
            entry.TestStatus = TestStatus.Tested;
            entry.LastTested = DateTime.Now;
            entry.Results = results;
            entry.TestCount++;
            
            // Auto-save after each test for crash recovery
            SaveDatabase();
        }
    }
    
    public void MarkAsFailed(string extension, string reason = "Unknown error")
    {
        extension = extension.ToLowerInvariant();
        
        if (_database.TryGetValue(extension, out var entry))
        {
            entry.TestStatus = TestStatus.Failed;
            BenchmarkLogger.LogError($"Failed to test {extension}: {reason}");
            SaveDatabase();
        }
    }
    
    public void MarkAsInProgress(string extension)
    {
        extension = extension.ToLowerInvariant();
        if (_database.TryGetValue(extension, out var entry))
        {
            entry.TestStatus = TestStatus.InProgress;
            SaveDatabase(); // Save state immediately
        }
    }
    
    public bool CheckForIncompleteSession()
    {
        // Check if any file types are marked as "InProgress" - indicates crash
        var inProgress = _database.Values.Where(e => e.TestStatus == TestStatus.InProgress).ToList();
        
        if (inProgress.Count > 0)
        {
            Console.WriteLine($"\n⚠️  INCOMPLETE SESSION DETECTED");
            Console.WriteLine($"   Found {inProgress.Count} file types that were being tested when interrupted");
            Console.WriteLine($"   These will be marked as 'Not Tested' and retried");
            
            foreach (var entry in inProgress)
            {
                entry.TestStatus = TestStatus.NotTested;
                BenchmarkLogger.LogWarning($"Resetting {entry.Extension} from InProgress to NotTested (recovery)");
            }
            
            SaveDatabase();
            return true;
        }
        
        return false;
    }

    public List<string> GetUntestedFileTypes()
    {
        return _database
            .Where(kvp => kvp.Value.TestStatus == TestStatus.NotTested)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public List<FileTypeEntry> GetEntriesWithStatus(TestStatus status)
    {
        return _database
            .Where(kvp => kvp.Value.TestStatus == status)
            .Select(kvp => kvp.Value)
            .ToList();
    }

    public FileTypeEntry? GetEntry(string extension)
    {
        extension = extension.ToLowerInvariant();
        return _database.TryGetValue(extension, out var entry) ? entry : null;
    }

    public List<string> GetAllFileTypes()
    {
        return _database.Keys.ToList();
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
        Console.WriteLine($"💾 Database saved: {_database.Count} file types tracked");
    }

    private Dictionary<string, FileTypeEntry> LoadDatabase()
    {
        if (File.Exists(DatabasePath))
        {
            try
            {
                var json = File.ReadAllText(DatabasePath);
                var db = JsonSerializer.Deserialize<Dictionary<string, FileTypeEntry>>(json);
                Console.WriteLine($"📂 Loaded database: {db?.Count ?? 0} file types");
                return db ?? new Dictionary<string, FileTypeEntry>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load database: {ex.Message}. Creating new one");
            }
        }
        
        return new Dictionary<string, FileTypeEntry>();
    }

    public void PrintSummary()
    {
        var tested = _database.Count(kvp => kvp.Value.TestStatus == TestStatus.Tested);
        var untested = _database.Count(kvp => kvp.Value.TestStatus == TestStatus.NotTested);
        var inProgress = _database.Count(kvp => kvp.Value.TestStatus == TestStatus.InProgress);

        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("FILE TYPE DATABASE SUMMARY");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"✅ Tested:     {tested}");
        Console.WriteLine($"⏳ In Progress: {inProgress}");
        Console.WriteLine($"❌ Not Tested: {untested}");
        Console.WriteLine($"📊 Total:      {_database.Count}");
        Console.WriteLine(new string('=', 60) + "\n");
    }

    public List<FileTypeEntry> GetFailedEntries()
    {
        return _database.Values.Where(e => e.TestStatus == TestStatus.Failed).ToList();
    }
}

public class FileTypeEntry
{
    public string Extension { get; set; } = "";
    public DateTime FirstSeen { get; set; }
    public DateTime? LastTested { get; set; }
    public TestStatus TestStatus { get; set; }
    public List<string> SamplePaths { get; set; } = new(); // Multiple samples
    public CompressionResults? Results { get; set; } // Overall average
    public List<SizeBracketResult> SizeBracketResults { get; set; } = new(); // Results by size
    public int TestCount { get; set; }
}

public class SizeBracketResult
{
    public string SizeBracket { get; set; } = ""; // "Small", "Medium", "Large", "Huge"
    public long MinSize { get; set; }
    public long MaxSize { get; set; }
    public CompressionResults? Results { get; set; }
}

public enum TestStatus
{
    NotTested,
    InProgress,
    Tested,
    Failed
}

public class CompressionResults
{
    public Dictionary<string, AlgorithmResult> Results { get; set; } = new();
    public string BestAlgorithm { get; set; } = "";
    public double BestRatio { get; set; }
    public long OriginalSize { get; set; }
}

public class AlgorithmResult
{
    public long CompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public long CompressionTimeMs { get; set; }
    public long DecompressionTimeMs { get; set; }
    public long MemoryUsedBytes { get; set; }
}
