using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Stores learned compression patterns and optimal strategies for file types.
/// Allows HyperCompress to improve over time by learning what works best.
/// </summary>
public class HyperCompressLearningDatabase
{
    private readonly string _databasePath;
    private readonly ILogger? _logger;
    private Dictionary<string, LearnedPattern> _patterns;
    
    public HyperCompressLearningDatabase(string? databasePath = null, ILogger? logger = null)
    {
        _databasePath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RamOptimizer",
            "HyperCompressPatterns.json"
        );
        
        _logger = logger;
        _patterns = new Dictionary<string, LearnedPattern>();
        
        LoadPatterns();
    }
    
    /// <summary>
    /// Learn from a compression result.
    /// </summary>
    public void LearnFromCompression(string fileName, byte[] originalData, HyperAlgorithm algorithmUsed, float compressionRatio)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            extension = GetMagicBytesKey(originalData);
        
        var key = extension;
        
        if (!_patterns.TryGetValue(key, out var pattern))
        {
            pattern = new LearnedPattern
            {
                FileExtension = extension,
                MagicBytes = originalData.Take(16).ToArray(),
                Observations = new List<CompressionObservation>()
            };
            _patterns[key] = pattern;
        }
        
        // Add observation
        pattern.Observations.Add(new CompressionObservation
        {
            Algorithm = algorithmUsed,
            CompressionRatio = compressionRatio,
            Timestamp = DateTime.UtcNow
        });
        
        // Keep only last 20 observations per pattern
        if (pattern.Observations.Count > 20)
        {
            pattern.Observations = pattern.Observations
                .OrderByDescending(o => o.Timestamp)
                .Take(20)
                .ToList();
        }
        
        // Update best algorithm
        pattern.BestAlgorithm = pattern.Observations
            .GroupBy(o => o.Algorithm)
            .Select(g => new { Algorithm = g.Key, AvgRatio = g.Average(o => o.CompressionRatio) })
            .OrderBy(x => x.AvgRatio)
            .First()
            .Algorithm;
        
        pattern.AverageRatio = pattern.Observations.Average(o => o.CompressionRatio);
        pattern.SampleCount = pattern.Observations.Count;
        
        _logger?.LogDebug($"Learned: {extension} → {pattern.BestAlgorithm} ({pattern.AverageRatio:P2})");
        
        SavePatterns();
    }
    
    /// <summary>
    /// Get the best known algorithm for a file type.
    /// </summary>
    public HyperAlgorithm? GetBestAlgorithm(string fileName, byte[] sample)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        // Try extension first
        if (!string.IsNullOrEmpty(extension) && _patterns.TryGetValue(extension, out var pattern))
        {
            if (pattern.SampleCount >= 3) // Need at least 3 samples to trust
                return pattern.BestAlgorithm;
        }
        
        // Try magic bytes
        var magicKey = GetMagicBytesKey(sample);
        if (_patterns.TryGetValue(magicKey, out var magicPattern))
        {
            if (magicPattern.SampleCount >= 3)
                return magicPattern.BestAlgorithm;
        }
        
        return null; // Unknown, let HyperCompressEngine decide
    }
    
    /// <summary>
    /// Get statistics about learned patterns.
    /// </summary>
    public LearningStatistics GetStatistics()
    {
        return new LearningStatistics
        {
            TotalPatterns = _patterns.Count,
            TotalObservations = _patterns.Values.Sum(p => p.SampleCount),
            BestOverallRatio = _patterns.Values.Any() ? _patterns.Values.Min(p => p.AverageRatio) : 1.0f,
            MostCommonAlgorithm = _patterns.Values
                .GroupBy(p => p.BestAlgorithm)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? HyperAlgorithm.HyperGeneral_Binary
        };
    }
    
    /// <summary>
    /// Clear all learned patterns.
    /// </summary>
    public void Clear()
    {
        _patterns.Clear();
        SavePatterns();
        _logger?.LogInformation("Cleared all learned compression patterns");
    }
    
    private void LoadPatterns()
    {
        if (!File.Exists(_databasePath))
        {
            _logger?.LogDebug($"No existing pattern database found at {_databasePath}");
            return;
        }
        
        try
        {
            var json = File.ReadAllText(_databasePath);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, LearnedPattern>>(json);
            
            if (loaded != null)
            {
                _patterns = loaded;
                _logger?.LogInformation($"Loaded {_patterns.Count} compression patterns");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load compression patterns database");
        }
    }
    
    private void SavePatterns()
    {
        try
        {
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(_patterns, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(_databasePath, json);
            _logger?.LogDebug($"Saved {_patterns.Count} patterns to database");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save compression patterns database");
        }
    }
    
    private string GetMagicBytesKey(byte[] data)
    {
        if (data.Length < 4) return "unknown";
        
        // Use first 4 bytes as hex key
        return $"magic:{BitConverter.ToString(data, 0, Math.Min(4, data.Length))}";
    }
}

/// <summary>
/// A learned compression pattern for a file type.
/// </summary>
public class LearnedPattern
{
    public string FileExtension { get; set; } = string.Empty;
    public byte[] MagicBytes { get; set; } = Array.Empty<byte>();
    public HyperAlgorithm BestAlgorithm { get; set; }
    public float AverageRatio { get; set; }
    public int SampleCount { get; set; }
    public List<CompressionObservation> Observations { get; set; } = new();
}

/// <summary>
/// A single compression observation.
/// </summary>
public class CompressionObservation
{
    public HyperAlgorithm Algorithm { get; set; }
    public float CompressionRatio { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Statistics about the learning database.
/// </summary>
public class LearningStatistics
{
    public int TotalPatterns { get; set; }
    public int TotalObservations { get; set; }
    public float BestOverallRatio { get; set; }
    public HyperAlgorithm MostCommonAlgorithm { get; set; }
}
