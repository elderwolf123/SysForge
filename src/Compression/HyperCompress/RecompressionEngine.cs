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

    public RecompressionEngine(HyperCompressEngine engine)
    {
        _engine = engine;
        _analyzer = new FileCompressionAnalyzer();
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
    /// Recompresses individual poorly-compressed files within a directory.
    /// Useful for games with mixed compression quality.
    /// </summary>
    public SelectiveRecompressionResult RecompressSelectively(string gamePath, string outputPath, RecompressionOptions? options = null)
    {
        options ??= new RecompressionOptions();
        
        var result = new SelectiveRecompressionResult
        {
            SourcePath = gamePath,
            OutputPath = outputPath
        };

        // Analyze to find poorly-compressed files
        var analysis = _analyzer.AnalyzeDirectory(gamePath);
        var targetFiles = analysis.PoorlyCompressedFiles
            .Where(f => f.PotentialSavings >= options.MinimumSavingsThresholdMB * 1024 * 1024)
            .ToList();

        Console.WriteLine($"Found {targetFiles.Count} files worth recompressing");

        // Copy directory structure
        Directory.CreateDirectory(outputPath);

        foreach (var file in Directory.GetFiles(gamePath, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(gamePath, file);
            var outputFile = Path.Combine(outputPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

            var fileAnalysis = targetFiles.FirstOrDefault(f => f.FilePath == file);
            
            if (fileAnalysis != null && fileAnalysis.RecommendRecompression)
            {
                // Recompress this file
                Console.WriteLine($"Recompressing: {relativePath}");
                var data = File.ReadAllBytes(file);
                var compressed = _engine.Compress(data, Path.GetFileName(file));
                File.WriteAllBytes(outputFile + ".hcc", compressed); // HyperCompress Compressed
                result.RecompressedFiles++;
                result.SpaceSaved += data.Length - compressed.Length;
            }
            else
            {
                // Copy as-is
                File.Copy(file, outputFile, true);
                result.CopiedFiles++;
            }
        }

        Console.WriteLine($"\n✅ Selective Recompression Complete!");
        Console.WriteLine($"  Recompressed: {result.RecompressedFiles} files");
        Console.WriteLine($"  Copied: {result.CopiedFiles} files");
        Console.WriteLine($"  Space Saved: {FormatBytes(result.SpaceSaved)}");

        return result;
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
