using System;
using System.IO;
using System.IO.Compression;

namespace CompressionBenchmark;

/// <summary>
/// Brotli compression encoder (built into .NET, no external dependencies)
/// Excellent for text and web content, often better than Zstd for certain file types.
/// </summary>
public class BrotliEncoder
{
    public byte[] Compress(byte[] data, int quality)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, (CompressionLevel)quality))
        {
            brotli.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    public byte[] Decompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }
}

/// <summary>
/// Bzip2 compression using SharpCompress (already in dependencies)
/// Good for certain file types, sometimes better than other algorithms.
/// </summary>
public class Bzip2Encoder
{
    public byte[] Compress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        using (var bzip2 = new SharpCompress.Compressors.BZip2.BZip2Stream(output, SharpCompress.Compressors.CompressionMode.Compress, false))
        {
            input.CopyTo(bzip2);
        }
        return output.ToArray();
    }

    public byte[] Decompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var output = new MemoryStream();
        using (var bzip2 = new SharpCompress.Compressors.BZip2.BZip2Stream(input, SharpCompress.Compressors.CompressionMode.Decompress, false))
        {
            bzip2.CopyTo(output);
        }
        return output.ToArray();
    }
}

/// <summary>
/// LZ4 High Compression variant - slower but better compression than standard LZ4.
/// </summary>
public class LZ4HCEncoder
{
    public byte[] Compress(byte[] data, int level)
    {
        var maxCompressedLength = K4os.Compression.LZ4.LZ4Codec.MaximumOutputSize(data.Length);
        var compressed = new byte[maxCompressedLength];
        
        var compressedLength = K4os.Compression.LZ4.LZ4Codec.Encode(
            data, 0, data.Length,
            compressed, 0, compressed.Length,
            (K4os.Compression.LZ4.LZ4Level)level);
        
        Array.Resize(ref compressed, compressedLength);
        return compressed;
    }

    public byte[] Decompress(byte[] compressed, int originalLength)
    {
        var decompressed = new byte[originalLength];
        K4os.Compression.LZ4.LZ4Codec.Decode(
            compressed, 0, compressed.Length,
            decompressed, 0, originalLength);
        return decompressed;
    }
}
