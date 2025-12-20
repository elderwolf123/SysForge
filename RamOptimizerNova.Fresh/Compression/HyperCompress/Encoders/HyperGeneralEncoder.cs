using System;
using System.IO;
using System.Text;
using ZstdSharp;

namespace RamOptimizer.Compression.HyperCompress.Encoders;

/// <summary>
/// General-purpose encoder for non-game files.
/// Uses multi-stage pipeline: Transform → Dictionary → Entropy encoding.
/// </summary>
public class HyperGeneralEncoder : IHyperEncoder
{
    public HyperAlgorithm Algorithm => HyperAlgorithm.HyperGeneral_Binary;
    
    public byte[] Compress(byte[] data, CompressionSettings settings)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        
        // Analyze data to select best strategy
        var analysis = AnalyzeData(data);
        
        writer.Write((byte)analysis.Strategy);
        
        byte[] transformed = analysis.Strategy switch
        {
            CompressionStrategy.HighEntropy => CompressHighEntropy(data, settings),
            CompressionStrategy.Text => CompressText(data, settings),
            CompressionStrategy.Binary => CompressBinary(data, settings),
            CompressionStrategy.Repetitive => CompressRepetitive(data, settings),
            _ => CompressBinary(data, settings)
        };
        
        writer.Write(transformed.Length);
        writer.Write(transformed);
        
        return output.ToArray();
    }
    
    public byte[] Decompress(byte[] compressed)
    {
        using var ms = new MemoryStream(compressed);
        using var reader = new BinaryReader(ms);
        
        var strategy = (CompressionStrategy)reader.ReadByte();
        int compressedLen = reader.ReadInt32();
        byte[] compressedData = reader.ReadBytes(compressedLen);
        
        // All strategies use Zstd, so decompress uniformly
        using var decompressor = new Decompressor();
        return decompressor.Unwrap(compressedData).ToArray();
    }
    
    public float EstimateRatio(byte[] sample)
    {
        var analysis = AnalyzeData(sample);
        
        return analysis.Strategy switch
        {
            CompressionStrategy.HighEntropy => 0.95f, // Already compressed
            CompressionStrategy.Text => 0.30f, // Text compresses very well
            CompressionStrategy.Binary => 0.55f, // Binary average
            CompressionStrategy.Repetitive => 0.25f, // Repetitive data compresses excellently
            _ => 0.60f
        };
    }
    
    public bool IsSuitable(byte[] data, string fileName)
    {
        // General encoder is suitable for anything not handled by specialized encoders
        return true;
    }
    
    /// <summary>
    /// Analyze data characteristics to select compression strategy.
    /// </summary>
    private DataAnalysis AnalyzeData(byte[] data)
    {
        var analysis = new DataAnalysis();
        
        // Calculate entropy
        analysis.Entropy = CalculateEntropy(data);
        
        // Check if text-like (high proportion of printable ASCII)
        analysis.IsTextLike = IsTextLike(data);
        
        // Check repetition
        analysis.Repetition = CalculateRepetition(data);
        
        // Select strategy
        if (analysis.Entropy > 7.5f)
            analysis.Strategy = CompressionStrategy.HighEntropy;
        else if (analysis.IsTextLike)
            analysis.Strategy = CompressionStrategy.Text;
        else if (analysis.Repetition > 0.4f)
            analysis.Strategy = CompressionStrategy.Repetitive;
        else
            analysis.Strategy = CompressionStrategy.Binary;
        
        return analysis;
    }
    
    /// <summary>
    /// Compress high-entropy data (already compressed or encrypted).
    /// Use minimal compression to avoid overhead.
    /// </summary>
    private byte[] CompressHighEntropy(byte[] data, CompressionSettings settings)
    {
        // Use fast LZ4-like compression level
        using var compressor = new Compressor(3);
        return compressor.Wrap(data).ToArray();
    }
    
    /// <summary>
    /// Compress text data.
    /// Text benefits from good dictionary and entropy coding.
    /// </summary>
    private byte[] CompressText(byte[] data, CompressionSettings settings)
    {
        // High compression level for text
        using var compressor = new Compressor(Math.Min(settings.Level + 3, 22));
        
        // Could add BWT (Burrows-Wheeler Transform) here for even better compression
        // For now, rely on Zstd's excellent text handling
        
        return compressor.Wrap(data).ToArray();
    }
    
    /// <summary>
    /// Compress binary data (default strategy).
    /// </summary>
    private byte[] CompressBinary(byte[] data, CompressionSettings settings)
    {
        using var compressor = new Compressor(settings.Level);
        return compressor.Wrap(data).ToArray();
    }
    
    /// <summary>
    /// Compress repetitive data.
    /// High repetition benefits from larger dictionary.
    /// </summary>
    private byte[] CompressRepetitive(byte[] data, CompressionSettings settings)
    {
        // Use higher compression with larger window
        using var compressor = new Compressor(Math.Min(settings.Level + 2, 22));
        return compressor.Wrap(data).ToArray();
    }
    
    /// <summary>
    /// Calculate Shannon entropy.
    /// </summary>
    private float CalculateEntropy(byte[] data)
    {
        if (data.Length == 0) return 0;
        
        var freq = new int[256];
        foreach (byte b in data)
            freq[b]++;
        
        double entropy = 0;
        foreach (var count in freq)
        {
            if (count == 0) continue;
            double probability = (double)count / data.Length;
            entropy -= probability * Math.Log(probability, 2);
        }
        
        return (float)entropy;
    }
    
    /// <summary>
    /// Check if data is text-like (high proportion of printable characters).
    /// </summary>
    private bool IsTextLike(byte[] data)
    {
        if (data.Length == 0) return false;
        
        int printable = 0;
        int sampleSize = Math.Min(4096, data.Length);
        
        for (int i = 0; i < sampleSize; i++)
        {
            byte b = data[i];
            // Printable ASCII + common whitespace
            if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13)
                printable++;
        }
        
        return (float)printable / sampleSize > 0.7f;
    }
    
    /// <summary>
    /// Calculate repetition ratio.
    /// </summary>
    private float CalculateRepetition(byte[] data)
    {
        if (data.Length < 128) return 0;
        
        const int blockSize = 64;
        var seen = new HashSet<ulong>();
        int totalBlocks = 0;
        int uniqueBlocks = 0;
        
        for (int i = 0; i <= data.Length - blockSize; i += blockSize)
        {
            ulong hash = ComputeHash(data, i, blockSize);
            totalBlocks++;
            
            if (seen.Add(hash))
                uniqueBlocks++;
        }
        
        return totalBlocks > 0 ? 1.0f - ((float)uniqueBlocks / totalBlocks) : 0;
    }
    
    /// <summary>
    /// Compute FNV-1a hash for a block.
    /// </summary>
    private ulong ComputeHash(byte[] data, int offset, int length)
    {
        const ulong FnvPrime = 1099511628211;
        const ulong FnvOffsetBasis = 14695981039346656037;
        
        ulong hash = FnvOffsetBasis;
        int end = Math.Min(offset + length, data.Length);
        
        for (int i = offset; i < end; i++)
        {
            hash ^= data[i];
            hash *= FnvPrime;
        }
        
        return hash;
    }
    
    private class DataAnalysis
    {
        public float Entropy { get; set; }
        public bool IsTextLike { get; set; }
        public float Repetition { get; set; }
        public CompressionStrategy Strategy { get; set; }
    }
    
    private enum CompressionStrategy : byte
    {
        Binary = 0,
        Text = 1,
        HighEntropy = 2,
        Repetitive = 3
    }
}
