namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Base interface for all HyperCompress encoder implementations.
/// Each encoder specializes in a specific file type or compression strategy.
/// </summary>
public interface IHyperEncoder
{
    /// <summary>
    /// Gets the algorithm identifier for this encoder.
    /// </summary>
    HyperAlgorithm Algorithm { get; }
    
    /// <summary>
    /// Compress data using this encoder's strategy.
    /// </summary>
    /// <param name="data">Uncompressed data</param>
    /// <param name="settings">Compression settings (level, parameters)</param>
    /// <returns>Compressed data</returns>
    byte[] Compress(byte[] data, CompressionSettings settings);
    
    /// <summary>
    /// Decompress data that was compressed with this encoder.
    /// </summary>
    /// <param name="compressed">Compressed data</param>
    /// <returns>Uncompressed data</returns>
    byte[] Decompress(byte[] compressed);
    
    /// <summary>
    /// Estimate compression ratio for a sample without full compression.
    /// Used for algorithm selection.
    /// </summary>
    /// <param name="sample">Sample of data (first 64KB typically)</param>
    /// <returns>Estimated ratio (0.0 = no compression, 1.0 = incompressible)</returns>
    float EstimateRatio(byte[] sample);
    
    /// <summary>
    /// Check if this encoder is suitable for the given data.
    /// </summary>
    /// <param name="data">Data to check</param>
    /// <param name="fileName">Original filename (for extension check)</param>
    /// <returns>True if this encoder should be used</returns>
    bool IsSuitable(byte[] data, string fileName);
}

/// <summary>
/// Compression settings for encoders.
/// </summary>
public class CompressionSettings
{
    /// <summary>
    /// Compression level (1-22, similar to Zstandard scale).
    /// </summary>
    public int Level { get; set; } = 10;
    
    /// <summary>
    /// Maximum memory to use for compression (in MB).
    /// </summary>
    public int MaxMemoryMB { get; set; } = 512;
    
    /// <summary>
    /// Enable multi-threading for compression.
    /// </summary>
    public bool EnableMultiThreading { get; set; } = true;
    
    /// <summary>
    /// Dictionary for dictionary-based compression.
    /// Null = auto-generate or use default.
    /// </summary>
    public byte[]? Dictionary { get; set; }
    
    /// <summary>
    /// Algorithm-specific parameters.
    /// </summary>
    public Dictionary<string, object> CustomParameters { get; set; } = new();
}

/// <summary>
/// Algorithm identifiers for HyperCompress encoders.
/// </summary>
public enum HyperAlgorithm : byte
{
    // Game-specific encoders
    HyperGame_Texture = 0x10,
    HyperGame_Audio = 0x11,
    HyperGame_Mesh = 0x12,
    HyperGame_Executable = 0x13,
    
    // General-purpose encoders
    HyperGeneral_Text = 0x20,
    HyperGeneral_Binary = 0x21,
    HyperGeneral_Database = 0x22,
    
    // Advanced algorithms
    HyperQuantum_QIPRA = 0x30,
    HyperFractal_FBCA = 0x31,
    
    // Learned/Adaptive
    HyperAdaptive_Learned = 0x40,
    
    // Fallback options
    Fallback_LZ4 = 0xFE,
    Fallback_Store = 0xFF  // No compression
}
