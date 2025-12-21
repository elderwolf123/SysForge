using System;
using System.IO;
using System.Threading.Tasks;
using ZstdSharp;

namespace RamOptimizerNova.Services;

public class CompressionService : IDisposable
{
    private readonly Compressor _compressor;
    private readonly Decompressor _decompressor;

    public CompressionService()
    {
        _compressor = new Compressor(22); // Compression level 22 (max)
        _decompressor = new Decompressor();
    }

    public async Task<CompressionResult> CompressFileAsync(string inputPath, string outputPath)
    {
        try
        {
            var inputBytes = await File.ReadAllBytesAsync(inputPath);
            var originalSize = inputBytes.Length;

            var compressedBytes = _compressor.Wrap(inputBytes);
            var compressedSize = compressedBytes.Length;

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
            var decompressedBytes = _decompressor.Unwrap(compressedBytes);

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

    public byte[] CompressBytes(byte[] data)
    {
        return _compressor.Wrap(data);
    }

    public byte[] DecompressBytes(byte[] data)
    {
        return _decompressor.Unwrap(data);
    }

    public void Dispose()
    {
        _compressor?.Dispose();
        _decompressor?.Dispose();
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
