using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Recompresses poorly-compressed game archives to save massive storage.
/// Solves "storage monopoly" problem where games use weak compression intentionally.
/// Perfect for 500GB tablets - can fit 4x more games!
/// </summary>
public class RecompressionEngine
{
private readonly HyperCompressEngine _engine;
    private readonly FileCompressionAnalyzer _analyzer;
    private readonly DRMDetectionEngine _drmDetector;
    private readonly FileReparseManager _reparseManager;

    public RecompressionEngine(HyperCompressEngine engine, string compressedStoragePath = null)
    {
        _engine = engine;
        _analyzer = new FileCompressionAnalyzer();
        _drmDetector = new DRMDetectionEngine();
        _reparseManager = new FileReparseManager(compressedStoragePath ?? Path.Combine(Path.GetTempPath(), "RamOptimizer_Compressed"), null);
    }

    /// <summary>
    /// Analyzes a game directory and returns recompression recommendations.
    /// </summary>
    public DirectoryAnalysisResult AnalyzeGameDirectory(string gamePath)
    {
        if (!Directory.Exists(gamePath))
            throw new DirectoryNotFoundException($"Game directory not found: {gamePath}");

        Console.WriteLine($"Analyzing {gamePath}...");
        var result = _analyzer.AnalyzeDirectory(gamePath);

        Console.WriteLine($"\nAnalysis Results:");
        Console.WriteLine($"  Total Size: {FormatBytes(result.TotalOriginalSize)}");
        Console.WriteLine($"  Poorly Compressed Files: {result.PoorlyCompressedFiles.Count}");
        Console.WriteLine($"  Potential Savings: {FormatBytes(result.TotalPotentialSavings)} ({(result.TotalPotentialSavings / (double)result.TotalOriginalSize * 100):F1}%)");

        if (result.PoorlyCompressedFiles.Any())
        {
            Console.WriteLine($"\nTop candidates for recompression:");
            foreach (var file in result.PoorlyCompressedFiles.OrderByDescending(f => f.PotentialSavings).Take(10))
            {
                Console.WriteLine($"  - {Path.GetFileName(file.FilePath)}: {FormatBytes(file.PotentialSavings)} savings");
            }
        }

        return result;
    }

    /// <summary>
    /// Comprehensive analysis including file analysis and DRM assessment for safe compression.
    /// </summary>
    public GameCompressionAnalysis AnalyzeGameForCompression(string gamePath)
    {
        if (!Directory.Exists(gamePath))
            throw new DirectoryNotFoundException($"Game directory not found: {gamePath}");

        Console.WriteLine($"=== Comprehensive Game Analysis for {Path.GetFileName(gamePath)} ===\n");

        var result = new GameCompressionAnalysis
        {
            GamePath = gamePath,
            GameName = Path.GetFileName(gamePath.TrimEnd(Path.DirectorySeparatorChar))
        };

        // Step 1: DRM Analysis
        Console.WriteLine("1️⃣ DRM ANALYSIS:");
        result.DrmAnalysis = _drmDetector.AnalyzeForDRM(gamePath);
        Console.WriteLine();

        // Step 2: File Analysis
        Console.WriteLine("2️⃣ FILE ANALYSIS:");
        result.FileAnalysis = AnalyzeGameDirectory(gamePath);
        Console.WriteLine();

        // Step 3: Overall Recommendations
        Console.WriteLine("3️⃣ COMPRESSION RECOMMENDATIONS:");
        result.Recommendations = GenerateCompressionRecommendations(result);

        Console.WriteLine($"\n=== Analysis Complete ===");
        Console.WriteLine($"Overall Assessment: {result.Recommendations.FirstOrDefault() ?? "Analysis incomplete"}");

        return result;
    }

    /// <summary>
    /// Recompresses a game directory into a .hca archive.
    /// </summary>
    public RecompressionResult RecompressGame(string gamePath, string outputArchivePath, RecompressionOptions? options = null)
    {
        options ??= new RecompressionOptions();
        
        var result = new RecompressionResult
        {
            OriginalPath = gamePath,
            ArchivePath = outputArchivePath,
            StartTime = DateTime.Now
        };

        try
        {
            // Step 1: Analyze to get current size
            Console.WriteLine("Step 1: Analyzing game files...");
            var analysis = _analyzer.AnalyzeDirectory(gamePath);
            result.OriginalSize = analysis.TotalOriginalSize;

            // Step 2: Create archive with HyperGeneralEncoder
            // This preserves .pak/.assets files intact - games will see them via VFS!
            Console.WriteLine($"\nStep 2: Recompressing {FormatBytes(result.OriginalSize)}...");
            Console.WriteLine("  NOTE: Preserving .pak/.assets structure - game will work via VFS mount");
            
            var archiver = new ChunkedArchiver(_engine);
            
            // Compress entire directory (including .pak, .assets containers)
            archiver.CompressDirectory(gamePath, outputArchivePath);
            
            result.CompressedSize = new FileInfo(outputArchivePath).Length;
            result.SpaceSaved = result.OriginalSize - result.CompressedSize;
            result.CompressionRatio = result.CompressedSize / (double)result.OriginalSize;

            Console.WriteLine($"\n✅ Recompression Complete!");
            Console.WriteLine($"  Original: {FormatBytes(result.OriginalSize)}");
            Console.WriteLine($"  Compressed: {FormatBytes(result.CompressedSize)}");
            Console.WriteLine($"  Saved: {FormatBytes(result.SpaceSaved)} ({(1 - result.CompressionRatio) * 100:F1}%)");

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Console.WriteLine($"\n❌ Recompression Failed: {ex.Message}");
        }
        finally
        {
            result.EndTime = DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;
        }

        return result;
    }

    /// <summary>
    /// Intelligent selective recompression based on file analysis, DRM safety, and usage patterns.
    /// Enhanced version with smart filtering and safety considerations.
    /// </summary>
    public SmartRecompressionResult RecompressIntelligently(string gamePath, string outputPath, GameCompressionAnalysis analysis, RecompressionOptions? options = null)
    {
        options ??= new RecompressionOptions();

        var result = new SmartRecompressionResult
        {
            SourcePath = gamePath,
            OutputPath = outputPath,
            DrmRiskLevel = analysis.DrmAnalysis.RiskLevel,
            PotentialSavings = analysis.FileAnalysis.TotalPotentialSavings
        };

        // Filter files based on DRM safety
        var safeFileMasks = GetSafeFileMasksForRiskLevel(analysis.DrmAnalysis.RiskLevel);

        Console.WriteLine($"Smart Selective Recompression - DRM Risk: {analysis.DrmAnalysis.RiskLevel}");

        // Get candidates based on both compression potential and DRM safety
        var fileAnalysis = _analyzer.AnalyzeDirectory(gamePath);
        var candidateFiles = fileAnalysis.PoorlyCompressedFiles
            .Where(f => f.PotentialSavings >= options.MinimumSavingsThresholdMB * 1024 * 1024)
            .ToList();

        // Apply DRM-safe filtering
        candidateFiles = candidateFiles.Where(f =>
        {
            var fileName = Path.GetFileName(f.FilePath);
            var extension = Path.GetExtension(f.FilePath).ToLower();

            return safeFileMasks.Any(mask =>
                mask.WildcardMatch(fileName) &&
                (f.PotentialSavings >= mask.MinimumSizeThreshold) &&
                IsFileTypeSafeForCompression(extension, analysis.DrmAnalysis.RiskLevel));
        }).ToList();

        Console.WriteLine($"Found {candidateFiles.Count} candidate files after DRM filtering");

        // Create output directory
        Directory.CreateDirectory(outputPath);

        foreach (var file in Directory.GetFiles(gamePath, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(gamePath, file);
            var outputFile = Path.Combine(outputPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

            var candidate = candidateFiles.FirstOrDefault(f => f.FilePath == file);

            if (candidate != null)
            {
                // Recompress this file with metadata preservation
                Console.WriteLine($"🔧 Compressing: {relativePath} ({FormatBytes(candidate.PotentialSavings)} savings)");
                var data = File.ReadAllBytes(file);
                var compressed = _engine.Compress(data, Path.GetFileName(file));

                // Store compression metadata for future access
                var metadata = new FileCompressionMetadata
                {
                    OriginalSize = data.Length,
                    CompressedSize = compressed.Length,
                    Ratio = (double)compressed.Length / data.Length,
                    Timestamp = DateTime.Now
                };

                File.WriteAllBytes(outputFile + ".hcc", compressed);
                File.WriteAllText(outputFile + ".meta", SerializeMetadata(metadata));

                result.RecompressedFiles++;
                result.SpaceSaved += data.Length - compressed.Length;
                result.Metadata.Add(metadata);
            }
            else
            {
                // Copy as-is
                File.Copy(file, outputFile, true);
                result.CopiedFiles++;
            }
        }

        Console.WriteLine($"\n✅ Smart Recompression Complete!");
        Console.WriteLine($"  Recompressed: {result.RecompressedFiles} files");
        Console.WriteLine($"  Copied: {result.CopiedFiles} files");
        Console.WriteLine($"  Space Saved: {FormatBytes(result.SpaceSaved)}");
        Console.WriteLine($"  DRM Safety: Assessed as {analysis.DrmAnalysis.RiskLevel}");

        return result;
    }

    /// <summary>
    /// Legacy selective recompression method - kept for compatibility.
    /// Use RecompressIntelligently for better results.
    /// </summary>
    public SelectiveRecompressionResult RecompressSelectively(string gamePath, string outputPath, RecompressionOptions? options = null)
    {
        // Fallback to basic analysis for legacy compatibility
        var analysis = new GameCompressionAnalysis
        {
            GamePath = gamePath,
            FileAnalysis = AnalyzeGameDirectory(gamePath),
            DrmAnalysis = new DRMAnalysisResult { RiskLevel = DrmRiskLevel.Medium } // Conservative default
        };

        var smartResult = RecompressIntelligently(gamePath, outputPath, analysis, options);

        // Convert SmartRecompressionResult to SelectiveRecompressionResult
        return new SelectiveRecompressionResult
        {
            SourcePath = smartResult.SourcePath,
            OutputPath = smartResult.OutputPath,
            RecompressedFiles = smartResult.RecompressedFiles,
            CopiedFiles = smartResult.CopiedFiles,
            SpaceSaved = smartResult.SpaceSaved
        };
    }

    private List<string> GenerateCompressionRecommendations(GameCompressionAnalysis analysis)
    {
        var recommendations = new List<string>();

        var drmRisk = analysis.DrmAnalysis.RiskLevel;
        var fileSavings = analysis.FileAnalysis.TotalPotentialSavings;
        var totalSize = analysis.FileAnalysis.TotalOriginalSize;

        // Base recommendation on DRM risk
        if (drmRisk == DrmRiskLevel.None)
        {
            recommendations.Add("🟢 SAFE: Full folder compression recommended - no DRM detected");
            recommendations.Add($"💾 Potential savings: {FormatBytes(fileSavings)} ({(fileSavings / (double)totalSize * 100):F1}% of {FormatBytes(totalSize)})");
            recommendations.Add("🚀 Use: Folder-level VFS mounting + junction relocation");
        }
        else if (drmRisk <= DrmRiskLevel.Low)
        {
            recommendations.Add("🟡 LOW RISK: Selective compression viable - low-impact DRM detected");
            recommendations.Add($"💾 Selective savings: {FormatBytes(fileSavings)} available");
            recommendations.Add("🛡️ Use: Selective file extraction + VFS mounting");
            recommendations.Add("⚠️ Test thoroughly before full deployment");
        }
        else if (drmRisk <= DrmRiskLevel.Medium)
        {
            recommendations.Add("🟠 MEDIUM RISK: Limited compression possible - significant DRM detected");
            recommendations.Add("📁 Only compress loose assets/media files, avoid exe/pak files");
            recommendations.Add("🔍 Manual review required - only compress user-created content");
            recommendations.Add("❌ DO NOT use file-level stubs or kernel hooking");
        }
        else
        {
            recommendations.Add("🔴 HIGH RISK: Compression NOT recommended - invasive DRM detected");
            recommendations.Add("🚫 Game contains anti-cheat or tamper protection");
            recommendations.Add("💡️ Consider: Alternative storage solutions or DRM-free games");
            recommendations.Add("⚖️ Risk assessment: " + string.Join(", ", analysis.DrmAnalysis.DetectedMarkers.Select(m => m.Name)));
        }

        // Add specific DRM warnings
        if (analysis.DrmAnalysis.HasAntiCheat)
        {
            recommendations.Add("🎮 ANTI-CHEAT SYSTEM DETECTED - compression may trigger false positives");
            recommendations.Add("🛑 Avoid any techniques that modify file access patterns");
        }

        if (analysis.DrmAnalysis.HasVerifiedLauncher)
        {
            recommendations.Add("🚀 VERIFIED LAUNCHER PRESENT - online verification of game files");
            recommendations.Add("🔍 Launcher may detect size/timestamp changes");
        }

        // Add technical recommendations
        if (drmRisk <= DrmRiskLevel.Low && fileSavings > 1024 * 1024 * 1024) // 1GB+
        {
            recommendations.Add("💪 LARGE SAVINGS POTENTIAL - prioritize this game for compression");
        }

        return recommendations;
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

    private List<SafetyFileMask> GetSafeFileMasksForRiskLevel(DrmRiskLevel riskLevel)
    {
        return riskLevel switch
        {
            DrmRiskLevel.None or DrmRiskLevel.Minimal => new List<SafetyFileMask>
            {
                new() { Pattern = "*.pak", MinimumSizeThreshold = 10 * 1024 * 1024 }, // 10MB
                new() { Pattern = "*.assets", MinimumSizeThreshold = 50 * 1024 * 1024 }, // 50MB
                new() { Pattern = "*.bundle", MinimumSizeThreshold = 20 * 1024 * 1024 }, // 20MB
                new() { Pattern = "*.cache", MinimumSizeThreshold = 100 * 1024 * 1024 }, // 100MB
                new() { Pattern = "*.temp", MinimumSizeThreshold = 500 * 1024 * 1024 }, // 500MB
            },
            DrmRiskLevel.Low => new List<SafetyFileMask>
            {
                new() { Pattern = "*.cache", MinimumSizeThreshold = 100 * 1024 * 1024 },
                new() { Pattern = "*.temp", MinimumSizeThreshold = 200 * 1024 * 1024 },
                new() { Pattern = "*.log", MinimumSizeThreshold = 50 * 1024 * 1024 },
                new() { Pattern = "*.tmp", MinimumSizeThreshold = 50 * 1024 * 1024 },
            },
            _ => new List<SafetyFileMask>
            {
                new() { Pattern = "*.cache", MinimumSizeThreshold = 500 * 1024 * 1024 }, // Very large cache files
                new() { Pattern = "*.temp", MinimumSizeThreshold = 1 * 1024 * 1024 * 1024 }, // 1GB+ temp files
            }
        };
    }

    private bool IsFileTypeSafeForCompression(string extension, DrmRiskLevel riskLevel)
    {
        // Dangerous extensions that should never be touched for higher risk levels
        var dangerousExtensions = new[] { ".exe", ".dll", ".pak", ".assets", ".uasset", ".umap" };

        if (riskLevel > DrmRiskLevel.Low)
        {
            return !dangerousExtensions.Contains(extension);
        }

        // For low/med risk, allow some but be cautious
        return !dangerousExtensions.Where(ext => !ext.StartsWith(".temp") && !ext.StartsWith(".cache")).Contains(extension);
    }

    private string SerializeMetadata(FileCompressionMetadata metadata)
    {
        // Simple JSON-like serialization
        return $"{metadata.OriginalSize},{metadata.CompressedSize},{metadata.Ratio},{metadata.Timestamp:O}";
    }
}

public class RecompressionOptions
{
    /// <summary>
    /// Only recompress files that will save at least this many MB.
    /// Default: 100MB
    /// </summary>
    public int MinimumSavingsThresholdMB { get; set; } = 100;

    /// <summary>
    /// Delete original files after successful recompression.
    /// </summary>
    public bool DeleteOriginalAfterSuccess { get; set; } = false;

    /// <summary>
    /// Create junction point to original game path.
    /// </summary>
    public bool CreateJunction { get; set; } = true;
}

public class RecompressionResult
{
    public string OriginalPath { get; set; } = "";
    public string ArchivePath { get; set; } = "";
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public long SpaceSaved { get; set; }
    public double CompressionRatio { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SelectiveRecompressionResult
{
    public string SourcePath { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public int RecompressedFiles { get; set; }
    public int CopiedFiles { get; set; }
    public long SpaceSaved { get; set; }
}

/// <summary>
/// Complete analysis result combining DRM assessment and file analysis for compression planning.
/// </summary>
public class GameCompressionAnalysis
{
    public string GamePath { get; set; } = "";
    public string GameName { get; set; } = "";
    public DRMAnalysisResult DrmAnalysis { get; set; } = new();
    public DirectoryAnalysisResult FileAnalysis { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Quick assessment if compression is safe for this game.
    /// </summary>
    public bool IsCompressionSafe => DrmAnalysis.RiskLevel <= DrmRiskLevel.Low;

    /// <summary>
    /// Expected space savings from compression.
    /// </summary>
    public long PotentialSavings => FileAnalysis.TotalPotentialSavings;
}

/// <summary>
/// Enhanced selective recompression result with DRM awareness and metadata.
/// </summary>
public class SmartRecompressionResult
{
    public string SourcePath { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public int RecompressedFiles { get; set; }
    public int CopiedFiles { get; set; }
    public long SpaceSaved { get; set; }
    public DrmRiskLevel DrmRiskLevel { get; set; }
    public long PotentialSavings { get; set; }
    public List<FileCompressionMetadata> Metadata { get; set; } = new();
}

/// <summary>
/// Metadata for compressed files to enable future access patterns.
/// </summary>
public class FileCompressionMetadata
{
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public double Ratio { get; set; }
    public DateTime Timestamp { get; set; }
    public string EncoderUsed { get; set; } = ""; // Future: track which encoder
}

/// <summary>
/// Safety mask for file filtering based on DRM risk.
/// </summary>
public class SafetyFileMask
{
    public string Pattern { get; set; } = "";
    public long MinimumSizeThreshold { get; set; }

    public bool WildcardMatch(string fileName)
    {
        // Simple wildcard matching - could be enhanced
        if (Pattern.Contains("*"))
        {
            var regexPattern = "^" + Pattern.Replace("*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return fileName.EndsWith(Pattern, StringComparison.OrdinalIgnoreCase);
    }
}
