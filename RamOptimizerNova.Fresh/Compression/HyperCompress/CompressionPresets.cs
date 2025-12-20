using System.Collections.Generic;
using RamOptimizer.Compression.HyperCompress.Encoders;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Predefined compression presets for different use cases and performance requirements.
/// </summary>
public static class CompressionPresets
{
    /// <summary>
    /// Fastest compression with acceptable ratios.
    /// Best for real-time or user-interactive scenarios.
    /// </summary>
    public static CompressionSettings Fast => new CompressionSettings
    {
        Level = 3,
        MaxMemoryMB = 128,
        EnableMultiThreading = true,
        CustomParameters = new Dictionary<string, object>
        {
            ["Preset"] = "Fast",
            ["LzmaLevel"] = 1,
            ["ModelOrder"] = 4,
            ["MemoryMB"] = 8,
            ["PpmdThreshold"] = 5L * 1024 * 1024, // 5MB
            ["ForceLZMA"] = false, // Allow auto-selection
            ["PpmdExtensions"] = new[] { ".txt", ".json", ".xml" } // Minimal PPMd use
        }
    };

    /// <summary>
    /// Balanced compression offering good ratios with reasonable speed.
    /// Recommended for most desktop applications.
    /// </summary>
    public static CompressionSettings Balanced => new CompressionSettings
    {
        Level = 10,
        MaxMemoryMB = 512,
        EnableMultiThreading = true,
        CustomParameters = new Dictionary<string, object>
        {
            ["Preset"] = "Balanced",
            ["LzmaLevel"] = 7,
            ["ModelOrder"] = 8,
            ["MemoryMB"] = 32,
            ["PpmdThreshold"] = 10L * 1024 * 1024, // 10MB
            ["ForceLZMA"] = false,
            ["PpmdExtensions"] = new[] { ".txt", ".log", ".json", ".xml", ".csv", ".md", ".html", ".css" }
        }
    };

    /// <summary>
    /// Maximum compression ratios with high resource usage.
    /// Best for archival storage where speed is less important.
    /// </summary>
    public static CompressionSettings Maximum => new CompressionSettings
    {
        Level = 22,
        MaxMemoryMB = 1024,
        EnableMultiThreading = true,
        CustomParameters = new Dictionary<string, object>
        {
            ["Preset"] = "Maximum",
            ["LzmaLevel"] = 9,
            ["ModelOrder"] = 12,
            ["MemoryMB"] = 128,
            ["PpmdThreshold"] = 50L * 1024 * 1024, // 50MB - wider PPMd usage
            ["ForceLZMA"] = false,
            ["PpmdExtensions"] = new[] { ".txt", ".log", ".json", ".xml", ".csv", ".md", ".html", ".css", ".js", ".py", ".cs", ".cpp", ".java" }
        }
    };

    /// <summary>
    /// Preset specifically optimized for game data compression.
    /// Balances speed and compression for gaming scenarios.
    /// </summary>
    public static CompressionSettings GameOptimized => new CompressionSettings
    {
        Level = 8,
        MaxMemoryMB = 256,
        EnableMultiThreading = true,
        CustomParameters = new Dictionary<string, object>
        {
            ["Preset"] = "GameOptimized",
            ["LzmaLevel"] = 6,
            ["ModelOrder"] = 6,
            ["MemoryMB"] = 16,
            ["PpmdThreshold"] = 2L * 1024 * 1024, // 2MB - favor LZMA for small files
            ["ForceLZMA"] = false,
            ["PpmdExtensions"] = new[] { ".txt", ".json", ".xml" }, // Limited text files
            ["FileTypePriorities"] = "binary_first" // Prefer binary algorithms
        }
    };

    /// <summary>
    /// Preset for document-heavy workloads.
    /// Optimizes for text and structured data.
    /// </summary>
    public static CompressionSettings DocumentOptimized => new CompressionSettings
    {
        Level = 15,
        MaxMemoryMB = 256,
        EnableMultiThreading = true,
        CustomParameters = new Dictionary<string, object>
        {
            ["Preset"] = "DocumentOptimized",
            ["LzmaLevel"] = 5,
            ["ForcePPMd"] = true, // Prefer PPMd for documents
            ["ModelOrder"] = 10,
            ["MemoryMB"] = 64,
            ["PpmdThreshold"] = 100L * 1024 * 1024, // 100MB - wide PPMd usage
            ["PpmdExtensions"] = new[] { ".txt", ".doc", ".docx", ".pdf", ".rtf", ".odt", ".log", ".json", ".xml", ".html", ".md" }
        }
    };

    /// <summary>
    /// Ultra-fast preset for minimal compression.
    /// Best when speed is critical and some compression is still desired.
    /// </summary>
    public static CompressionSettings UltraFast => new CompressionSettings
    {
        Level = 1,
        MaxMemoryMB = 32,
        EnableMultiThreading = false,
        CustomParameters = new Dictionary<string, object>
        {
            ["Preset"] = "UltraFast",
            ["LzmaLevel"] = 1,
            ["ModelOrder"] = 2, // Minimal PPMd
            ["MemoryMB"] = 4,
            ["PpmdThreshold"] = 1L * 1024 * 1024, // 1MB
            ["ForceLZMA"] = true // Force LZMA for consistency
        }
    };

    /// <summary>
    /// Get preset by name string.
    /// </summary>
    public static CompressionSettings GetByName(string presetName)
    {
        return presetName.ToLowerInvariant() switch
        {
            "fast" => Fast,
            "balanced" => Balanced,
            "maximum" or "max" => Maximum,
            "game" or "gaming" => GameOptimized,
            "document" or "docs" => DocumentOptimized,
            "ultrafast" or "speed" => UltraFast,
            _ => Balanced // Default fallback
        };
    }

    /// <summary>
    /// Get all available preset names.
    /// </summary>
    public static string[] GetAvailablePresets()
    {
        return new[]
        {
            "Fast",
            "Balanced",
            "Maximum",
            "GameOptimized",
            "DocumentOptimized",
            "UltraFast"
        };
    }

    /// <summary>
    /// Get preset description for UI display.
    /// </summary>
    public static string GetPresetDescription(string presetName)
    {
        return presetName.ToLowerInvariant() switch
        {
            "fast" => "Fast compression with moderate ratios (good for real-time usage)",
            "balanced" => "Best balance of speed and compression for general use",
            "maximum" or "max" => "Maximum compression ratios, slower but best for storage",
            "game" or "gaming" => "Optimized for game files and gaming scenarios",
            "document" or "docs" => "Optimized for text documents and structured data",
            "ultrafast" or "speed" => "Fastest possible compression with minimal CPU usage",
            _ => "Balanced compression settings"
        };
    }

    /// <summary>
    /// Get estimated compression ratio range for a preset (as percentage string).
    /// </summary>
    public static string GetPresetRatioRange(string presetName)
    {
        return presetName.ToLowerInvariant() switch
        {
            "fast" => "60-75%",
            "balanced" => "75-85%",
            "maximum" or "max" => "85-95%+",
            "game" or "gaming" => "70-85%",
            "document" or "docs" => "80-95%",
            "ultrafast" or "speed" => "50-65%",
            _ => "75-85%"
        };
    }

    /// <summary>
    /// Get estimated speed rating for a preset (1-5, higher is faster).
    /// </summary>
    public static int GetPresetSpeedRating(string presetName)
    {
        return presetName.ToLowerInvariant() switch
        {
            "fast" => 4,
            "balanced" => 3,
            "maximum" or "max" => 1,
            "game" or "gaming" => 3,
            "document" or "docs" => 2,
            "ultrafast" or "speed" => 5,
            _ => 3
        };
    }
}
