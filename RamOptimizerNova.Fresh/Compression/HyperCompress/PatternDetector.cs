using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Detects file patterns and characteristics to select optimal compression strategy.
/// </summary>
public class PatternDetector
{
    private static readonly HashSet<string> CompressedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", // Images
        ".mp3", ".ogg", ".m4a", ".aac", ".flac", // Audio
        ".mp4", ".mkv", ".avi", ".webm", // Video
        ".zip", ".rar", ".7z", ".gz", ".bz2", // Archives
        ".zst", ".lz4" // Already compressed
    };
    
    private static readonly HashSet<string> TextureExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dds", ".tga", ".bmp", ".hdr"
    };
    
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".aiff"
    };
    
    private static readonly HashSet<string> MeshExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".fbx", ".obj", ".dae", ".gltf", ".glb"
    };
    
    private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".so", ".dylib"
    };
    
    /// <summary>
    /// Detect file pattern and characteristics.
    /// </summary>
    public FilePattern DetectPattern(byte[] data, string fileName)
    {
        var pattern = new FilePattern
        {
            FileName = fileName
        };
        
        var extension = Path.GetExtension(fileName);
        
        // Quick checks based on extension
        if (CompressedExtensions.Contains(extension))
        {
            pattern.Type = FilePatternType.AlreadyCompressed;
            pattern.Entropy = 7.9f; // Assume high entropy
            return pattern;
        }
        
        if (TextureExtensions.Contains(extension))
            pattern.Type = FilePatternType.GameTexture;
        else if (AudioExtensions.Contains(extension))
            pattern.Type = FilePatternType.GameAudio;
        else if (MeshExtensions.Contains(extension))
            pattern.Type = FilePatternType.GameMesh;
        else if (ExecutableExtensions.Contains(extension))
            pattern.Type = FilePatternType.GameExecutable;
        else
            pattern.Type = FilePatternType.Unknown;
        
        // Analyze data characteristics
        AnalyzeData(data, pattern);
        
        // Refine type if unknown
        if (pattern.Type == FilePatternType.Unknown)
        {
            pattern.Type = ClassifyUnknown(data, pattern);
        }
        
        return pattern;
    }
    
    /// <summary>
    /// Analyze data characteristics (entropy, repetition, self-similarity).
    /// </summary>
    private void AnalyzeData(byte[] data, FilePattern pattern)
    {
        // Use sample for large files
        int sampleSize = Math.Min(64 * 1024, data.Length);
        var sample = data.Take(sampleSize).ToArray();
        
        pattern.Entropy = CalculateEntropy(sample);
        pattern.Repetition = DetectRepetition(sample);
        pattern.SelfSimilarity = DetectSelfSimilarity(sample);
    }
    
    /// <summary>
    /// Calculate Shannon entropy (measure of randomness).
    /// 0 = all same byte, 8 = completely random.
    /// </summary>
    private float CalculateEntropy(byte[] data)
    {
        if (data.Length == 0) return 0;
        
        // Count byte frequencies
        var freq = new int[256];
        foreach (byte b in data)
            freq[b]++;
        
        // Calculate entropy
        double entropy = 0;
        foreach (var count in freq)
        {
            if (count == 0) continue;
            
            double probability = (double)count / data.Length;
            entropy -= probability * Math.Log(probability, 2);
        }
        
        return (float)entropy;
    }
    
    /// <summary>
    /// Detect repetitive blocks (good for dictionary compression).
    /// Returns ratio of repeated blocks (0.0 = no repetition, 1.0 = all repeated).
    /// </summary>
    private float DetectRepetition(byte[] data)
    {
        const int blockSize = 64;
        if (data.Length < blockSize * 2) return 0;
        
        var blockHashes = new Dictionary<ulong, int>();
        int totalBlocks = 0;
        int repeatedBlocks = 0;
        
        // Hash blocks and count occurrences
        for (int i = 0; i <= data.Length - blockSize; i += blockSize)
        {
            ulong hash = ComputeBlockHash(data, i, blockSize);
            totalBlocks++;
            
            if (blockHashes.ContainsKey(hash))
            {
                repeatedBlocks++;
                blockHashes[hash]++;
            }
            else
            {
                blockHashes[hash] = 1;
            }
        }
        
        return totalBlocks > 0 ? (float)repeatedBlocks / totalBlocks : 0;
    }
    
    /// <summary>
    /// Detect self-similarity at different scales (fractal property).
    /// </summary>
    private float DetectSelfSimilarity(byte[] data)
    {
        // Simplified fractal detection
        // Check if patterns at scale N appear at scale N/2
        
        const int baseScale = 256;
        if (data.Length < baseScale * 4) return 0;
        
        var basePatterns = new HashSet<ulong>();
        var scaledMatches = 0;
        
        // Extract patterns at base scale
        for (int i = 0; i <= data.Length - baseScale; i += baseScale)
        {
            ulong hash = ComputeBlockHash(data, i, baseScale);
            basePatterns.Add(hash);
        }
        
        // Check if similar patterns exist at half scale
        int halfScale = baseScale / 2;
        for (int i = 0; i <= data.Length - halfScale; i += halfScale)
        {
            ulong hash = ComputeBlockHash(data, i, halfScale);
            if (basePatterns.Contains(hash))
                scaledMatches++;
        }
        
        int totalHalfBlocks = (data.Length - halfScale) / halfScale + 1;
        return totalHalfBlocks > 0 ? (float)scaledMatches / totalHalfBlocks : 0;
    }
    
    /// <summary>
    /// Classify unknown file types based on data analysis.
    /// </summary>
    private FilePatternType ClassifyUnknown(byte[] data, FilePattern pattern)
    {
        // High entropy = already compressed or encrypted
        if (pattern.Entropy > 7.5f)
            return FilePatternType.AlreadyCompressed;
        
        // Low entropy + high repetition = text or structured data
        if (pattern.Entropy < 5.0f && pattern.Repetition > 0.3f)
            return FilePatternType.GeneralText;
        
        // Everything else
        return FilePatternType.GeneralBinary;
    }
    
    /// <summary>
    /// Compute a simple hash for a block of data.
    /// </summary>
    private ulong ComputeBlockHash(byte[] data, int offset, int length)
    {
        // FNV-1a hash
        const ulong FnvPrime = 1099511628211;
        const ulong FnvOffsetBasis = 14695981039346656037;
        
        ulong hash = FnvOffsetBasis;
        int end = Math.Min(offset + length, data.Length);
        
        for (int i = offset; i < end; i++)
        {
            hash ^= data[i];
            hash *= FnvPrime;
        }
        
        return hash;
    }
}
