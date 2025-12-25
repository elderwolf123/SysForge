using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.Services
{
    /// <summary>
    /// Deep scanner for finding all folders on a drive
    /// </summary>
    public class DeepScanner
    {
        private readonly FileLogger _logger = FileLogger.Instance;
        
        // System folders to exclude (prevent Windows corruption)
        private static readonly string[] ExcludedFolders = new[]
        {
            "Windows",
            "System32",
            "WinSxS",
            "SysWOW64",
            "$Recycle.Bin",
            "ProgramData\\Microsoft",
            "MSOCache",
            "Recovery",
            "System Volume Information",
            "$WINDOWS.~BT",
            "Config.Msi",
            "WindowsApps",
            "Installer"
        };
        
        /// <summary>
        /// Scan entire drive for compressible folders
        /// </summary>
        public async Task<List<ScannableFolder>> ScanDriveAsync(
            string driveLetter,
            IProgress<ScanProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<ScannableFolder>();
            var scanProgress = new ScanProgress();
            
            try
            {
                _logger.Log($"[DEEP SCAN] Starting scan of {driveLetter}:\\");
                
                var rootPath = $"{driveLetter}:\\";
                
                if (!Directory.Exists(rootPath))
                {
                    _logger.LogWarning($"Drive {driveLetter}:\\ not found");
                    return results;
                }
                
                await Task.Run(() => ScanDirectory(rootPath, results, scanProgress, progress, cancellationToken), cancellationToken);
                
                _logger.Log($"[DEEP SCAN] Completed: {results.Count} folders, {scanProgress.TotalSize / 1024 / 1024 / 1024:F2} GB");
                
                return results;
            }
            catch (OperationCanceledException)
            {
                _logger.Log("[DEEP SCAN] Scan cancelled by user");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Deep scan failed: {ex.Message}", ex);
                return results;
            }
        }
        
        private void ScanDirectory(
            string path,
            List<ScannableFolder> results,
            ScanProgress scanProgress,
            IProgress<ScanProgress>? progress,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Check if folder should be excluded
                if (ShouldExclude(path))
                {
                    _logger.Log($"[DEEP SCAN] Skipping excluded: {path}");
                    return;
                }
                
                scanProgress.CurrentPath = path;
                scanProgress.FoldersScanned++;
                
                // Report progress every 10 folders
                if (scanProgress.FoldersScanned % 10 == 0)
                {
                    progress?.Report(new ScanProgress
                    {
                        FoldersScanned = scanProgress.FoldersScanned,
                        FilesScanned = scanProgress.FilesScanned,
                        TotalSize = scanProgress.TotalSize,
                        CurrentPath = path
                    });
                }
                
                // Get folder info
                var dirInfo = new DirectoryInfo(path);
                var files = dirInfo.GetFiles();
                var fileCount = files.Length;
                var folderSize = files.Sum(f => f.Length);
                
                scanProgress.FilesScanned += fileCount;
                scanProgress.TotalSize += folderSize;
                
                // Only add folders with files
                if (fileCount > 0)
                {
                    results.Add(new ScannableFolder
                    {
                        Path = path,
                        FileCount = fileCount,
                        SizeBytes = folderSize,
                        SizeFormatted = FormatSize(folderSize)
                    });
                }
                
                // Recurse into subdirectories
                try
                {
                    var subdirs = dirInfo.GetDirectories();
                    foreach (var subdir in subdirs)
                    {
                        ScanDirectory(subdir.FullName, results, scanProgress, progress, cancellationToken);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip access denied folders
                    _logger.Log($"[DEEP SCAN] Access denied: {path}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.Log($"[DEEP SCAN] Access denied: {path}");
            }
            catch (PathTooLongException)
            {
                _logger.Log($"[DEEP SCAN] Path too long: {path}");
            }
            catch (Exception ex)
            {
                _logger.Log($"[DEEP SCAN] Error scanning {path}: {ex.Message}");
            }
        }
        
        private bool ShouldExclude(string path)
        {
            var pathUpper = path.ToUpperInvariant();
            
            foreach (var excluded in ExcludedFolders)
            {
                // Check if path contains excluded folder
                if (pathUpper.Contains($"\\{excluded.ToUpperInvariant()}\\") ||
                    pathUpper.EndsWith($"\\{excluded.ToUpperInvariant()}"))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
    
    /// <summary>
    /// Scannable folder result
    /// </summary>
    public class ScannableFolder
    {
        public string Path { get; set; } = "";
        public int FileCount { get; set; }
        public long SizeBytes { get; set; }
        public string SizeFormatted { get; set; } = "";
    }
    
    /// <summary>
    /// Scan progress information
    /// </summary>
    public class ScanProgress
    {
        public int FoldersScanned { get; set; }
        public int FilesScanned { get; set; }
        public long TotalSize { get; set; }
        public string CurrentPath { get; set; } = "";
    }
}
