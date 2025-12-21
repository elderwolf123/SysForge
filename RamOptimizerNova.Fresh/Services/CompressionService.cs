using System;
using System.IO;
using System.Threading.Tasks;
using ZstdSharp;

namespace RamOptimizerNova.Services;

public class CompressionService : IDisposable
{
    public async Task<CompressionResult> CompressFileAsync(string inputPath, string outputPath)
    {
        try
        {
            var inputBytes = await File.ReadAllBytesAsync(inputPath);
            var originalSize = inputBytes.Length;

            // Use ZstdSharp static methods
            var compressedBytes = new byte[Compressor.GetCompressBoundLong((ulong)inputBytes.Length)];
            var compressedSize = Compressor.Compress(compressedBytes, inputBytes, 22); // Level 22

            // Trim to actual size
            Array.Resize(ref compressedBytes, (int)compressedSize);

            await File.WriteAllBytesAsync(outputPath, compressedBytes);

            return new CompressionResult
            {
                Success = true,
                OriginalSize = originalSize,
                CompressedSize = compressedSize,
                CompressionRatio = (double)compressedSize / originalSize,
                Savings = originalSize - compressedSize,
                OutputPath = outputPath
            };
        }
        catch (Exception ex)
        {
            return new CompressionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<DecompressionResult> DecompressFileAsync(string inputPath, string outputPath)
    {
        try
        {
            var compressedBytes = await File.ReadAllBytesAsync(inputPath);
            
            // Get decompressed size
            var decompressedSize = Decompressor.GetDecompressedSize(compressedBytes);
            var decompressedBytes = new byte[decompressedSize];
            
            // Decompress
            Decompressor.Decompress(decompressedBytes, compressedBytes);

            await File.WriteAllBytesAsync(outputPath, decompressedBytes);

            return new DecompressionResult
            {
                Success = true,
                OriginalSize = compressedBytes.Length,
                DecompressedSize = decompressedSize,
                OutputPath = outputPath
            };
        }
        catch (Exception ex)
        {
            return new DecompressionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public byte[] CompressBytes(byte[] data, int level = 22)
    {
        var compressedBytes = new byte[Compressor.GetCompressBoundLong((ulong)data.Length)];
        var size = Compressor.Compress(compressedBytes, data, level);
        Array.Resize(ref compressedBytes, (int)size);
        return compressedBytes;
    }

    public byte[] DecompressBytes(byte[] data)
    {
        var size = Decompressor.GetDecompressedSize(data);
        var decompressedBytes = new byte[size];
        Decompressor.Decompress(decompressedBytes, data);
        return decompressedBytes;
    }

    public void Dispose()
    {
        // No resources to dispose with static methods
    }
}

public class CompressionResult
{
    public bool Success { get; set; }
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public long Savings { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public string? ErrorMessage {get; set; }
}

public class DecompressionResult
{
    public bool Success { get; set; }
    public long OriginalSize { get; set; }
    public long DecompressedSize { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
