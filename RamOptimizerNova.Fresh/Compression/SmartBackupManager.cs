using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.Compression
{
    /// <summary>
    /// Backup retention policies for compression operations
    /// </summary>
    public enum BackupRetentionPolicy
    {
        DeleteImmediately,  // Delete after successful verification (default - max space savings)
        Keep1Hour,          // Keep for 1 hour (for testing/paranoid users)
        Keep1Day,           // Keep for 24 hours (very cautious users)
        Keep1Week,          // Keep for 7 days (not recommended - wastes significant space)
        KeepPermanent       // Never auto-delete (user explicitly wants - wastes space)
    }

    /// <summary>
    /// Smart backup manager that creates temporary backups with configurable retention
    /// to ensure safety without nullifying space savings
    /// </summary>
    public class SmartBackupManager
    {
        private readonly ILogger? _logger;
        private readonly string _tempBackupDirectory;

        public SmartBackupManager(ILogger? logger = null)
        {
            _logger = logger;
            
            // Store backups in dedicated temp directory for easy cleanup
            _tempBackupDirectory = Path.Combine(
                Path.GetTempPath(), 
                "RamOptimizerBackups"
            );
            
            Directory.CreateDirectory(_tempBackupDirectory);
        }

        /// <summary>
        /// Create a temporary backup with specified retention policy
        /// </summary>
        public async Task<BackupInfo> CreateTemporaryBackupAsync(
            string originalPath, 
            BackupRetentionPolicy policy = BackupRetentionPolicy.DeleteImmediately)
        {
            if (!File.Exists(originalPath))
            {
                throw new FileNotFoundException($"Original file not found: {originalPath}");
            }

            // Generate unique backup path
            string fileName = Path.GetFileName(originalPath);
            string backupFileName = $"{fileName}.backup.{Guid.NewGuid():N}";
            string backupPath = Path.Combine(_tempBackupDirectory, backupFileName);

            _logger?.LogInformation($"Creating temporary backup: {fileName}");

            // Copy file to backup location
            await Task.Run(() => File.Copy(originalPath, backupPath, overwrite: true));

            var fileInfo = new FileInfo(originalPath);
            long fileSize = fileInfo.Length;

            // Calculate file hash for verification
            string fileHash = await CalculateFileHashAsync(originalPath);

            var backupInfo = new BackupInfo
            {
                OriginalPath = originalPath,
                BackupPath = backupPath,
                FileSize = fileSize,
                FileHash = fileHash,
                CreatedTime = DateTime.UtcNow,
                RetentionPolicy = policy
            };

            _logger?.LogInformation($"Backup created: {FormatSize(fileSize)}");
            _logger?.LogInformation($"Retention policy: {GetRetentionDescription(policy)}");

            // Schedule auto-deletion if applicable
            if (policy != BackupRetentionPolicy.DeleteImmediately && 
                policy != BackupRetentionPolicy.KeepPermanent)
            {
                ScheduleBackupDeletion(backupPath, policy);
            }

            return backupInfo;
        }

        /// <summary>
        /// Delete backup after successful compression verification
        /// </summary>
        public void DeleteBackupAfterVerification(BackupInfo backupInfo)
        {
            if (backupInfo.RetentionPolicy == BackupRetentionPolicy.DeleteImmediately)
            {
                TryDeleteBackup(backupInfo.BackupPath);
                _logger?.LogInformation($"Backup deleted immediately, freed: {FormatSize(backupInfo.FileSize)}");
            }
            else if (backupInfo.RetentionPolicy != BackupRetentionPolicy.KeepPermanent)
            {
                _logger?.LogInformation($"Backup will auto-delete: {GetRetentionDescription(backupInfo.RetentionPolicy)}");
            }
            else
            {
                _logger?.LogWarning($"Backup kept permanently: {FormatSize(backupInfo.FileSize)} (space not freed!)");
            }
        }

        /// <summary>
        /// Restore file from backup
        /// </summary>
        public async Task<bool> RestoreFromBackupAsync(BackupInfo backupInfo)
        {
            try
            {
                if (!File.Exists(backupInfo.BackupPath))
                {
                    _logger?.LogError($"Backup file not found: {backupInfo.BackupPath}");
                    return false;
                }

                _logger?.LogWarning($"Restoring from backup: {Path.GetFileName(backupInfo.OriginalPath)}");

                // Verify backup integrity before restoring
                string backupHash = await CalculateFileHashAsync(backupInfo.BackupPath);
                if (backupHash != backupInfo.FileHash)
                {
                    _logger?.LogError("Backup file corrupted - hash mismatch!");
                    return false;
                }

                // Restore backup to original location
                await Task.Run(() => File.Copy(backupInfo.BackupPath, backupInfo.OriginalPath, overwrite: true));

                _logger?.LogInformation("File successfully restored from backup");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to restore from backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clean up all temporary backups
        /// </summary>
        public void CleanupAllBackups()
        {
            try
            {
                if (Directory.Exists(_tempBackupDirectory))
                {
                    var files = Directory.GetFiles(_tempBackupDirectory, "*.backup.*");
                    long totalFreed = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            totalFreed += fileInfo.Length;
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore errors for individual files
                        }
                    }

                    _logger?.LogInformation($"Cleaned up {files.Length} backup files, freed: {FormatSize(totalFreed)}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to cleanup backups: {ex.Message}");
            }
        }

        #region Private Helpers

        private void ScheduleBackupDeletion(string backupPath, BackupRetentionPolicy policy)
        {
            TimeSpan deleteAfter = policy switch
            {
                BackupRetentionPolicy.Keep1Hour => TimeSpan.FromHours(1),
                BackupRetentionPolicy.Keep1Day => TimeSpan.FromDays(1),
                BackupRetentionPolicy.Keep1Week => TimeSpan.FromDays(7),
                _ => TimeSpan.Zero
            };

            if (deleteAfter > TimeSpan.Zero)
            {
                // Schedule deletion in background
                _ = Task.Run(async () =>
                {
                    await Task.Delay(deleteAfter);
                    TryDeleteBackup(backupPath);
                    _logger?.LogInformation($"Auto-deleted backup after {GetRetentionDescription(policy)}");
                });
            }
        }

        private void TryDeleteBackup(string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to delete backup {Path.GetFileName(backupPath)}: {ex.Message}");
            }
        }

        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var sha512 = SHA512.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await sha512.ComputeHashAsync(stream);
            return Convert.ToBase64String(hash);
        }

        private string GetRetentionDescription(BackupRetentionPolicy policy)
        {
            return policy switch
            {
                BackupRetentionPolicy.DeleteImmediately => "Delete immediately after verification",
                BackupRetentionPolicy.Keep1Hour => "Keep for 1 hour",
                BackupRetentionPolicy.Keep1Day => "Keep for 1 day",
                BackupRetentionPolicy.Keep1Week => "Keep for 1 week",
                BackupRetentionPolicy.KeepPermanent => "Keep permanently",
                _ => "Unknown"
            };
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

        #endregion
    }

    /// <summary>
    /// Information about a created backup
    /// </summary>
    public class BackupInfo
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public BackupRetentionPolicy RetentionPolicy { get; set; }
    }
}
