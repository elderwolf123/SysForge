using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Main engine for HyperCompress algorithm.
/// Analyzes files and selects optimal encoder (HyperGame, HyperGeneral, QIPRA, FBCA).
/// </summary>
public class HyperCompressEngine
{
    private readonly ILogger? _logger;
    private readonly Dictionary<HyperAlgorithm, IHyperEncoder> _encoders;
    private readonly PatternDetector  _patternDetector;
    
    public HyperCompressEngine(ILogger? logger = null)
    {
        _logger = logger;
        _encoders = new Dictionary<HyperAlgorithm, IHyperEncoder>();
        _patternDetector = new PatternDetector();
        
        // Register encoders as they're implemented
        // TODO: RegisterEncoder(new HyperGameTextureEncoder());
        // TODO: RegisterEncoder(new HyperQuantumEncoder());
        // etc.
    }
    
    /// <summary>
    /// Register an encoder for use.
    /// </summary>
    public void RegisterEncoder(IHyperEncoder encoder)
    {
        _encoders[encoder.Algorithm] = encoder;
        _logger?.LogInformation($"Registered encoder: {encoder.Algorithm}");
    }
    
    /// <summary>
    /// Compress data using the optimal encoder.
    /// </summary>
    public byte[] Compress(byte[] data, string fileName, CompressionSettings? settings = null)
    {
        settings ??= new CompressionSettings();
        
        // 1. Detect file pattern
        var pattern = _patternDetector.DetectPattern(data, fileName);
        _logger?.LogDebug($"Pattern detected: {pattern.Type} for {fileName}");
        
        // 2. Select best encoder
        var encoder = SelectEncoder(pattern, data);
        if (encoder == null)
        {
            _logger?.LogWarning($"No suitable encoder found for {fileName}, using fallback");
            return CompressFallback(data);
        }
        
        _logger?.LogInformation($"Using {encoder.Algorithm} for {fileName}");
        
        // 3. Compress
        try
        {
            return encoder.Compress(data, settings);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Compression failed with {encoder.Algorithm}, trying fallback");
            return CompressFallback(data);
        }
    }
    
    /// <summary>
    /// Decompress data using the specified algorithm.
    /// </summary>
    public byte[] Decompress(byte[] compressed, HyperAlgorithm algorithm)
    {
        if (!_encoders.TryGetValue(algorithm, out var encoder))
        {
            throw new InvalidOperationException($"No encoder registered for {algorithm}");
        }
        
        return encoder.Decompress(compressed);
    }
    
    /// <summary>
    /// Select the best encoder for the given pattern and data.
    /// </summary>
    private IHyperEncoder? SelectEncoder(FilePattern pattern, byte[] data)
    {
        // Get all suitable encoders
        var suitable = _encoders.Values
            .Where(e => e.IsSuitable(data, pattern.FileName))
            .ToList();
        
        if (suitable.Count == 0)
            return null;
        
        if (suitable.Count == 1)
            return suitable[0];
        
        // Multiple suitable - test with sample and pick best ratio
        var sample = data.Take(Math.Min(64 * 1024, data.Length)).ToArray();
        
        var best = suitable
            .Select(e => (Encoder: e, Ratio: e.EstimateRatio(sample)))
            .OrderBy(x => x.Ratio) // Lower ratio = better compression
            .First();
        
        _logger?.LogDebug($"Selected {best.Encoder.Algorithm} (estimated ratio: {best.Ratio:P2})");
        
        return best.Encoder;
    }
    
    /// <summary>
    /// Fallback compression using LZ4 or store uncompressed.
    /// </summary>
    private byte[] CompressFallback(byte[] data)
    {
        // Check if LZ4 encoder is registered
        if (_encoders.TryGetValue(HyperAlgorithm.Fallback_LZ4, out var lz4))
        {
            return lz4.Compress(data, new CompressionSettings());
        }
        
        // Last resort - store uncompressed
        _logger?.LogWarning("Storing data uncompressed (no fallback encoder available)");
        return data;
    }
}

/// <summary>
/// File pattern information detected by PatternDetector.
/// </summary>
public class FilePattern
{
    public string FileName { get; set; } = string.Empty;
    public FilePatternType Type { get; set; }
    public float Entropy { get; set; }
    public float Repetition { get; set; }
    public float SelfSimilarity { get; set; }
    public Dictionary<string, object> Features { get; set; } = new();
}

/// <summary>
/// Pattern type categories.
/// </summary>
public enum FilePatternType
{
    GameTexture,
    GameAudio,
    GameMesh,
    GameExecutable,
    GeneralText,
    GeneralBinary,
    AlreadyCompressed,
    Unknown
}
