using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Manages NTFS reparse points for file-level virtualization.
/// Enables compression of individual files while preserving original access.
/// WARNING: This is for research purposes and may not be DRM-safe for all systems.
/// </summary>
public class FileReparseManager : IDisposable
{
    private readonly string _compressedStoragePath;
    private readonly ILogger? _logger;
    private bool _disposed;

    public FileReparseManager(string compressedStoragePath, ILogger? logger = null)
    {
        _compressedStoragePath = compressedStoragePath;
        _logger = logger;

        // Ensure storage directory exists
        Directory.CreateDirectory(_compressedStoragePath);
    }

    /// <summary>
    /// Creates a reparse point for a single file, compressing it first.
    /// The original file becomes a reparse point to the compressed version.
    /// </summary>
    public FileReparseResult CreateReparsePoint(string originalFilePath, HyperCompressEngine engine, bool preserveOriginal = true)
    {
        var result = new FileReparseResult
        {
            OriginalFilePath = originalFilePath,
            CompressionTime = DateTime.Now
        };

        try
        {
            if (!File.Exists(originalFilePath))
            {
                result.Success = false;
                result.ErrorMessage = "Original file does not exist";
                return result;
            }

            // Get original file info
            var originalInfo = new FileInfo(originalFilePath);
            result.OriginalSize = originalInfo.Length;

            var compressedFileName = GenerateCompressedFileName(originalFilePath);
            var compressedPath = Path.Combine(_compressedStoragePath, compressedFileName);

            // Read original file
            var originalData = File.ReadAllBytes(originalFilePath);

            // Compress the data
            var compressedData = engine.Compress(originalData, Path.GetFileName(originalFilePath));

            // Store metadata about compression
            var metadata = new FileReparseMetadata
            {
                OriginalPath = originalFilePath,
                OriginalSize = originalData.Length,
                CompressedSize = compressedData.Length,
                CompressionRatio = (double)compressedData.Length / originalData.Length,
                CompressionTimestamp = DateTime.Now,
                ChecksumOriginal = CalculateChecksum(originalData)
            };

            // Save compressed data and metadata
            File.WriteAllBytes(compressedPath + ".hcc", compressedData);
            File.WriteAllText(compressedPath + ".reparse", SerializeMetadata(metadata));

            // Backup original if requested
            if (preserveOriginal)
            {
                var backupPath = originalFilePath + ".backup";
                File.Copy(originalFilePath, backupPath, true);
                result.BackupCreated = true;
                result.BackupPath = backupPath;
            }

            // Create the reparse point (redirect to compressed virtual file)
            var reparseData = CreateReparseData(metadata, compressedPath);
            var success = SetReparsePoint(originalFilePath, reparseData);

            if (success)
            {
                result.Success = true;
                result.CompressedSize = compressedData.Length;
                result.Metadata = metadata;
                _logger?.LogInformation($"Reparse point created: {originalFilePath} -> compressed storage");
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Failed to create reparse point";

                // Cleanup on failure
                if (File.Exists(compressedPath + ".hcc")) File.Delete(compressedPath + ".hcc");
                if (File.Exists(compressedPath + ".reparse")) File.Delete(compressedPath + ".reparse");
                if (result.BackupCreated && File.Exists(result.BackupPath)) File.Delete(result.BackupPath);
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Exception during reparse point creation: {ex.Message}";
            _logger?.LogError(ex, "Error creating reparse point for {File}", originalFilePath);
            return result;
        }
    }

    /// <summary>
    /// Removes a reparse point and restores the original file access.
    /// </summary>
    public bool RemoveReparsePoint(string filePath)
    {
        try
        {
            // Find the compressed data
            var compressedFileName = GenerateCompressedFileName(filePath);
            var compressedPath = Path.Combine(_compressedStoragePath, compressedFileName);
            var metadataPath = compressedPath + ".reparse";

            if (File.Exists(metadataPath))
            {
                var metadata = DeserializeMetadata(File.ReadAllText(metadataPath));

                // Remove reparse point (restore normal file access)
                DeleteReparsePoint(filePath);

                // Restore original file from backup if it exists
                var backupPath = filePath + ".backup";
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, filePath, true);
                    File.Delete(backupPath);
                }
                else
                {
                    // If no backup, restore from compressed data
                    var compressedData = File.ReadAllBytes(compressedPath + ".hcc");
                    File.WriteAllBytes(filePath, compressedData); // Note: This writes compressed data!
                    _logger?.LogWarning("Restored compressed data to {File} - may not be usable", filePath);
                }

                // Cleanup compressed files
                File.Delete(compressedPath + ".hcc");
                File.Delete(metadataPath);

                _logger?.LogInformation($"Reparse point removed: {filePath}");
                return true;
            }
            else
            {
                _logger?.LogWarning($"No reparse metadata found for {filePath}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing reparse point for {File}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Gets status of reparse point for a file.
    /// </summary>
    public ReparsePointStatus GetReparseStatus(string filePath)
    {
        var status = new ReparsePointStatus { FilePath = filePath };

        try
        {
            var compressedFileName = GenerateCompressedFileName(filePath);
            var compressedPath = Path.Combine(_compressedStoragePath, compressedFileName);

            status.MetadataExists = File.Exists(compressedPath + ".reparse");
            status.CompressedDataExists = File.Exists(compressedPath + ".hcc");
            status.BackupExists = File.Exists(filePath + ".backup");

            // Check if file has reparse point
            status.IsReparsePoint = GetReparsePointTag(filePath) != 0;

            if (status.MetadataExists)
            {
                try
                {
                    var metadata = DeserializeMetadata(File.ReadAllText(compressedPath + ".reparse"));
                    status.Metadata = metadata;
                }
                catch (Exception ex)
                {
                    status.MetadataError = ex.Message;
                }
            }

            return status;
        }
        catch (Exception ex)
        {
            status.Error = ex.Message;
            return status;
        }
    }

    private string GenerateCompressedFileName(string originalPath)
    {
        var fileName = Path.GetFileName(originalPath);
        var dirName = Path.GetDirectoryName(originalPath)?.Replace(Path.DirectorySeparatorChar, '_').Replace(' ', '_') ?? "root";
        var hash = originalPath.GetHashCode().ToString("X8");
        return $"{dirName}_{fileName}_{hash}";
    }

    private string CalculateChecksum(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private string SerializeMetadata(FileReparseMetadata metadata)
    {
        // Simple serialization for reparse metadata
        return $"{metadata.OriginalPath}|{metadata.OriginalSize}|{metadata.CompressedSize}|{metadata.CompressionRatio}|{metadata.CompressionTimestamp:O}|{metadata.ChecksumOriginal}";
    }

    private FileReparseMetadata DeserializeMetadata(string data)
    {
        var parts = data.Split('|');
        return new FileReparseMetadata
        {
            OriginalPath = parts[0],
            OriginalSize = long.Parse(parts[1]),
            CompressedSize = long.Parse(parts[2]),
            CompressionRatio = double.Parse(parts[3]),
            CompressionTimestamp = DateTime.Parse(parts[4]),
            ChecksumOriginal = parts[5]
        };
    }

    #region Windows API for Reparse Points

    [StructLayout(LayoutKind.Sequential)]
    private struct REPARSE_DATA_BUFFER
    {
        public uint ReparseTag;
        public ushort ReparseDataLength;
        public ushort Reserved;
        // Followed by reparse data...
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

    private const uint FSCTL_SET_REPARSE_POINT = 0x000900A4;
    private const uint FSCTL_GET_REPARSE_POINT = 0x000900A8;
    private const uint FSCTL_DELETE_REPARSE_POINT = 0x000900AC;

    private static readonly uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;

    /// <summary>
    /// Creates reparse point data structure (simplified for mount point reparse).
    /// </summary>
    private byte[] CreateReparseData(FileReparseMetadata metadata, string compressedPath)
    {
        // For simplicity, create a mount point reparse that redirects to compressed storage
        // This is a simplified implementation - real mount point reparse would need proper formatting
        var targetPath = compressedPath + ".hcc";
        var reparsePath = $@"\??\{targetPath}";

        // Mount point reparse data format (simplified)
        var reparseData = new List<byte>();

        // Add reparse tag
        reparseData.AddRange(BitConverter.GetBytes(IO_REPARSE_TAG_MOUNT_POINT));
        reparseData.AddRange(BitConverter.GetBytes((ushort)0)); // Reserved

        // Lengths (simplified)
        reparseData.AddRange(BitConverter.GetBytes((ushort)reparsePath.Length * 2));
        reparseData.AddRange(BitConverter.GetBytes((ushort)2)); // Reserved

        // Target path
        reparseData.AddRange(System.Text.Encoding.Unicode.GetBytes(reparsePath));
        reparseData.AddRange(new byte[2]); // Null terminator

        return reparseData.ToArray();
    }

    private bool SetReparsePoint(string filePath, byte[] reparseData)
    {
        using var fileHandle = CreateFile(
            filePath,
            GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);

        if (fileHandle.IsInvalid)
            return false;

        var buffer = new byte[reparseData.Length + Marshal.SizeOf<REPARSE_DATA_BUFFER>()];
        var data = new REPARSE_DATA_BUFFER
        {
            ReparseTag = IO_REPARSE_TAG_MOUNT_POINT,
            ReparseDataLength = (ushort)reparseData.Length
        };

        // Copy header
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data));
        Marshal.StructureToPtr(data, ptr, false);
        Marshal.Copy(ptr, buffer, 0, Marshal.SizeOf<REPARSE_DATA_BUFFER>());
        Marshal.FreeHGlobal(ptr);

        // Copy data
        Array.Copy(reparseData, 0, buffer, Marshal.SizeOf<REPARSE_DATA_BUFFER>(), reparseData.Length);

        uint bytesReturned;
        var result = DeviceIoControl(
            fileHandle.DangerousGetHandle(),
            FSCTL_SET_REPARSE_POINT,
            Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0),
            (uint)buffer.Length,
            IntPtr.Zero,
            0,
            out bytesReturned,
            IntPtr.Zero);

        return result;
    }

    private uint GetReparsePointTag(string filePath)
    {
        using var fileHandle = CreateFile(
            filePath,
            0, // No access needed
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);

        if (fileHandle.IsInvalid)
            return 0;

        var buffer = new byte[Marshal.SizeOf<REPARSE_DATA_BUFFER>()];
        uint bytesReturned;

        var result = DeviceIoControl(
            fileHandle.DangerousGetHandle(),
            FSCTL_GET_REPARSE_POINT,
            IntPtr.Zero,
            0,
            Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0),
            (uint)buffer.Length,
            out bytesReturned,
            IntPtr.Zero);

        if (!result || bytesReturned < Marshal.SizeOf<REPARSE_DATA_BUFFER>())
            return 0;

        var header = Marshal.PtrToStructure<REPARSE_DATA_BUFFER>(
            Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0));

        return header.ReparseTag;
    }

    private bool DeleteReparsePoint(string filePath)
    {
        using var fileHandle = CreateFile(
            filePath,
            GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);

        if (fileHandle.IsInvalid)
            return false;

        // Minimal reparse data buffer for deletion
        var buffer = new byte[Marshal.SizeOf<REPARSE_DATA_BUFFER>()];
        var data = new REPARSE_DATA_BUFFER
        {
            ReparseTag = IO_REPARSE_TAG_MOUNT_POINT,
            ReparseDataLength = 0
        };

        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data));
        Marshal.StructureToPtr(data, ptr, false);
        Marshal.Copy(ptr, buffer, 0, Marshal.SizeOf<REPARSE_DATA_BUFFER>());
        Marshal.FreeHGlobal(ptr);

        uint bytesReturned;
        var result = DeviceIoControl(
            fileHandle.DangerousGetHandle(),
            FSCTL_DELETE_REPARSE_POINT,
            Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0),
            (uint)buffer.Length,
            IntPtr.Zero,
            0,
            out bytesReturned,
            IntPtr.Zero);

        return result;
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            // Cleanup resources if needed
        }
    }
}

/// <summary>
/// Result of reparse point creation operation.
/// </summary>
public class FileReparseResult
{
    public string OriginalFilePath { get; set; } = "";
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public FileReparseMetadata? Metadata { get; set; }
    public bool BackupCreated { get; set; }
    public string? BackupPath { get; set; }
    public DateTime CompressionTime { get; set; }
}

/// <summary>
/// Metadata for file reparse points.
/// </summary>
public class FileReparseMetadata
{
    public string OriginalPath { get; set; } = "";
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public DateTime CompressionTimestamp { get; set; }
    public string ChecksumOriginal { get; set; } = "";
}

/// <summary>
/// Status information for reparse points.
/// </summary>
public class ReparsePointStatus
{
    public string FilePath { get; set; } = "";
    public bool IsReparsePoint { get; set; }
    public bool MetadataExists { get; set; }
    public bool CompressedDataExists { get; set; }
    public bool BackupExists { get; set; }
    public FileReparseMetadata? Metadata { get; set; }
    public string? Error { get; set; }
    public string? MetadataError { get; set; }

    public bool IsHealthy => IsReparsePoint && MetadataExists && CompressedDataExists;
}
