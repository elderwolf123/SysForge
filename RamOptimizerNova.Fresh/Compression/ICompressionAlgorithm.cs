using System;
using System.IO;
using System.Threading.Tasks;

namespace RamOptimizer.Compression
{
    /// <summary>
    /// Compression tier selection for hybrid system
    /// </summary>
    public enum CompressionTier
    {
        Tier1_WindowsCompact,  // ~55% transparent via Windows Compact
        Tier2_VirtualFS,       // ~75% transparent via WinFsp (coming soon)
        Tier3_UltraArchive     // ~90% cold storage (future)
    }

    /// <summary>
    /// Compression mode selection
    /// </summary>
    public enum CompressionMode
    {
        Standard,  // Fast and reliable (LZ4, Zstd, Brotli)
        Ultra      // Maximum compression (advanced algorithms)
    }

    /// <summary>
    /// Compression level for standard algorithms
    /// </summary>
    public enum CompressionLevel
    {
        Fastest = 1,
        Fast = 3,
        Balanced = 5,
        Good = 10,
        Best = 15,
        Maximum = 22
    }

    /// <summary>
    /// Result of a compression operation
    /// </summary>
    public class CompressionResult
    {
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio => CompressedSize / (double)OriginalSize;
        public double SpaceSaved => 1.0 - CompressionRatio;
        public string Algorithm { get; set; } = string.Empty;
        public TimeSpan CompressionTime { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Base interface for all compression algorithms
    /// </summary>
    public interface ICompressionAlgorithm
    {
        string Name { get; }
       Task<CompressionResult> CompressAsync(Stream input, Stream output, int level);
        Task DecompressAsync(Stream input, Stream output);
    }
}
