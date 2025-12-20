using System;
using System.IO;
using System.Linq;

namespace RamOptimizer.Compression.HyperCompress.Encoders;

/// <summary>
/// Hybrid LZMA2 + PPMd encoder that automatically selects the best algorithm.
/// Combines LZMA2's general compression with PPMd's text excellence.
/// </summary>
public class HybridUltraEncoder : IHyperEncoder
{
    private readonly Lzma2Encoder _lzmaEncoder;
    private readonly PpmdEncoder _ppmdEncoder;

    public HyperAlgorithm Algorithm => HyperAlgorithm.HybridUltra;

    public HybridUltraEncoder()
    {
        _lzmaEncoder = new Lzma2Encoder();
        _ppmdEncoder = new PpmdEncoder();
    }

    /// <summary>
    /// Compress using the best algorithm for the data.
    /// </summary>
    public byte[] Compress(byte[] data, CompressionSettings settings)
    {
        // Determine which algorithm to use
        bool usePPMd = ShouldUsePPMd(data, settings);

        IHyperEncoder algorithm = usePPMd ? _ppmdEncoder : _lzmaEncoder;
        var compressedData = algorithm.Compress(data, settings);

        // Add algorithm marker to the output
        using var output = new MemoryStream();
        output.WriteByte((byte)(usePPMd ? 1 : 0)); // 0 = LZMA2, 1 = PPMd
        output.Write(compressedData, 0, compressedData.Length);

        return output.ToArray();
    }

    /// <summary>
    /// Decompress hybrid compressed data.
    /// </summary>
    public byte[] Decompress(byte[] compressedData)
    {
        if (compressedData.Length == 0) return compressedData;

        // Read algorithm marker
        bool usePPMd = compressedData[0] == 1;
        var actualCompressed = compressedData.Skip(1).ToArray();

        IHyperEncoder algorithm = usePPMd ? _ppmdEncoder : _lzmaEncoder;
        return algorithm.Decompress(actualCompressed);
    }

    /// <summary>
    /// Estimate compression ratio using the best algorithm.
    /// </summary>
    public float EstimateRatio(byte[] sample)
    {
        if (sample.Length == 0) return 1.0f;

        // Compare LZMA2 and PPMd estimates
        float lzmaRatio = _lzmaEncoder.EstimateRatio(sample);
        float ppmdRatio = _ppmdEncoder.EstimateRatio(sample);

        // Use the better of the two
        return Math.Min(lzmaRatio, ppmdRatio);
    }

    /// <summary>
    /// Hybrid is always suitable as it falls back appropriately.
    /// </summary>
    public bool IsSuitable(byte[] data, string fileName)
    {
        return true; // Hybrid can handle anything
    }

    /// <summary>
    /// Determine if PPMd should be used for this data.
    /// PPMd is better for text, LZMA2 for general binary.
    /// </summary>
    private bool ShouldUsePPMd(byte[] data, CompressionSettings settings)
    {
        // Check custom parameters first
        if (settings.CustomParameters.ContainsKey("ForcePPMd"))
        {
            return Convert.ToBoolean(settings.CustomParameters["ForcePPMd"]);
        }

        if (settings.CustomParameters.ContainsKey("ForceLZMA"))
        {
            return false;
        }

        // Auto-detect based on content
        int sampleSize = Math.Min(data.Length, 4096); // Check first 4KB
        var sample = new byte[sampleSize];
        Array.Copy(data, sample, sampleSize);

        // PPMd threshold from settings (default 10MB)
        long ppmdThreshold = 10 * 1024 * 1024; // 10MB
        if (settings.CustomParameters.ContainsKey("PpmdThreshold"))
        {
            ppmdThreshold = Convert.ToInt64(settings.CustomParameters["PpmdThreshold"]);
        }

        // For small files, prefer PPMd if text-like
        if (data.Length <= ppmdThreshold)
        {
            return _ppmdEncoder.EstimateRatio(sample) < _lzmaEncoder.EstimateRatio(sample) * 1.2f;
        }

        // Check content type
        bool isTextLike = IsTextLike(sample);
        var extensions = GetPPMdExtensions(settings);

        // Use PPMd for text files and known text extensions
        string extension = settings.CustomParameters.ContainsKey("FileExtension") ?
            settings.CustomParameters["FileExtension"].ToString() ?? "" : "";

        bool extensionMatch = extensions.Any(ext => extension.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

        return isTextLike || extensionMatch;
    }

    private bool IsTextLike(byte[] sample)
    {
        if (sample.Length == 0) return false;

        int textChars = 0;
        int totalChars = Math.Min(sample.Length, 1024);

        for (int i = 0; i < totalChars; i++)
        {
            byte b = sample[i];
            if ((b >= 32 && b <= 126) || b is 9 or 10 or 13) // Printable + whitespace
            {
                textChars++;
            }
            else if (b == 0 && i > 0) // Null byte not at start (likely binary)
            {
                return false;
            }
        }

        return (float)textChars / totalChars > 0.8f; // 80%+ text content
    }

    private string[] GetPPMdExtensions(CompressionSettings settings)
    {
        // Default PPMd extensions
        var defaults = new[] { ".txt", ".log", ".json", ".xml", ".csv", ".md", ".html", ".css", ".js" };

        if (settings.CustomParameters.ContainsKey("PpmdExtensions"))
        {
            var custom = settings.CustomParameters["PpmdExtensions"] as string[];
            if (custom != null) return custom;
        }

        return defaults;
    }

    /// <summary>
    /// Get preset compression settings for hybrid mode.
    /// </summary>
    public static CompressionSettings GetPresetSettings(string preset)
    {
        var settings = new CompressionSettings();

        switch (preset.ToLowerInvariant())
        {
            case "fast":
                settings.Level = 3; // Quick compression
                settings.CustomParameters["LzmaLevel"] = 1;
                settings.CustomParameters["ModelOrder"] = 4;
                settings.CustomParameters["MemoryMB"] = 8;
                break;

            case "balanced":
                settings.Level = 10; // Default balanced
                settings.CustomParameters["LzmaLevel"] = 7;
                settings.CustomParameters["ModelOrder"] = 8;
                settings.CustomParameters["MemoryMB"] = 32;
                break;

            case "maximum":
                settings.Level = 22; // Maximum compression
                settings.CustomParameters["LzmaLevel"] = 9;
                settings.CustomParameters["ModelOrder"] = 12;
                settings.CustomParameters["MemoryMB"] = 128;
                break;

            default:
                // Balanced as default
                settings.Level = 10;
                settings.CustomParameters["LzmaLevel"] = 7;
                settings.CustomParameters["ModelOrder"] = 8;
                settings.CustomParameters["MemoryMB"] = 32;
                break;
        }

        // Set hybrid-specific defaults
        settings.CustomParameters["PpmdThreshold"] = 10 * 1024 * 1024L; // 10MB

        return settings;
    }
}
