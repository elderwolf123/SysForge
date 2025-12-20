using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Fsp;
using Microsoft.Extensions.Logging;
using RamOptimizer.Compression.Algorithms;

namespace RamOptimizer.Compression.VirtualFS
{
    /// <summary>
    /// WinFsp-based transparent compression file system
    /// Presents compressed files as normal files with on-the-fly decompression
    /// </summary>
    public class TransparentCompressionFileSystem : FileSystemBase
    {
        private readonly string _compressedStoragePath;
        private readonly CompressionMetadataDatabase _metadataDB;
        private readonly IntelligentDecompressionCache _cache;
        private readonly ZstandardAlgorithm _decompressor;
        private readonly ILogger? _logger;

        public TransparentCompressionFileSystem(
            string compressedStoragePath,
            long cacheSizeMB = 2048,
            ILogger? logger = null)
        {
            _compressedStoragePath = compressedStoragePath;
            _metadataDB = new CompressionMetadataDatabase(compressedStoragePath);
            _cache = new IntelligentDecompressionCache(cacheSizeMB);
            _decompressor = new ZstandardAlgorithm();
            _logger = logger;

            _logger?.LogInformation($"Initialized TransparentCompressionFileSystem at {compressedStoragePath}");
        }

        #region WinFsp Overrides

        public override int GetVolumeInfo(
            out Fsp.Interop.VolumeInfo VolumeInfo)
        {
            VolumeInfo = new Fsp.Interop.VolumeInfo
            {
                TotalSize = 1024UL * 1024 * 1024 * 1024, // 1TB virtual
                FreeSize = 512UL * 1024 * 1024 * 1024    // 512GB virtual
            };
            return STATUS_SUCCESS;
        }

        public override int GetSecurityByName(
            string FileName,
            out uint FileAttributes,
            ref byte[] SecurityDescriptor)
        {
            var metadata = _metadataDB.GetFileMetadata(FileName);
            
            if (metadata == null)
            {
                FileAttributes = 0;
                return STATUS_OBJECT_NAME_NOT_FOUND;
            }

            FileAttributes = (uint)(metadata.IsDirectory 
                ? System.IO.FileAttributes.Directory 
                : System.IO.FileAttributes.Normal);

            return STATUS_SUCCESS;
        }

        public override int Open(
            string FileName,
            uint CreateOptions,
            uint GrantedAccess,
            out object FileNode,
            out object FileDesc,
            out Fsp.Interop.FileInfo FileInfo,
            out string NormalizedName)
        {
            var metadata = _metadataDB.GetFileMetadata(FileName);
            
            if (metadata == null)
            {
                FileNode = null;
                FileDesc = null;
                FileInfo = default;
                NormalizedName = null;
                return STATUS_OBJECT_NAME_NOT_FOUND;
            }

            FileNode = metadata;
            FileDesc = metadata;
            NormalizedName = FileName;
            
            FileInfo = new Fsp.Interop.FileInfo
            {
                FileAttributes = (uint)(metadata.IsDirectory 
                    ? System.IO.FileAttributes.Directory 
                    : System.IO.FileAttributes.Normal),
                ReparseTag = 0,
                FileSize = (ulong)metadata.OriginalSize,
                AllocationSize = (ulong)metadata.OriginalSize,
                CreationTime = (ulong)metadata.LastModified.ToFileTimeUtc(),
                LastAccessTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
                LastWriteTime = (ulong)metadata.LastModified.ToFileTimeUtc(),
                ChangeTime = (ulong)metadata.LastModified.ToFileTimeUtc(),
                IndexNumber = 0
            };

            _logger?.LogDebug($"Opened: {FileName} (Original size: {metadata.OriginalSize})");

            return STATUS_SUCCESS;
        }

        public override int Read(
            object FileNode,
            object FileDesc,
            IntPtr Buffer,
            ulong Offset,
            uint Length,
            out uint PBytesTransferred)
        {
            var metadata = (FileMetadata)FileNode;
            
            try
            {
                // Get decompressed data from cache or decompress
                byte[] decompressedData = _cache.GetOrDecompress(
                    metadata.VirtualPath,
                    () => DecompressFile(metadata.CompressedPath)
                );

                // Calculate read range
                long offset = (long)Offset;
                int length = (int)Math.Min(Length, decompressedData.Length - offset);

                if (length <= 0)
                {
                    PBytesTransferred = 0;
                    return STATUS_SUCCESS;
                }

                // Copy to buffer
                Marshal.Copy(decompressedData, (int)offset, Buffer, length);
                PBytesTransferred = (uint)length;

                _logger?.LogTrace($"Read {length} bytes from {metadata.VirtualPath} at offset {offset}");

                return STATUS_SUCCESS;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Read failed for {metadata.VirtualPath}: {ex.Message}");
                PBytesTransferred = 0;
                return STATUS_UNEXPECTED_IO_ERROR;
            }
        }

        public override bool ReadDirectoryEntry(
            object FileNode,
            object FileDesc,
            string Pattern,
            string Marker,
            ref object Context,
            out string FileName,
            out Fsp.Interop.FileInfo FileInfo)
        {
            var dirMetadata = (FileMetadata)FileNode;
            if (!dirMetadata.IsDirectory)
            {
                FileName = null!;
                FileInfo = default;
                return false;
            }

            IEnumerator<string> enumerator;

            if (Context == null)
            {
                try
                {
                    var dirPath = dirMetadata.VirtualPath;
                    var query = _metadataDB.GetAllFilePaths()
                        .Where(p => Path.GetDirectoryName(p)?.Equals(dirPath, StringComparison.OrdinalIgnoreCase) == true);

                    if (!string.IsNullOrEmpty(Marker))
                    {
                        query = query.Where(p => string.Compare(Path.GetFileName(p), Marker, StringComparison.OrdinalIgnoreCase) > 0);
                    }

                    // Sort to ensure consistent order for Marker logic
                    var files = query.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase).ToList();
                    enumerator = files.GetEnumerator();
                    Context = enumerator;
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Failed to initialize directory enumeration for {dirMetadata.VirtualPath}: {ex.Message}");
                    FileName = null!;
                    FileInfo = default;
                    return false;
                }
            }
            else
            {
                enumerator = (IEnumerator<string>)Context;
            }

            while (enumerator.MoveNext())
            {
                var filePath = enumerator.Current;
                var fileMetadata = _metadataDB.GetFileMetadata(filePath);
                if (fileMetadata == null) continue;

                FileName = Path.GetFileName(filePath);
                
                // Simple pattern matching if needed, though FSD usually handles it
                if (!string.IsNullOrEmpty(Pattern) && Pattern != "*")
                {
                    // Basic wildcard support could go here, but for now rely on FSD
                }

                FileInfo = new Fsp.Interop.FileInfo
                {
                    FileAttributes = (uint)(fileMetadata.IsDirectory 
                        ? System.IO.FileAttributes.Directory 
                        : System.IO.FileAttributes.Normal),
                    FileSize = (ulong)fileMetadata.OriginalSize,
                    AllocationSize = (ulong)fileMetadata.OriginalSize,
                    CreationTime = (ulong)fileMetadata.LastModified.ToFileTimeUtc(),
                    LastAccessTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
                    LastWriteTime = (ulong)fileMetadata.LastModified.ToFileTimeUtc(),
                    ChangeTime = (ulong)fileMetadata.LastModified.ToFileTimeUtc(),
                    IndexNumber = 0
                };
                return true;
            }

            FileName = null!;
            FileInfo = default;
            return false;
        }

        public override int GetFileInfo(
            object FileNode,
            object FileDesc,
            out Fsp.Interop.FileInfo FileInfo)
        {
            var metadata = (FileMetadata)FileNode;
            
            FileInfo = new Fsp.Interop.FileInfo
            {
                FileAttributes = (uint)(metadata.IsDirectory 
                    ? System.IO.FileAttributes.Directory 
                    : System.IO.FileAttributes.Normal),
                ReparseTag = 0,
                FileSize = (ulong)metadata.OriginalSize,
                AllocationSize = (ulong)metadata.OriginalSize,
                CreationTime = (ulong)metadata.LastModified.ToFileTimeUtc(),
                LastAccessTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
                LastWriteTime = (ulong)metadata.LastModified.ToFileTimeUtc(),
                ChangeTime = (ulong)metadata.LastModified.ToFileTimeUtc(),
                IndexNumber = 0
            };

            return STATUS_SUCCESS;
        }

        public override void Close(
            object FileNode,
            object FileDesc)
        {
            // Cleanup if needed
            // WinFsp will call this when file handle is closed
        }

        #endregion

        #region Decompression

        private byte[] DecompressFile(string compressedPath)
        {
            _logger?.LogDebug($"Decompressing: {compressedPath}");

            using var compressedStream = File.OpenRead(compressedPath);
            using var decompressedStream = new MemoryStream();
            
            _decompressor.DecompressAsync(compressedStream, decompressedStream).Wait();
            
            return decompressedStream.ToArray();
        }

        #endregion

        #region Helpers

        // Removed AddDirInfo helper as we use ReadDirectoryEntry now

        #endregion

        #region Constants

        // Removed redundant constants as they are inherited from FileSystemBase

        #endregion

        public IntelligentDecompressionCache.CacheStatistics GetCacheStatistics()
        {
            return _cache.GetStatistics();
        }
    }
}
