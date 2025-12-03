using System;
using System.IO;
using System.Threading.Tasks;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Adapter to integrate HyperCompress with the existing ICompressionAlgorithm interface.
/// Allows HyperCompress to be used alongside Zstandard, LZ4, etc.
/// </summary>
public class HyperCompressAlgorithm : ICompressionAlgorithm
{
    public string Name => "HyperCompress";
    
    private readonly HyperCompressEngine _engine;
    private readonly HyperCompressLearningDatabase _learningDb;
    
    public  HyperCompressAlgorithm()
    {
        _engine = new HyperCompressEngine();
        _learningDb = new HyperCompressLearningDatabase();
        
        // Register all encoders
        _engine.RegisterEncoder(new Encoders.FallbackLZ4Encoder());
        _engine.RegisterEncoder(new Encoders.HyperGameTextureEncoder());
        _engine.RegisterEncoder(new Encoders.HyperGameAudioEncoder());
        _engine.RegisterEncoder(new Encoders.HyperGameExecutableEncoder());
        _engine.RegisterEncoder(new Encoders.HyperGeneralEncoder());
    }
    
    public async Task<CompressionResult> CompressAsync(Stream input, Stream output, int level = 10)
    {
        var result = new CompressionResult();
        
        try
        {
            // Read input
            byte[] data = new byte[input.Length];
            await input.ReadAsync(data, 0, data.Length);
            result.OriginalSize = data.Length;
            
            // Compress with HyperCompress
            var settings = new CompressionSettings { Level = level };
            byte[] compressed = _engine.Compress(data, "data", settings);
            
            // Write to output
            await output.WriteAsync(compressed, 0, compressed.Length);
            result.CompressedSize = compressed.Length;
            result.Success = true;
            
            // Learn from this compression
            float ratio = (float)compressed.Length / data.Length;
            _learningDb.LearnFromCompression("data", data, HyperAlgorithm.HyperGeneral_Binary, ratio);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    public async Task DecompressAsync(Stream input, Stream output)
    {
        try
        {
            // Read compressed data
            byte[] compressed = new byte[input.Length];
            await input.ReadAsync(compressed, 0, compressed.Length);
            
            // Decompress (default to general binary)
            byte[] decompressed = _engine.Decompress(compressed, HyperAlgorithm.HyperGeneral_Binary);
            
            // Write to output
            await output.WriteAsync(decompressed, 0, decompressed.Length);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Decompression failed: {ex.Message}", ex);
        }
    }
}
