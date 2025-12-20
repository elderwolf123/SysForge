using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using RamOptimizer.Compression.HyperCompress.Encoders;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Reads .hca (HyperCompressed Archive) files and extracts data.
/// </summary>
public class ChunkedArchiveReader : IDisposable
{
    private readonly string _archivePath;
    private readonly HyperCompressEngine _engine;
    private readonly ILogger? _logger;
    private FileStream? _archiveStream;
    private BinaryReader? _reader;
    private ArchiveFormat.Header? _header;
    private List<ArchiveFormat.ChunkEntry>? _chunks;
    private List<ArchiveFormat.FileEntry>? _files;
    private Dictionary<int, byte[]>? _chunkCache; // LRU cache for decompressed chunks
    private readonly int _maxCachedChunks = 10;
    
    public ChunkedArchiveReader(
        string archivePath,
        HyperCompressEngine engine,
        ILogger? logger = null)
    {
        _archivePath = archivePath;
        _engine = engine;
        _logger = logger;
        _chunkCache = new Dictionary<int, byte[]>();
    }
    
    /// <summary>
    /// Open and read archive metadata.
    /// </summary>
    public void Open()
    {
        if (_archiveStream != null)
            throw new InvalidOperationException("Archive is already open");
        
        _archiveStream = new FileStream(_archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _reader = new BinaryReader(_archiveStream);
        
        // Read header
        _header = ArchiveFormat.Header.ReadFrom(_reader);
        
        if (!_header.IsValid())
        {
            throw new InvalidDataException("Invalid .hca archive format");
        }
        
        _logger?.LogInformation($"Opened archive: {Path.GetFileName(_archivePath)} " +
            $"(v{_header.Version:X4}, {_header.TotalChunks} chunks)");
        
        // Read chunk table (stored at LearningDBOffset position)
        _archiveStream.Seek(_header.LearningDBOffset, SeekOrigin.Begin);
        int chunkCount = _reader.ReadInt32();
        
        _chunks = new List<ArchiveFormat.ChunkEntry>();
        for (int i = 0; i < chunkCount; i++)
        {
            _chunks.Add(ArchiveFormat.ChunkEntry.ReadFrom(_reader));
        }
        
        // Read file index
        _archiveStream.Seek(_header.IndexOffset, SeekOrigin.Begin);
        int fileCount = _reader.ReadInt32();
        
        _files = new List<ArchiveFormat.FileEntry>();
        for (int i = 0; i < fileCount; i++)
        {
            _files.Add(ArchiveFormat.FileEntry.ReadFrom(_reader));
        }
        
        _logger?.LogDebug($"Archive contains {_files.Count} files in {_chunks.Count} chunks");
    }
    
    /// <summary>
    /// Get list of all files in archive.
    /// </summary>
    public IReadOnlyList<string> GetFileList()
    {
        EnsureOpen();
        return _files!.Select(f => f.Name).ToList();
    }
    
    /// <summary>
    /// Extract a single file from the archive.
    /// </summary>
    public byte[] ExtractFile(string fileName)
    {
        EnsureOpen();
        
        var fileEntry = _files!.FirstOrDefault(f => 
            f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        
        if (fileEntry == null)
            throw new FileNotFoundException($"File not found in archive: {fileName}");
        
        // For files spanning multiple chunks, we'd need to concatenate
        // For now, assume files fit in single chunk (most common case)
        if (fileEntry.ChunkIDs.Length != 1)
            throw new NotImplementedException("Multi-chunk file extraction not yet implemented");
        
        int chunkId = fileEntry.ChunkIDs[0];
        byte[] chunkData = GetDecompressedChunk(chunkId);
        
        // Extract file data from chunk
        int offset = fileEntry.OffsetInFirstChunk;
        int size = (int)fileEntry.OriginalSize;
        
        byte[] fileData = new byte[size];
        Array.Copy(chunkData, offset, fileData, 0, size);
        
        return fileData;
    }
    
    /// <summary>
    /// Extract all files to a directory.
    /// </summary>
    public void ExtractAll(string targetDirectory, IProgress<int>? progress = null)
    {
        EnsureOpen();
        
        Directory.CreateDirectory(targetDirectory);
        
        int processed = 0;
        foreach (var file in _files!)
        {
            var targetPath = Path.Combine(targetDirectory, file.Name);
            var targetDir = Path.GetDirectoryName(targetPath);
            
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);
            
            var data = ExtractFile(file.Name);
            File.WriteAllBytes(targetPath, data);
            
            File.SetLastWriteTime(targetPath, file.LastModified);
            
            processed++;
            progress?.Report(processed);
        }
        
        _logger?.LogInformation($"Extracted {processed} files to {targetDirectory}");
    }
    
    /// <summary>
    /// Get decompressed chunk data (with caching).
    /// </summary>
    private byte[] GetDecompressedChunk(int chunkId)
    {
        // Check cache first
        if (_chunkCache!.TryGetValue(chunkId, out var cached))
        {
            _logger?.LogDebug($"Cache hit for chunk {chunkId}");
            return cached;
        }
        
        // Read and decompress chunk
        var chunkEntry = _chunks![chunkId];
        
        _archiveStream!.Seek(chunkEntry.FileOffset, SeekOrigin.Begin);
        byte[] compressed = _reader!.ReadBytes(chunkEntry.CompressedSize);
        
        // ALWAYS use HyperGeneralEncoder for chunks
        // This matches what ChunkedArchiver does during compression
        var encoder = new HyperGeneralEncoder();
        byte[] decompressed = encoder.Decompress(compressed);
        
        // Verify checksum
        uint actualChecksum = ArchiveFormat.ComputeCRC32(decompressed);
        if (actualChecksum != chunkEntry.Checksum)
        {
            throw new InvalidDataException($"Chunk {chunkId} checksum mismatch");
        }
        
        // Add to cache (evict oldest if full)
        if (_chunkCache.Count >= _maxCachedChunks)
        {
            var oldest = _chunkCache.Keys.First();
            _chunkCache.Remove(oldest);
        }
        
        _chunkCache[chunkId] = decompressed;
        
        _logger?.LogDebug($"Decompressed chunk {chunkId}: {compressed.Length} → {decompressed.Length} bytes");
        
        return decompressed;
    }
    
    private void EnsureOpen()
    {
        if (_archiveStream == null || _reader == null || _header == null)
            throw new InvalidOperationException("Archive is not open. Call Open() first.");
    }
    
    public void Dispose()
    {
        _reader?.Dispose();
        _archiveStream?.Dispose();
        _chunkCache?.Clear();
    }
}
