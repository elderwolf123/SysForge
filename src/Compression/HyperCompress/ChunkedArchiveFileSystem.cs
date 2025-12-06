using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Fsp;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// WinFsp-based virtual file system for HyperCompress .hca archives
/// Presents archived files as normal files with on-the-fly chunk decompression
/// </summary>
public class ChunkedArchiveFileSystem : FileSystemBase
{
    private readonly string _archivePath;
    private readonly ChunkedArchiveReader _reader;
    private readonly HyperCompressEngine _engine;
    private readonly Dictionary<string, ArchiveFormat.FileEntry> _fileIndex;
    private readonly ILogger? _logger;

    public ChunkedArchiveFileSystem(
        string archivePath,
        HyperCompressEngine engine,
        ILogger? logger = null)
    {
        _archivePath = archivePath;
        _engine = engine;
        _logger = logger;
        
        // Open archive
        _reader = new ChunkedArchiveReader(archivePath, engine, logger);
        _reader.Open();
        
        // Build file index for fast lookup
        _fileIndex = BuildFileIndex();
        
        _logger?.LogInformation($"Initialized ChunkedArchiveFileSystem for {Path.GetFileName(archivePath)}");
    }

    #region Initialization

    private Dictionary<string, ArchiveFormat.FileEntry> BuildFileIndex()
    {
        var index = new Dictionary<string, ArchiveFormat.FileEntry>(StringComparer.OrdinalIgnoreCase);
        var fileList = _reader.GetFileList();
        
        foreach (var fileName in fileList)
        {
            // For now, we'll need to add a method to get FileEntry from reader
            // or we cache the file list with metadata
            var normalizedPath = "\\" + fileName.Replace("/", "\\");
            // We'll store filename as key for now
            // A proper implementation would cache the full FileEntry
            index[normalizedPath] = null!; // Will be populated when we enhance ChunkedArchiveReader
        }
        
        // Add root directory
        index["\\"] = null!;
        
        _logger?.LogDebug($"Built file index with {fileList.Count} files");
        return index;
    }

    #endregion

    #region WinFsp Overrides

    public override int GetVolumeInfo(out Fsp.Interop.VolumeInfo VolumeInfo)
    {
        VolumeInfo = new Fsp.Interop.VolumeInfo
        {
            TotalSize = 1024UL * 1024 * 1024 * 1024, // 1TB virtual
            FreeSize = 0 // Read-only archive
        };
        return STATUS_SUCCESS;
    }

    public override int GetSecurityByName(
        string FileName,
        out uint FileAttributes,
        ref byte[] SecurityDescriptor)
    {
        if (!_fileIndex.ContainsKey(FileName))
        {
            FileAttributes = 0;
            return STATUS_OBJECT_NAME_NOT_FOUND;
        }

        // Root or directory
        if (FileName == "\\" || IsDirectory(FileName))
        {
            FileAttributes = (uint)System.IO.FileAttributes.Directory;
        }
        else
        {
            FileAttributes = (uint)System.IO.FileAttributes.Normal;
        }

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
        if (!_fileIndex.ContainsKey(FileName))
        {
            FileNode = null!;
            FileDesc = null!;
            FileInfo = default;
            NormalizedName = null!;
            return STATUS_OBJECT_NAME_NOT_FOUND;
        }

        // Store filename as FileNode
        FileNode = FileName;
        FileDesc = FileName;
        NormalizedName = FileName;

        // Get file size
        long fileSize = 0;
        bool isDirectory = (FileName == "\\" || IsDirectory(FileName));
        
        if (!isDirectory)
        {
            try
            {
                var rawFileName = FileName.TrimStart('\\').Replace("\\", "/");
                var data = _reader.ExtractFile(rawFileName);
                fileSize = data.Length;
            }
            catch
            {
                // If extraction fails, attempt to read from file list
                fileSize = 0;
            }
        }

        FileInfo = new Fsp.Interop.FileInfo
        {
            FileAttributes = (uint)(isDirectory 
                ? System.IO.FileAttributes.Directory 
                : System.IO.FileAttributes.Normal),
            ReparseTag = 0,
            FileSize = (ulong)fileSize,
            AllocationSize = (ulong)fileSize,
            CreationTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
            LastAccessTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
            LastWriteTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
            ChangeTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
            IndexNumber = 0
        };

        _logger?.LogDebug($"Opened: {FileName}");
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
        var fileName = (string)FileNode;
        
        try
        {
            // Extract file from archive
            var rawFileName = fileName.TrimStart('\\').Replace("\\", "/");
            byte[] fileData = _reader.ExtractFile(rawFileName);

            // Calculate read range
            long offset = (long)Offset;
            int length = (int)Math.Min(Length, fileData.Length - offset);

            if (length <= 0)
            {
                PBytesTransferred = 0;
                return STATUS_SUCCESS;
            }

            // Copy to buffer
            Marshal.Copy(fileData, (int)offset, Buffer, length);
            PBytesTransferred = (uint)length;

            _logger?.LogTrace($"Read {length} bytes from {fileName} at offset {offset}");
            return STATUS_SUCCESS;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Read failed for {fileName}: {ex.Message}");
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
        var dirPath = (string)FileNode;
        
        IEnumerator<string> enumerator;

        if (Context == null)
        {
            try
            {
                // Get all files in this directory
                var files = _fileIndex.Keys
                    .Where(f => f != "\\" && GetParentDirectory(f) == dirPath)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!string.IsNullOrEmpty(Marker))
                {
                    files = files.Where(f => 
                        string.Compare(Path.GetFileName(f), Marker, StringComparison.OrdinalIgnoreCase) > 0)
                        .ToList();
                }

                enumerator = files.GetEnumerator();
                Context = enumerator;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to enumerate directory {dirPath}: {ex.Message}");
                FileName = null!;
                FileInfo = default;
                return false;
            }
        }
        else
        {
            enumerator = (IEnumerator<string>)Context;
        }

        if (enumerator.MoveNext())
        {
            var filePath = enumerator.Current;
            FileName = Path.GetFileName(filePath);

            bool isDirectory = IsDirectory(filePath);
            long fileSize = 0;

            if (!isDirectory)
            {
                try
                {
                    var rawFileName = filePath.TrimStart('\\').Replace("\\", "/");
                    var data = _reader.ExtractFile(rawFileName);
                    fileSize = data.Length;
                }
                catch
                {
                    fileSize = 0;
                }
            }

            FileInfo = new Fsp.Interop.FileInfo
            {
                FileAttributes = (uint)(isDirectory 
                    ? System.IO.FileAttributes.Directory 
                    : System.IO.FileAttributes.Normal),
                FileSize = (ulong)fileSize,
                AllocationSize = (ulong)fileSize,
                CreationTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
                LastAccessTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
                LastWriteTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
                ChangeTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
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
        var fileName = (string)FileNode;
        bool isDirectory = (fileName == "\\" || IsDirectory(fileName));
        long fileSize = 0;

        if (!isDirectory)
        {
            try
            {
                var rawFileName = fileName.TrimStart('\\').Replace("\\", "/");
                var data = _reader.ExtractFile(rawFileName);
                fileSize = data.Length;
            }
            catch
            {
                fileSize = 0;
            }
        }

        FileInfo = new Fsp.Interop.FileInfo
        {
            FileAttributes = (uint)(isDirectory 
                ? System.IO.FileAttributes.Directory 
                : System.IO.FileAttributes.Normal),
            FileSize = (ulong)fileSize,
            AllocationSize = (ulong)fileSize,
            CreationTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
            LastAccessTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
            LastWriteTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
            ChangeTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
            IndexNumber = 0
        };

        return STATUS_SUCCESS;
    }

    public override void Close(object FileNode, object FileDesc)
    {
        // Cleanup if needed
    }

    #endregion

    #region Helpers

    private bool IsDirectory(string path)
    {
        // A path is a directory if other paths exist that start with it
        return _fileIndex.Keys.Any(f => 
            f != path && 
            f.StartsWith(path.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase));
    }

    private string GetParentDirectory(string path)
    {
        if (path == "\\") return null!;
        
        var parent = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parent))
            return "\\";
        
        return parent;
    }

    #endregion

    public void Dispose()
    {
        _reader?.Dispose();
    }
}
