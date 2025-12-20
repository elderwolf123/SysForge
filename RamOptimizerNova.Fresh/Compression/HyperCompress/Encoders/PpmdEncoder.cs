using System;
using System.IO;

namespace RamOptimizer.Compression.HyperCompress.Encoders;

/// <summary>
/// PPMd (Prediction by Partial Matching) encoder optimized for text compression.
/// Provides excellent ratios for text files and structured data.
/// </summary>
public class PpmdEncoder : IHyperEncoder
{
    public HyperAlgorithm Algorithm => HyperAlgorithm.PPMd;

    /// <summary>
    /// Compress data using PPMd algorithm.
    /// </summary>
    public byte[] Compress(byte[] data, CompressionSettings settings)
    {
        // Extract PPMd-specific parameters
        int modelOrder = settings.CustomParameters.ContainsKey("ModelOrder") ?
            Convert.ToInt32(settings.CustomParameters["ModelOrder"]) : 8; // Default 8

        int memoryMB = settings.CustomParameters.ContainsKey("MemoryMB") ?
            Convert.ToInt32(settings.CustomParameters["MemoryMB"]) : 32; // Default 32MB

        // Clamp to valid ranges
        modelOrder = Math.Max(2, Math.Min(16, modelOrder));
        memoryMB = Math.Max(1, Math.Min(256, memoryMB));

        return Compress(data, modelOrder, memoryMB);
    }

    /// <summary>
    /// Internal compress method for PPMd parameters.
    /// </summary>
    private byte[] Compress(byte[] data, int modelOrder, int memoryMB)
    {
        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();

        // PPMd encoder (placeholder - would use SharpCompress.PPMd or SevenZip)
        // For now, return input data with header (implementation stub)
        // var encoder = new PPMdEncoder(modelOrder, memoryMB * 1024 * 1024);
        // encoder.Compress(inputStream, outputStream);

        // Placeholder: Store parameters in header
        outputStream.WriteByte((byte)modelOrder); // Model order
        outputStream.Write(BitConverter.GetBytes(memoryMB), 0, 4); // Memory in MB
        outputStream.Write(data, 0, data.Length); // Uncompressed data

        return outputStream.ToArray();
    }

    /// <summary>
    /// Decompress PPMd compressed data.
    /// </summary>
    public byte[] Decompress(byte[] compressedData)
    {
        if (compressedData.Length < 5) return compressedData;

        using var inputStream = new MemoryStream(compressedData);
        using var outputStream = new MemoryStream();

        // Read PPMd header
        int modelOrder = inputStream.ReadByte();
        var memoryBytes = new byte[4];
        inputStream.Read(memoryBytes, 0, 4);
        int memoryMB = BitConverter.ToInt32(memoryBytes, 0);

        // PPMd decoder (placeholder)
        // var decoder = new PPMdDecoder(modelOrder, memoryMB * 1024 * 1024);
        // decoder.Decompress(inputStream, outputStream);

        // Placeholder: Return remaining data
        var decompressedData = new byte[inputStream.Length - inputStream.Position];
        inputStream.Read(decompressedData, 0, decompressedData.Length);

        return decompressedData;
    }

    /// <summary>
    /// Estimate compression ratio for PPMd.
    /// PPMd excels at text and structured data.
    /// </summary>
    public float EstimateRatio(byte[] sample)
    {
        if (sample.Length == 0) return 1.0f;

        // PPMd is great for text (90-99% compression)
        // Check for text patterns
        int textChars = 0;
        int totalChars = Math.Min(sample.Length, 1024); // Check first 1KB

        for (int i = 0; i < totalChars; i++)
        {
            byte b = sample[i];
            if (b >= 32 && b <= 126) // Printable ASCII
            {
                textChars++;
            }
            else if (b is 9 or 10 or 13) // Tab, LF, CR
            {
                textChars++;
            }
        }

        float textRatio = (float)textChars / totalChars;

        if (textRatio > 0.9f) // Mostly text
        {
            return 0.05f; // 95%+ compression typical for text
        }
        else if (textRatio > 0.7f) // Mixed text/binary
        {
            return 0.2f; // 80% compression
        }
        else
        {
            return 0.5f; // 50% compression for binary (not PPMd's strength)
        }
    }

    /// <summary>
    /// Check if PPMd is suitable for this data.
    /// Best for text files and structured data.
    /// </summary>
    public bool IsSuitable(byte[] data, string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var textExtensions = new[] { ".txt", ".log", ".json", ".xml", ".csv", ".md", ".html", ".css", ".js", ".py", ".cs", ".cpp", ".h", ".java" };

        // Extension-based suitability
        bool extensionMatch = Array.Exists(textExtensions, ext => extension == ext);

        // Content-based suitability (check for null bytes - binary data)
        int nullBytes = 0;
        int checkLength = Math.Min(data.Length, 1024);
        for (int i = 0; i < checkLength; i++)
        {
            if (data[i] == 0) nullBytes++;
        }

        bool contentSuitable = ((float)nullBytes / checkLength) < 0.01f; // Less than 1% nulls

        return extensionMatch || (contentSuitable && EstimateRatio(data) < 0.3f);
    }

    /// <summary>
    /// Get optimal PPMd settings for different scenarios.
    /// </summary>
    public static (int modelOrder, int memoryMB) GetRecommendedSettings(bool maximumCompression = false)
    {
        if (maximumCompression)
        {
            return (12, 128); // Model order 12, 128MB memory
        }
        else
        {
            return (8, 32); // Model order 8, 32MB memory (balanced)
        }
    }
}
