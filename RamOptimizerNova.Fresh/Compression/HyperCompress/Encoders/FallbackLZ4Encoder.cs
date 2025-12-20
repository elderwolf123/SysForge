using K4os.Compression.LZ4;
using System;

namespace RamOptimizer.Compression.HyperCompress.Encoders;

/// <summary>
/// Fallback encoder using LZ4 for fast compression.
/// Used when no specialized encoder is suitable.
/// </summary>
public class FallbackLZ4Encoder : IHyperEncoder
{
    public HyperAlgorithm Algorithm => HyperAlgorithm.Fallback_LZ4;
    
    public byte[] Compress(byte[] data, CompressionSettings settings)
    {
        // LZ4 high compression
        var target = new byte[LZ4Codec.MaximumOutputSize(data.Length)];
        var encodedLength = LZ4Codec.Encode(
            data, 0, data.Length,
            target, 0, target.Length,
            LZ4Level.L12_MAX
        );
        
        // Trim to actual size
        var result = new byte[encodedLength];
        Array.Copy(target, result, encodedLength);
        
        return result;
    }
    
    public byte[] Decompress(byte[] compressed)
    {
        // Need to know original size - store it in first 4 bytes
        // (This is a simplified implementation)
        throw new NotImplementedException("Decompression requires size header - to be implemented with archive format");
    }
    
    public float EstimateRatio(byte[] sample)
    {
        if (sample.Length < 1024) return 0.9f; // Assume minimal compression for small samples
        
        // Quick test compression
        var target = new byte[LZ4Codec.MaximumOutputSize(sample.Length)];
        var encodedLength = LZ4Codec.Encode(
            sample, 0, sample.Length,
            target, 0, target.Length,
            LZ4Level.L03_HC
        );
        
        return (float)encodedLength / sample.Length;
    }
    
    public bool IsSuitable(byte[] data, string fileName)
    {
        // LZ4 is always suitable as fallback
        return true;
    }
}
