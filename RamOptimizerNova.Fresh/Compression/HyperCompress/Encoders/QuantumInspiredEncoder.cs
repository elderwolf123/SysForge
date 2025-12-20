using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using K4os.Compression.LZ4;

namespace RamOptimizer.Compression.HyperCompress.Encoders;

/// <summary>
/// Quantum-Inspired Pattern Recognition Algorithm (QIPRA) encoder.
/// Uses multi-scale pattern analysis, adaptive dictionary building,
/// and pattern superposition for enhanced compression.
/// </summary>
public class QuantumInspiredEncoder : IHyperEncoder
{
    public HyperAlgorithm Algorithm => HyperAlgorithm.HyperQuantum_QIPRA;

    private const int MAX_PATTERN_SIZE = 256;
    private const int MIN_PATTERN_SIZE = 3;
    private const int MAX_DICTIONARY_ENTRIES = 4096;
    private const int PATTERN_FREQUENCY_THRESHOLD = 2;

    public byte[] Compress(byte[] data, CompressionSettings settings)
   {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        // Write header
        writer.Write((byte)1); // Version
        writer.Write(data.Length); // Original size

        // Step 1: Multi-scale pattern analysis
        var patterns = AnalyzeMultiScalePatterns(data);

        // Step 2: Build adaptive dictionary (most valuable patterns)
        var dictionary = BuildAdaptiveDictionary(patterns);

        // Step 3: Encode dictionary
        WriteDictionary(writer, dictionary);

        // Step 4: Encode data using pattern references
        var encodedData = EncodeWithPatterns(data, dictionary);

        // Step 5: Compress residuals with LZ4 for additional compression
        var compressed = LZ4Pickler.Pickle(encodedData);
        
        writer.Write(compressed.Length);
        writer.Write(compressed);

        return output.ToArray();
    }

    public byte[] Decompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var reader = new BinaryReader(input);

        // Read header
        byte version = reader.ReadByte();
        if (version != 1)
            throw new InvalidDataException($"Unsupported QIPRA version: {version}");

        int originalSize = reader.ReadInt32();

        // Read dictionary
        var dictionary = ReadDictionary(reader);

        // Read compressed data
        int compressedLength = reader.ReadInt32();
        byte[] compressedData = reader.ReadBytes(compressedLength);

        // Decompress with LZ4
        var encodedData = LZ4Pickler.Unpickle(compressedData);

        // Decode using dictionary
        var decompressed = DecodeWithPatterns(encodedData, dictionary, originalSize);

        return decompressed;
    }

    public float EstimateRatio(byte[] sample)
    {
        // Quick pattern analysis for estimation
        var patterns = AnalyzeMultiScalePatterns(sample.Take(Math.Min(16384, sample.Length)).ToArray());
        
        // Calculate compression potential based on pattern redundancy
        long totalBytes = sample.Length;
        long savingsEstimate = patterns.Sum(p => (p.Value.Data.Length - 2) * (p.Value.Frequency - 1));
        
        float estimatedRatio = 1.0f - (Math.Min(savingsEstimate, totalBytes) / (float)totalBytes);
        
        // QIPRA works best with repetitive data
        return Math.Max(0.1f, Math.Min(0.9f, estimatedRatio));
    }

    public bool IsSuitable(byte[] data, string fileName)
    {
        // QIPRA is suitable for any data but excels at repetitive patterns
        // Let it compete with other encoders via EstimateRatio
        return true;
    }

    #region Pattern Analysis

    private class Pattern
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public int Frequency { get; set; }
        public List<int> Positions { get; set; } = new();
        public int CompressionValue => (Data.Length - 2) * (Frequency - 1); // Savings
    }

    private Dictionary<string, Pattern> AnalyzeMultiScalePatterns(byte[] data)
    {
        var allPatterns = new Dictionary<string, Pattern>();

        // Analyze at multiple scales (quantum superposition)
        for (int scale = MIN_PATTERN_SIZE; scale <= Math.Min(MAX_PATTERN_SIZE, data.Length / 4); scale *= 2)
        {
            FindPatternsAtScale(data, scale, allPatterns);
        }

        // Filter by frequency threshold
        return allPatterns
            .Where(p => p.Value.Frequency >= PATTERN_FREQUENCY_THRESHOLD)
            .ToDictionary(p => p.Key, p => p.Value);
    }

    private void FindPatternsAtScale(byte[] data, int scale, Dictionary<string, Pattern> patterns)
    {
        for (int i = 0; i <= data.Length - scale; i++)
        {
            var patternBytes = data.Skip(i).Take(scale).ToArray();
            var key = Convert.ToBase64String(patternBytes);

            if (patterns.ContainsKey(key))
            {
                patterns[key].Frequency++;
                patterns[key].Positions.Add(i);
            }
            else
            {
                patterns[key] = new Pattern
                {
                    Data = patternBytes,
                    Frequency = 1,
                    Positions = new List<int> { i }
                };
            }
        }
    }

    private List<Pattern> BuildAdaptiveDictionary(Dictionary<string, Pattern> patterns)
    {
        // Select most valuable patterns (entanglement-based selection)
        return patterns.Values
            .OrderByDescending(p => p.CompressionValue)
            .Take(MAX_DICTIONARY_ENTRIES)
            .ToList();
    }

    #endregion

    #region Encoding/Decoding

    private void WriteDictionary(BinaryWriter writer, List<Pattern> dictionary)
    {
        writer.Write((ushort)dictionary.Count);
        
        foreach (var pattern in dictionary)
        {
            writer.Write((byte)pattern.Data.Length);
            writer.Write(pattern.Data);
        }
    }

    private List<Pattern> ReadDictionary(BinaryReader reader)
    {
        int count = reader.ReadUInt16();
        var dictionary = new List<Pattern>(count);

        for (int i = 0; i < count; i++)
        {
            int length = reader.ReadByte();
            byte[] data = reader.ReadBytes(length);
            
            dictionary.Add(new Pattern { Data = data });
        }

        return dictionary;
    }

    private byte[] EncodeWithPatterns(byte[] data, List<Pattern> dictionary)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        int pos = 0;
        while (pos < data.Length)
        {
            // Try to find longest matching pattern
            int bestPatternIdx = -1;
            int bestLength = 0;

            for (int i = 0; i < dictionary.Count; i++)
            {
                var pattern = dictionary[i].Data;
                if (pos + pattern.Length <= data.Length)
                {
                    bool matches = true;
                    for (int j = 0; j < pattern.Length; j++)
                    {
                        if (data[pos + j] != pattern[j])
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches && pattern.Length > bestLength)
                    {
                        bestPatternIdx = i;
                        bestLength = pattern.Length;
                    }
                }
            }

            if (bestPatternIdx >= 0)
            {
                // Write pattern reference: 0xFF (marker) + pattern index (2 bytes)
                writer.Write((byte)0xFF);
                writer.Write((ushort)bestPatternIdx);
                pos += bestLength;
            }
            else
            {
                // Write literal byte
                byte b = data[pos];
                if (b == 0xFF)
                {
                    // Escape 0xFF literals
                    writer.Write((byte)0xFF);
                    writer.Write((byte)0xFF);
                }
                else
                {
                    writer.Write(b);
                }
                pos++;
            }
        }

        return output.ToArray();
    }

    private byte[] DecodeWithPatterns(byte[] encoded, List<Pattern> dictionary, int originalSize)
    {
        using var input = new MemoryStream(encoded);
        using var reader = new BinaryReader(input);
        using var output = new MemoryStream(originalSize);

        while (input.Position < input.Length)
        {
            byte b = reader.ReadByte();

            if (b == 0xFF)
            {
                // Check if pattern reference or escaped 0xFF
                if (input.Position < input.Length)
                {
                    byte next = reader.ReadByte();
                    if (next == 0xFF)
                    {
                        // Escaped 0xFF literal
                        output.WriteByte(0xFF);
                    }
                    else
                    {
                        // Pattern reference (next is first byte of ushort index)
                        byte second = reader.ReadByte();
                        ushort patternIdx = (ushort)((second << 8) | next);
                        
                        if (patternIdx < dictionary.Count)
                        {
                            output.Write(dictionary[patternIdx].Data);
                        }
                        else
                        {
                            throw new InvalidDataException($"Invalid pattern index: {patternIdx}");
                        }
                    }
                }
                else
                {
                    output.WriteByte(b);
                }
            }
            else
            {
                // Literal byte
                output.WriteByte(b);
            }
        }

        return output.ToArray();
    }

    #endregion
}
