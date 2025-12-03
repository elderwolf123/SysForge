using System;
using System.IO;
using ZstdSharp;

namespace RamOptimizer.Compression.HyperCompress.Encoders;

/// <summary>
/// Specialized encoder for game audio files (WAV, uncompressed).
/// For compressed formats (MP3, OGG), uses fast pass-through.
/// </summary>
public class HyperGameAudioEncoder : IHyperEncoder
{
    private static readonly byte[] WAV_RIFF = { 0x52, 0x49, 0x46, 0x46 }; // "RIFF"
    private static readonly byte[] OGG_MAGIC = { 0x4F, 0x67, 0x67, 0x53 }; // "OggS"
    
    public HyperAlgorithm Algorithm => HyperAlgorithm.HyperGame_Audio;
    
    public byte[] Compress(byte[] data, CompressionSettings settings)
    {
        if (StartsWithMagic(data, WAV_RIFF))
            return CompressWAV(data, settings);
        else if (StartsWithMagic(data, OGG_MAGIC))
            return FastPassthrough(data); // Already compressed
        else
            return CompressGenericAudio(data, settings);
    }
    
    public byte[] Decompress(byte[] compressed)
    {
        using var ms = new MemoryStream(compressed);
        using var reader = new BinaryReader(ms);
        
        byte format = reader.ReadByte();
        
        if (format == 0) // Fast passthrough
        {
            return reader.ReadBytes(compressed.Length - 1);
        }
        
        // Read compressed data
        int compressedLen = reader.ReadInt32();
        byte[] compressedData = reader.ReadBytes(compressedLen);
        
        using var decompressor = new Decompressor();
        byte[] decompressed = decompressor.Unwrap(compressedData).ToArray();
        
        if (format == 1) // WAV with delta encoding
        {
            return DeltaDecode16BitPCM(decompressed);
        }
        
        return decompressed;
    }
    
    public float EstimateRatio(byte[] sample)
    {
        if (StartsWithMagic(sample, WAV_RIFF))
            return 0.55f; // WAV compresses well with delta encoding
        else if (StartsWithMagic(sample, OGG_MAGIC))
            return 0.98f; // Already compressed, minimal gain
        
        return 0.7f;
    }
    
    public bool IsSuitable(byte[] data, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext == ".wav" || ext == ".ogg" || ext == ".mp3" || ext == ".m4a" || ext == ".aiff";
    }
    
    /// <summary>
    /// Compress WAV file with PCM delta encoding.
    /// </summary>
    private byte[] CompressWAV(byte[] data, CompressionSettings settings)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        
        writer.Write((byte)1); // WAV format with delta encoding
        
        // Parse WAV header to find data chunk
        int dataStart = FindWAVDataChunk(data);
        
        if (dataStart == -1 || dataStart >= data.Length)
        {
            // Can't find data chunk, use generic compression
            return CompressGenericAudio(data, settings);
        }
        
        // Keep header uncompressed for fast metadata access
        byte[] header = new byte[dataStart];
        Array.Copy(data, 0, header, 0, dataStart);
        writer.Write(header.Length);
        writer.Write(header);
        
        // Get PCM data
        byte[] pcmData = new byte[data.Length - dataStart];
        Array.Copy(data, dataStart, pcmData, 0, pcmData.Length);
        
        // Apply delta encoding (assumes 16-bit PCM - most common)
        byte[] deltaEncoded = DeltaEncode16BitPCM(pcmData);
        
        // Compress with Zstandard
        using var compressor = new Compressor(settings.Level);
        byte[] compressed = compressor.Wrap(deltaEncoded).ToArray();
        
        writer.Write(compressed.Length);
        writer.Write(compressed);
        
        return output.ToArray();
    }
    
    /// <summary>
    /// Fast passthrough for already-compressed audio (MP3, OGG).
    /// </summary>
    private byte[] FastPassthrough(byte[] data)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        
        writer.Write((byte)0); // Passthrough format
        writer.Write(data);
        
        return output.ToArray();
    }
    
    /// <summary>
    /// Generic audio compression.
    /// </summary>
    private byte[] CompressGenericAudio(byte[] data, CompressionSettings settings)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        
        writer.Write((byte)2); // Generic format
        
        using var compressor = new Compressor(settings.Level);
        byte[] compressed = compressor.Wrap(data).ToArray();
        
        writer.Write(compressed.Length);
        writer.Write(compressed);
        
        return output.ToArray();
    }
    
    /// <summary>
    /// Delta encode 16-bit PCM samples.
    /// Stores differences between adjacent samples instead of absolute values.
    /// </summary>
    private byte[] DeltaEncode16BitPCM(byte[] pcmData)
    {
        if (pcmData.Length < 4) return pcmData;
        
        byte[] result = new byte[pcmData.Length];
        
        // First sample unchanged
        result[0] = pcmData[0];
        result[1] = pcmData[1];
        
        // Subsequent samples: store delta
        for (int i = 2; i < pcmData.Length; i += 2)
        {
            if (i + 1 < pcmData.Length)
            {
                short current = BitConverter.ToInt16(pcmData, i);
                short previous = BitConverter.ToInt16(pcmData, i - 2);
                short delta = (short)(current - previous);
                
                byte[] deltaBytes = BitConverter.GetBytes(delta);
                result[i] = deltaBytes[0];
                result[i + 1] = deltaBytes[1];
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Decode delta-encoded 16-bit PCM.
    /// </summary>
    private byte[] DeltaDecode16BitPCM(byte[] deltaData)
    {
        if (deltaData.Length < 4) return deltaData;
        
        byte[] result = new byte[deltaData.Length];
        
        // First sample unchanged
        result[0] = deltaData[0];
        result[1] = deltaData[1];
        
        // Decode deltas
        for (int i = 2; i < deltaData.Length; i += 2)
        {
            if (i + 1 < deltaData.Length)
            {
                short delta = BitConverter.ToInt16(deltaData, i);
                short previous = BitConverter.ToInt16(result, i - 2);
                short current = (short)(previous + delta);
                
                byte[] currentBytes = BitConverter.GetBytes(current);
                result[i] = currentBytes[0];
                result[i + 1] = currentBytes[1];
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Find the start of the data chunk in a WAV file.
    /// </summary>
    private int FindWAVDataChunk(byte[] data)
    {
        // WAV format: RIFF header, then chunks
        // Look for "data" chunk ID
        byte[] dataChunkID = { 0x64, 0x61, 0x74, 0x61 }; // "data"
        
        for (int i = 12; i < data.Length - 8; i++)
        {
            bool match = true;
            for (int j = 0; j < 4; j++)
            {
                if (data[i + j] != dataChunkID[j])
                {
                    match = false;
                    break;
                }
            }
            
            if (match)
            {
                // Found "data" chunk, skip chunk ID (4 bytes) and size (4 bytes)
                return i + 8;
            }
        }
        
        return -1;
    }
    
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
