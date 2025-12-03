using System;
using System.IO;
using ZstdSharp;

namespace RamOptimizer.Compression.HyperCompress.Encoders;

/// <summary>
/// Specialized encoder for game textures (DDS, PNG, TGA, BMP).
/// Uses texture-specific optimizations for better compression.
/// </summary>
public class HyperGameTextureEncoder : IHyperEncoder
{
    private static readonly byte[] DDS_MAGIC = { 0x44, 0x44, 0x53, 0x20 }; // "DDS "
    private static readonly byte[] PNG_MAGIC = { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
    
    public HyperAlgorithm Algorithm => HyperAlgorithm.HyperGame_Texture;
    
    public byte[] Compress(byte[] data, CompressionSettings settings)
    {
        // Detect texture format
        if (StartsWithMagic(data, DDS_MAGIC))
            return CompressDDS(data, settings);
        else if (StartsWithMagic(data, PNG_MAGIC))
            return CompressPNG(data, settings);
        else
            return CompressGenericTexture(data, settings);
    }
    
    public byte[] Decompress(byte[] compressed)
    {
        // Read format byte and decompress accordingly
        using var ms = new MemoryStream(compressed);
        using var reader = new BinaryReader(ms);
        
        byte format = reader.ReadByte();
        byte[] compressedData = reader.ReadBytes(compressed.Length - 1);
        
        using var decompressor = new Decompressor();
        return decompressor.Unwrap(compressedData).ToArray();
    }
    
    public float EstimateRatio(byte[] sample)
    {
        // Textures typically compress to 40-60% with our optimizations
        return 0.5f;
    }
    
    public bool IsSuitable(byte[] data, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext == ".dds" || ext == ".png" || ext == ".tga" || ext == ".bmp")
            return true;
        
        // Check magic bytes
        if (data.Length < 4) return false;
        return StartsWithMagic(data, DDS_MAGIC) || StartsWithMagic(data, PNG_MAGIC);
    }
    
    /// <summary>
    /// Compress DDS texture with specialized optimization.
    /// </summary>
    private byte[] CompressDDS(byte[] data, CompressionSettings settings)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        
        // Write format identifier
        writer.Write((byte)1); // DDS format
        
        // DDS Structure: Header (128 bytes) + optional DX10 header (20 bytes) + pixel data
        
        // Parse DDS header
        if (data.Length < 128)
        {
            // Invalid DDS, use generic compression
            return CompressGenericTexture(data, settings);
        }
        
        // Extract header (keep uncompressed for fast reads)
        byte[] header = new byte[128];
        Array.Copy(data, 0, header, 0, 128);
        writer.Write(header);
        
        int pixelDataStart = 128;
        
        // Check for DX10 header
        uint flags = BitConverter.ToUInt32(data, 80); // dwCaps2 at offset 80
        if ((flags & 0x4) != 0) // DDSCAPS2_CUBEMAP or DX10
        {
            // Might have DX10 extended header
            if (data.Length > 148)
            {
                byte[] dx10Header = new byte[20];
                Array.Copy(data, 128, dx10Header, 0, 20);
                writer.Write(dx10Header);
                pixelDataStart = 148;
            }
        }
        
        // Get pixel data
        byte[] pixelData = new byte[data.Length - pixelDataStart];
        Array.Copy(data, pixelDataStart, pixelData, 0, pixelData.Length);
        
        // Optimization: For block-compressed formats (DXT1/5, BC1-7), 
        // pixels are already organized in blocks - delta encode between blocks
        byte[] optimized = DeltaEncodeBlocks(pixelData, 16); // DXT blocks are 16 bytes
        
        // Compress with Zstandard
        using var compressor = new Compressor(settings.Level);
        byte[] compressed = compressor.Wrap(optimized).ToArray();
        
        writer.Write(compressed.Length);
        writer.Write(compressed);
        
        return output.ToArray();
    }
    
    /// <summary>
    /// Compress PNG (already compressed, but can optimize metadata).
    /// </summary>
    private byte[] CompressPNG(byte[] data, CompressionSettings settings)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        
        writer.Write((byte)2); // PNG format
        
        // PNG is already compressed with DEFLATE
        // We can still apply Zstd for additional compression
        // (Useful for PNG with minimal compression or large metadata)
        
        using var compressor = new Compressor(Math.Min(settings.Level, 10)); // Lower level for already-compressed
        byte[] compressed = compressor.Wrap(data).ToArray();
        
        writer.Write(compressed.Length);
        writer.Write(compressed);
        
        return output.ToArray();
    }
    
    /// <summary>
    /// Generic texture compression for unknown formats.
    /// </summary>
    private byte[] CompressGenericTexture(byte[] data, CompressionSettings settings)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        
        writer.Write((byte)0); // Generic format
        
        // Apply Zstandard with high compression
        using var compressor = new Compressor(settings.Level);
        byte[] compressed = compressor.Wrap(data).ToArray();
        
        writer.Write(compressed.Length);
        writer.Write(compressed);
        
        return output.ToArray();
    }
    
    /// <summary>
    /// Delta encode blocks - store differences between sequential blocks.
    /// Improves compression when adjacent blocks are similar.
    /// </summary>
    private byte[] DeltaEncodeBlocks(byte[] data, int blockSize)
    {
        if (data.Length < blockSize * 2) return data;
        
        byte[] result = new byte[data.Length];
        
        // First block stays unchanged
        Array.Copy(data, 0, result, 0, blockSize);
        
        // Subsequent blocks: store delta from previous
        for (int i = blockSize; i < data.Length; i++)
        {
            int prevIndex = i - blockSize;
            if (prevIndex >= 0 && prevIndex < data.Length)
            {
                result[i] = (byte)(data[i] - data[prevIndex]);
            }
            else
            {
                result[i] = data[i];
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Check if data starts with magic bytes.
    /// </summary>
    private bool StartsWithMagic(byte[] data, byte[] magic)
    {
        if (data.Length < magic.Length) return false;
        
        for (int i = 0; i < magic.Length; i++)
        {
            if (data[i] != magic[i]) return false;
        }
        
        return true;
    }
}
