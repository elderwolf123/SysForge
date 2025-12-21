using System;
using System.IO;
using System.Threading.Tasks;

namespace RamOptimizerNova.Services;

public enum CompressionLevel
{
    Fast = 1,        // Zstd Level 3 - Fast compression, okay ratio
    Balanced = 9,    // Zstd Level 9 - Good balance
    Maximum = 22     // Zstd Level 22 - Best compression
}

public class CompressionService : IDisposable
{
    public async Task<CompressionResult> CompressFileAsync(string inputPath, string outputPath, CompressionLevel level = CompressionLevel.Balanced)
    {
        try
        {
            var inputBytes = await File.ReadAllBytesAsync(inputPath);
            var originalSize = inputBytes.Length;

            // Use simple System.IO.Compression for now - works reliably
            var compressedBytes = await CompressBytesAsync(inputBytes, (int)level);
            
            await File.WriteAllBytesAsync(outputPath, compressedBytes);

            return new CompressionResult
            {
                Success = true,
                OriginalSize = originalSize,
                CompressedSize = compressedBytes.Length,
                CompressionRatio = (double)compressedBytes.Length / originalSize,
                Savings = originalSize - compressedBytes.Length,
                OutputPath = outputPath,
                Level = level
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
            var decompressedBytes = await DecompressBytesAsync(compressedBytes);

            await File.WriteAllBytesAsync(outputPath, decompressedBytes);

            return new DecompressionResult
            {
                Success = true,
                OriginalSize = compressedBytes.Length,
                DecompressedSize = decompressedBytes.Length,
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

    private async Task<byte[]> CompressBytesAsync(byte[] data, int level)
    {
        using var outputStream = new MemoryStream();
        using (var compressionStream = new System.IO.Compression.GZipStream(outputStream, 
            System.IO.Compression.CompressionLevel.SmallestSize))
        {
            await compressionStream.WriteAsync(data, 0, data.Length);
        }
        return outputStream.ToArray();
    }

    private async Task<byte[]> DecompressBytesAsync(byte[] data)
    {
        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using (var decompressionStream = new System.IO.Compression.GZipStream(inputStream, 
            System.IO.Compression.CompressionMode.Decompress))
        {
            await decompressionStream.CopyToAsync(outputStream);
        }
        return outputStream.ToArray();
    }

    public void Dispose()
    {
        // No resources to dispose
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
    public CompressionLevel Level { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DecompressionResult
{
    public bool Success { get; set; }
    public long OriginalSize { get; set; }
    public long DecompressedSize { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
