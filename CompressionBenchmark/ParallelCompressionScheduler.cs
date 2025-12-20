using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompressionBenchmark;

/// <summary>
/// Manages parallel compression testing with intelligent RAM-aware scheduling.
/// Dynamically adjusts parallelism based on file sizes and available RAM.
/// </summary>
public class ParallelCompressionScheduler
{
    private readonly RamSafetyChecker _ramChecker;
    private readonly long _reservedRamBytes;

    public ParallelCompressionScheduler(long reservedRamBytes)
    {
        _ramChecker = new RamSafetyChecker();
        _reservedRamBytes = reservedRamBytes;
    }

    /// <summary>
    /// Calculate optimal parallelism based on file sizes and available RAM.
    /// </summary>
    public int CalculateParallelism(List<long> fileSizes)
    {
        var availableRam = _ramChecker.GetAvailableRamBytes() - _reservedRamBytes;
        
        if (fileSizes.Count == 0)
            return 1;

        var avgFileSize = (long)fileSizes.Average();
        var maxFileSize = fileSizes.Max();
        
        // Conservative estimate: each compression uses 3x file size in RAM
        // (original + compressed + working memory)
        var estimatedRamPerFile = avgFileSize * 3;
        
        // Calculate how many files we can safely process in parallel
        var maxParallel = Math.Max(1, (int)(availableRam / estimatedRamPerFile));
        
        // Cap at CPU core count for efficiency
        var cpuCores = Environment.ProcessorCount;
        maxParallel = Math.Min(maxParallel, cpuCores);
        
        // Small files: Use more parallelism (up to 2x cores)
        if (avgFileSize < 1024 * 1024) // <1MB
        {
            maxParallel = Math.Min(cpuCores * 2, maxParallel);
        }
        // Large files: Be more conservative
        else if (avgFileSize > 100 * 1024 * 1024) // >100MB
        {
            maxParallel = Math.Max(1, maxParallel / 2);
        }
        
        return Math.Max(1, Math.Min(maxParallel, 16)); // Cap at 16 for stability
    }

    /// <summary>
    /// Get RAM status for logging
    /// </summary>
    public void LogRamStatus(int parallelism)
    {
        var available = _ramChecker.GetAvailableRamBytes();
        var usable = available - _reservedRamBytes;
        
        Console.WriteLine($"\n⚡ PARALLEL PROCESSING:");
        Console.WriteLine($"   Available RAM: {FormatBytes(available)}");
        Console.WriteLine($"   Reserved: {FormatBytes(_reservedRamBytes)}");
        Console.WriteLine($"   Usable: {FormatBytes(usable)}");
        Console.WriteLine($"   Parallelism: {parallelism} concurrent tests");
        Console.WriteLine($"   CPU Cores: {Environment.ProcessorCount}");
        Console.WriteLine();
        
        BenchmarkLogger.LogInfo($"Parallel mode: {parallelism} concurrent tests, {FormatBytes(usable)} RAM available");
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
