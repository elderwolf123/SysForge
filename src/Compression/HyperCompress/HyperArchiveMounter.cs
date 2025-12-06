using System;
using System.IO;
using System.Runtime.InteropServices;
using Fsp;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.Compression.HyperCompress;

/// <summary>
/// Manages mounting and unmounting of HyperCompress .hca archives as virtual drives
/// </summary>
public class HyperArchiveMounter : IDisposable
{
    private readonly ILogger? _logger;
    private FileSystemHost? _fileSystemHost;
    private ChunkedArchiveFileSystem? _fileSystem;
    private string? _mountPoint;

    public bool IsMounted => _fileSystemHost != null;
    public string? CurrentMountPoint => _mountPoint;

    public HyperArchiveMounter(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Mount a .hca archive to a specific path
    /// </summary>
    public bool Mount(string archivePath, string mountPath, HyperCompressEngine engine)
    {
        if (IsMounted)
        {
            _logger?.LogWarning("Already mounted, unmount first");
            return false;
        }

        if (!File.Exists(archivePath))
        {
            _logger?.LogError($"Archive not found: {archivePath}");
            return false;
        }

        try
        {
            _logger?.LogInformation($"Mounting {Path.GetFileName(archivePath)} at {mountPath}");

            // Create mount point directory if it doesn't exist
            Directory.CreateDirectory(mountPath);

            // Create virtual file system
            _fileSystem = new ChunkedArchiveFileSystem(archivePath, engine, _logger);

            // Create WinFsp host
            _fileSystemHost = new FileSystemHost(_fileSystem);

            // Mount with specific options
            int result = _fileSystemHost.Mount(
                mountPath,
                null, // security descriptor
                true, // synchronous
                0     // debug flags
            );

            if (result == 0)
            {
                _mountPoint = mountPath;
                _logger?.LogInformation($"Successfully mounted at {mountPath}");
                return true;
            }
            else
            {
                _logger?.LogError($"Mount failed with error code: {result}");
                _fileSystemHost = null;
                _fileSystem = null;
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Mount failed: {ex.Message}");
            _fileSystemHost = null;
            _fileSystem = null;
            return false;
        }
    }

    /// <summary>
    /// Unmount the currently mounted archive
    /// </summary>
    public bool Unmount()
    {
        if (!IsMounted)
        {
            _logger?.LogWarning("Not currently mounted");
            return false;
        }

        try
        {
            _logger?.LogInformation($"Unmounting from {_mountPoint}");

            _fileSystemHost?.Unmount();
            _fileSystemHost?.Dispose();
            _fileSystem?.Dispose();

            _fileSystemHost = null;
            _fileSystem = null;
            _mountPoint = null;

            _logger?.LogInformation("Successfully unmounted");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Unmount failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Create a junction point (directory symlink) to mount point
    /// This allows games to appear at their original location
    /// </summary>
    public bool CreateJunction(string junctionPath, string targetPath)
    {
        try
        {
            if (Directory.Exists(junctionPath))
            {
                _logger?.LogWarning($"Junction target already exists: {junctionPath}");
                return false;
            }

            // Create directory junction using Windows API
            bool success = CreateSymbolicLink(junctionPath, targetPath, SYMBOLIC_LINK_FLAG_DIRECTORY);

            if (success)
            {
                _logger?.LogInformation($"Created junction: {junctionPath} -> {targetPath}");
            }
            else
            {
                _logger?.LogError($"Failed to create junction: {Marshal.GetLastWin32Error()}");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Junction creation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Remove a junction point
    /// </summary>
    public bool RemoveJunction(string junctionPath)
    {
        try
        {
            if (Directory.Exists(junctionPath))
            {
                Directory.Delete(junctionPath);
                _logger?.LogInformation($"Removed junction: {junctionPath}");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to remove junction: {ex.Message}");
            return false;
        }
    }

    #region Windows API for Junctions

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateSymbolicLink(
        string lpSymlinkFileName,
        string lpTargetFileName,
        int dwFlags);

    private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 1;

    #endregion

    public void Dispose()
    {
        if (IsMounted)
        {
            Unmount();
        }
    }
}
