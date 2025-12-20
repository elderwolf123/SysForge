using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Analyzes files to detect poor compression and recommend recompression.
/// Targets: Unity .assets, Unreal .pak, UE .bundle - often weakly compressed.
/// </summary>
public class FileCompressionAnalyzer
{
    public FileAnalysisResult AnalyzeFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("File not found", filePath);

        var result = new FileAnalysisResult
        {
            FilePath = filePath,
            OriginalSize = fileInfo.Length,
            FileType = DetectFileType(filePath)
        };

        // Sample file for entropy analysis
        var sample = ReadFileSample(filePath, Math.Min(1024 * 1024, fileInfo.Length)); // 1MB sample
        result.Entropy = CalculateEntropy(sample);
        result.IsPoorlyCompressed = IsPoorlyCompressed(result.FileType, result.Entropy, fileInfo.Length);
        
        if (result.IsPoorlyCompressed)
        {
            result.EstimatedRecompressedSize = EstimateRecompressedSize(fileInfo.Length, result.Entropy);
            result.PotentialSavings = result.OriginalSize - result.EstimatedRecompressedSize;
            result.RecommendRecompression = result.PotentialSavings > 100 * 1024 * 1024; // >100MB savings
        }

        return result;
    }

    public DirectoryAnalysisResult AnalyzeDirectory(string directoryPath)
    {
        var result = new DirectoryAnalysisResult
        {
            DirectoryPath = directoryPath,
            Files = new List<FileAnalysisResult>()
        };

        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            try
            {
                var analysis = AnalyzeFile(file);
                result.Files.Add(analysis);
                
                if (analysis.IsPoorlyCompressed)
                {
                    result.PoorlyCompressedFiles.Add(analysis);
                    result.TotalPotentialSavings += analysis.PotentialSavings;
                }
            }
            catch
            {
                // Skip files that can't be analyzed
            }
        }

        result.TotalOriginalSize = result.Files.Sum(f => f.OriginalSize);
        result.TotalEstimatedSize = result.Files.Sum(f => f.EstimatedRecompressedSize);

        return result;
    }

    private GameFileType DetectFileType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var filename = Path.GetFileName(filePath).ToLowerInvariant();

        return ext switch
        {
            ".pak" => GameFileType.UnrealPak,
            ".bundle" when filename.Contains("unity") => GameFileType.UnityBundle,
            ".assets" => GameFileType.UnityAssets,
            ".resource" => GameFileType.UnityResource,
            ".resS" => GameFileType.UnityResourceS,
            ".dat" when filename.Contains("game") => GameFileType.GameData,
            ".bin" when filename.Contains("data") => GameFileType.GameData,
            ".archive" => GameFileType.GenericArchive,
            _ => GameFileType.Unknown
        };
    }

    private bool IsPoorlyCompressed(GameFileType fileType, double entropy, long fileSize)
    {
        // Files larger than 10MB with high entropy are likely poorly compressed
        if (fileSize < 10 * 1024 * 1024)
            return false;

        return fileType switch
        {
            GameFileType.UnrealPak => entropy > 6.5, // .pak often uses weak LZ4
            GameFileType.UnityBundle => entropy > 6.0, // Unity often uses LZMA but weak settings
            GameFileType.UnityAssets => entropy > 7.0, // .assets often uncompressed
            GameFileType.GameData => entropy > 6.5,
            _ => entropy > 7.5 // High entropy = poorly/uncompressed
        };
    }

    private long EstimateRecompressedSize(long originalSize, double entropy)
    {
        // Conservative estimates based on entropy
        double compressionRatio = entropy switch
        {
            < 2.0 => 0.02, // 98% compression
            < 4.0 => 0.10, // 90% compression
            < 6.0 => 0.22, // 78% compression (HyperGeneral baseline)
            < 7.0 => 0.40, // 60% compression
            < 7.5 => 0.60, // 40% compression
            _ => 0.85      // 15% compression (already compressed)
        };

        return (long)(originalSize * compressionRatio);
    }

    private byte[] ReadFileSample(string filePath, long bytesToRead)
    {
        using var fs = File.OpenRead(filePath);
        var buffer = new byte[bytesToRead];
        fs.Read(buffer, 0, buffer.Length);
        return buffer;
    }

    private double CalculateEntropy(byte[] data)
    {
        var freq = new int[256];
        foreach (byte b in data)
            freq[b]++;

        double entropy = 0.0;
        foreach (int count in freq)
        {
            if (count == 0) continue;
            double p = count / (double)data.Length;
            entropy -= p * Math.Log(p, 2);
        }

        return entropy;
    }
}

public class FileAnalysisResult
{
    public string FilePath { get; set; } = "";
    public long OriginalSize { get; set; }
    public long EstimatedRecompressedSize { get; set; }
    public long PotentialSavings { get; set; }
    public double Entropy { get; set; }
    public GameFileType FileType { get; set; }
    public bool IsPoorlyCompressed { get; set; }
    public bool RecommendRecompression { get; set; }
}

public class DirectoryAnalysisResult
{
    public string DirectoryPath { get; set; } = "";
    public List<FileAnalysisResult> Files { get; set; } = new();
    public List<FileAnalysisResult> PoorlyCompressedFiles { get; set; } = new();
    public long TotalOriginalSize { get; set; }
    public long TotalEstimatedSize { get; set; }
    public long TotalPotentialSavings { get; set; }
}

public enum GameFileType
{
    Unknown,
    UnrealPak,
    UnityBundle,
    UnityAssets,
    UnityResource,
    UnityResourceS,
    GameData,
    GenericArchive
}
