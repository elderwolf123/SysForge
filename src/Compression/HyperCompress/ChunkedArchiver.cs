using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Creates .hca (HyperCompressed Archive) files from directories.
/// Splits data into chunks, compresses with optimal encoders, and builds index.
/// </summary>
public class ChunkedArchiver
{
    private readonly HyperCompressEngine _engine;
    private readonly HyperCompressLearningDatabase _learningDb;
    private readonly ILogger? _logger;
    private readonly int _chunkSize;
    
    public ChunkedArchiver(
        HyperCompressEngine engine,
        HyperCompressLearningDatabase? learningDb = null,
        ILogger? logger = null,
        int chunkSize = ArchiveFormat.DefaultChunkSize)
    {
        _engine = engine;
        _learningDb = learningDb ?? new HyperCompressLearningDatabase();
        _logger = logger;
        _chunkSize = chunkSize;
    }
    
    /// <summary>
    /// Compress a directory into a .hca archive.
    /// </summary>
    public async Task<ArchiveCreationResult> CreateArchiveAsync(
        string sourceDirectory,
        string archivePath,
        CompressionSettings? settings = null,
        IProgress<ArchiveProgress>? progress = null)
    {
        settings ??= new CompressionSettings();
        var result = new ArchiveCreationResult { ArchivePath = archivePath };
        
        try
        {
            _logger?.LogInformation($"Creating archive: {archivePath} from {sourceDirectory}");
            
            // 1. Collect all files
            var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .ToList();
            
            result.TotalFiles = files.Count;
            result.OriginalSize = files.Sum(f => f.Length);
            
            _logger?.LogInformation($"Found {files.Count} files, total size: {result.OriginalSize:N0} bytes");
            
            // 2. Create archive file
            using var archiveStream = new FileStream(archivePath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(archiveStream);
            
            // 3. Write placeholder header (will update later)
            var header = new ArchiveFormat.Header
            {
                Magic = ArchiveFormat.MagicBytes,
                Version = ArchiveFormat.CurrentVersion,
                ChunkSize = _chunkSize,
                TotalChunks = 0, // Will update
                IndexOffset = 0 // Will update
            };
            
            long headerPos = archiveStream.Position;
            header.WriteTo(writer);
            
            // 4. Process files into chunks
            var chunks = new List<ArchiveFormat.ChunkEntry>();
            var fileEntries = new List<ArchiveFormat.FileEntry>();
            var currentChunk = new List<byte>();
            var currentChunkFiles = new List<(int fileId, int offset, string path)>();
            int fileId = 0;
            int processedFiles = 0;
            
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(sourceDirectory, file.FullName);
                var fileData = await File.ReadAllBytesAsync(file.FullName);
                
                // Add to current chunk
                int offsetInChunk = currentChunk.Count;
                currentChunkFiles.Add((fileId, offsetInChunk, relativePath));
                currentChunk.AddRange(fileData);
                
                // If chunk is full, compress and write it
                if (currentChunk.Count >= _chunkSize)
                {
                    await WriteChunkAsync(writer, currentChunk, currentChunkFiles, chunks, fileEntries, settings);
                    currentChunk.Clear();
                    currentChunkFiles.Clear();
                }
                
                fileId++;
                processedFiles++;
                
                progress?.Report(new ArchiveProgress
                {
                    FilesProcessed = processedFiles,
                    TotalFiles = result.TotalFiles,
                    BytesProcessed = files.Take(processedFiles).Sum(f => f.Length)
                });
            }
            
            // Write final chunk if any data remains
            if (currentChunk.Count > 0)
            {
                await WriteChunkAsync(writer, currentChunk, currentChunkFiles, chunks, fileEntries, settings);
            }
            
            // 5. Write file index
            long indexOffset = archiveStream.Position;
            writer.Write(fileEntries.Count);
            foreach (var entry in fileEntries)
            {
                entry.WriteTo(writer);
            }
            
            // 6. Update header with final info
            header.TotalChunks = chunks.Count;
            header.IndexOffset = indexOffset;
            
            archiveStream.Seek(headerPos, SeekOrigin.Begin);
            header.WriteTo(writer);
            
            result.CompressedSize = archiveStream.Length;
            result.CompressionRatio = (float)result.CompressedSize / result.OriginalSize;
            result.Success = true;
            
            _logger?.LogInformation(
                $"Archive created: {chunks.Count} chunks, " +
                $"{result.OriginalSize:N0} → {result.CompressedSize:N0} bytes " +
                $"({result.CompressionRatio:P2})");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create archive");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }
    
    /// <summary>
    /// Write a chunk to the archive.
    /// </summary>
    private async Task WriteChunkAsync(
        BinaryWriter writer,
        List<byte> chunkData,
        List<(int fileId, int offset, string path)> filesInChunk,
        List<ArchiveFormat.ChunkEntry> chunks,
        List<ArchiveFormat.FileEntry> fileEntries,
        CompressionSettings settings)
    {
        int chunkId = chunks.Count;
        byte[] uncompressed = chunkData.ToArray();
        
        // Compress chunk using HyperCompress engine
        // For chunk compression, we'll use the general encoder since chunks contain mixed data
        var compressed = _engine.Compress(uncompressed, "chunk.dat", settings);
        
        // Create chunk entry
        var chunkEntry = new ArchiveFormat.ChunkEntry
        {
            ChunkID = chunkId,
            FileOffset = writer.BaseStream.Position,
            CompressedSize = compressed.Length,
            UncompressedSize = uncompressed.Length,
            Algorithm = HyperAlgorithm.HyperGeneral_Binary, // Chunks use general encoder
            Checksum = ArchiveFormat.ComputeCRC32(uncompressed)
        };
        
        // Write compressed data
        writer.Write(compressed);
        
        chunks.Add(chunkEntry);
        
        // Create file entries for files in this chunk
        foreach (var (fileId, offset, path) in filesInChunk)
        {
            // Find file size
            int fileSize = 0;
            int nextOffset = filesInChunk
                .Where(f => f.fileId > fileId)
                .OrderBy(f => f.offset)
                .FirstOrDefault().offset;
            
            if (nextOffset == 0)
                nextOffset = chunkData.Count;
            
            fileSize = nextOffset - offset;
            
            var fileEntry = new ArchiveFormat.FileEntry
            {
                FileID = fileId,
                Name = path,
                OriginalSize = fileSize,
                ChunkIDs = new[] { chunkId },
                OffsetInFirstChunk = offset,
                Algorithm = HyperAlgorithm.HyperGeneral_Binary,
                LastModified = DateTime.UtcNow
            };
            
            fileEntries.Add(fileEntry);
        }
        
        _logger?.LogDebug($"Wrote chunk {chunkId}: {uncompressed.Length:N0} → {compressed.Length:N0} bytes");
    }
}

/// <summary>
/// Result of archive creation.
/// </summary>
public class ArchiveCreationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ArchivePath { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public float CompressionRatio { get; set; }
}

/// <summary>
/// Progress information for archive creation.
/// </summary>
public class ArchiveProgress
{
    public int FilesProcessed { get; set; }
    public int TotalFiles { get; set; }
    public long BytesProcessed { get; set; }
    
    public float PercentComplete => TotalFiles > 0 ? (float)FilesProcessed / TotalFiles * 100 : 0;
}
