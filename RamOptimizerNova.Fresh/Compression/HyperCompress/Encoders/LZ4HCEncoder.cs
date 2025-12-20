using K4os.Compression.LZ4;
using System;

namespace RamOptimizer.Compression.HyperCompress.Encoders;

/// <summary>
/// LZ4 High Compression encoder using K4os.Compression.LZ4 library.
/// Provides better compression ratios than standard LZ4 while maintaining good speed.
/// </summary>
public class LZ4HCEncoder : IHyperEncoder
{
    public HyperAlgorithm Algorithm => HyperAlgorithm.HyperGeneral_Text;
    
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
        // LZ4 decompression - need to know original size
        // For now, throw exception as this requires more complex implementation
        throw new NotImplementedException("LZ4-HC decompression requires size header - to be implemented with archive format");
    }
    
    public float EstimateRatio(byte[] sample)
    {
        if (sample.Length < 1024) return 0.85f; // Assume minimal compression for small samples
        
        // Quick test compression
        var target = new byte[LZ4Codec.MaximumOutputSize(sample.Length)];
        var encodedLength = LZ4Codec.Encode(
            sample, 0, sample.Length,
            target, 0, target.Length,
            LZ4Level.L12_MAX
        );
        
        return (float)encodedLength / sample.Length;
    }
    
    public bool IsSuitable(byte[] data, string fileName)
    {
        // LZ4-HC is suitable for most data types, especially when speed is important
        return true;
    }
}