using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RamOptimizer.Compression.Algorithms;

namespace RamOptimizer.Compression
{
    /// <summary>
    /// Standard Mode compression engine using proven algorithms (Zstd, LZ4, Brotli)
    /// </summary>
    public class StandardModeEngine
    {
        private readonly ILogger? _logger;
        private readonly SmartBackupManager _backupManager;
        private readonly FileTypeClassifier _fileClassifier;
        private readonly Dictionary<string, ICompressionAlgorithm> _algorithms;

        public StandardModeEngine(ILogger? logger = null)
        {
            _logger = logger;
            _backupManager = new SmartBackupManager(logger);
            _fileClassifier = new FileTypeClassifier();
            
            // Initialize available algorithms
            _algorithms = new Dictionary<string, ICompressionAlgorithm>(StringComparer.OrdinalIgnoreCase)
            {
                ["Zstandard"] = new ZstandardAlgorithm(),
                ["LZ4"] = new LZ4Algorithm(),
                ["Brotli"] = new BrotliAlgorithm()
            };
        }

        /// <summary>
        /// Compress a file using smart algorithm selection
        /// </summary>
        public async Task<CompressionResult> CompressFileAsync(
            string filePath,
            BackupRetentionPolicy backupPolicy = BackupRetentionPolicy.DeleteImmediately,
            string? specificAlgorithm = null,
            int? specificLevel = null,
            bool allowMediaCompression = false,
            double minSavingsThreshold = 0.05)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            long originalSize = fileInfo.Length;

            _logger?.LogInformation($"Starting compression: {Path.GetFileName(filePath)} ({FormatSize(originalSize)})");

            // Check if file should be skipped
            if (_fileClassifier.ShouldSkipCompression(filePath, allowMediaCompression))
            {
                _logger?.LogInformation("File already compressed or not suitable - skipping");
                return new CompressionResult
                {
                    OriginalSize = originalSize,
                    CompressedSize = originalSize,
                    Algorithm = "Skipped",
                    Success = true
                };
            }

            // Step 1: Create temporary backup
            BackupInfo? backup = null;
            try
            {
                backup = await _backupManager.CreateTemporaryBackupAsync(filePath, backupPolicy);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to create backup: {ex.Message}");
                return new CompressionResult { Success = false, Error = "Backup creation failed", OriginalSize = originalSize };
            }

            try
            {
                // Step 2: Select algorithm and compression level
                string algorithmName = specificAlgorithm ?? _fileClassifier.GetRecommendedAlgorithm(filePath, originalSize);
                int compressionLevel = specificLevel ?? _fileClassifier.GetRecommendedCompressionLevel(filePath, originalSize);

                if (!_algorithms.TryGetValue(algorithmName, out var algorithm))
                {
                    throw new InvalidOperationException($"Algorithm not found: {algorithmName}");
                }

                _logger?.LogInformation($"Using {algorithmName} compression (level {compressionLevel})");

                // Step 3: Calculate original file hash
                string originalHash = await CalculateFileHashAsync(filePath);

                // Step 4: Compress to temporary file
                string tempCompressedPath = filePath + ".tmp.compressed";
                CompressionResult result;

                using (var inputStream = File.OpenRead(filePath))
                using (var outputStream = File.Create(tempCompressedPath))
                {
                    result = await algorithm.CompressAsync(inputStream, outputStream, compressionLevel);
                }

                if (!result.Success)
                {
                    _logger?.LogError($"Compression failed: {result.Error}");
                    File.Delete(tempCompressedPath);
                    return result;
                }

                // Step 5: Verify compression was successful  
                bool verified = await VerifyCompressionAsync(tempCompressedPath, algorithm, originalHash, originalSize);
                
                if (!verified)
                {
                    _logger?.LogError("Compression verification failed!");
                    File.Delete(tempCompressedPath);
                    
                    // Restore from backup
                    await _backupManager.RestoreFromBackupAsync(backup);
                    
                    return new CompressionResult 
                    { 
                        Success = false, 
                        Error = "Verification failed", 
                        OriginalSize = originalSize 
                    };
                }

                // Step 6: Check if compression actually saved space
                var compressedInfo = new FileInfo(tempCompressedPath);
                long compressedSize = compressedInfo.Length;
                
                double savingsRatio = 1.0 - (compressedSize / (double)originalSize);
                if (savingsRatio < minSavingsThreshold) // Doesn't meet minimum threshold
                {
                    _logger?.LogWarning($"Compression not worth it: {savingsRatio:P2} savings (minimum: {minSavingsThreshold:P2})");
                    File.Delete(tempCompressedPath);
                    _backupManager.DeleteBackupAfterVerification(backup);
                    
                     return new CompressionResult
                    {
                        OriginalSize = originalSize,
                        CompressedSize = originalSize,
                        Algorithm = "Skipped (insufficient savings)",
                        Success = true
                    };
                }

                // Step 7: Replace original with compressed
                File.Delete(filePath);
                File.Move(tempCompressedPath, filePath);

                // Step 8: Delete backup (if policy allows)
                _backupManager.DeleteBackupAfterVerification(backup);

                // Calculate actual savings
                long netSavings = originalSize - compressedSize;
                
                _logger?.LogInformation($"✅ Compression successful!");
                _logger?.LogInformation($"   Original: {FormatSize(originalSize)}");
                _logger?.LogInformation($"   Compressed: {FormatSize(compressedSize)} ({result.CompressionRatio:P2})");
                _logger?.LogInformation($"   NET SAVINGS: {FormatSize(netSavings)} ({result.SpaceSaved:P2})");
                _logger?.LogInformation($"   Time: {result.CompressionTime.TotalSeconds:F2}s");

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Compression failed: {ex.Message}");
                
                // Try to restore from backup
                if (backup != null)
                {
                    await _backupManager.RestoreFromBackupAsync(backup);
                }
                
                return new CompressionResult
                {
                    Success = false,
                    Error = ex.Message,
                    OriginalSize = originalSize
                };
            }
        }

        /// <summary>
        /// Decompress a file
        /// </summary>
        public async Task<bool> DecompressFileAsync(string filePath, string algorithmName)
        {
            if (!_algorithms.TryGetValue(algorithmName, out var algorithm))
            {
                _logger?.LogError($"Algorithm not found: {algorithmName}");
                return false;
            }

            try
            {
                _logger?.LogInformation($"Decompressing: {Path.GetFileName(filePath)}");

                string tempDecompressedPath = filePath + ".tmp.decompressed";

                using (var inputStream = File.OpenRead(filePath))
                using (var outputStream = File.Create(tempDecompressedPath))
                {
                    await algorithm.DecompressAsync(inputStream, outputStream);
                }

                // Replace compressed with decompressed
                File.Delete(filePath);
                File.Move(tempDecompressedPath, filePath);

                _logger?.LogInformation("Decompression successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Decompression failed: {ex.Message}");
                return false;
            }
        }

        #region Verification

        private async Task<bool> VerifyCompressionAsync(
            string compressedPath,
            ICompressionAlgorithm algorithm,
            string originalHash,
            long originalSize)
        {
            try
            {
                _logger?.LogInformation("Verifying compression...");

                // Decompress to temporary location
                string tempDecompressedPath = compressedPath + ".verify";
                
                using (var compressedStream = File.OpenRead(compressedPath))
                using (var decompressedStream = File.Create(tempDecompressedPath))
                {
                    await algorithm.DecompressAsync(compressedStream, decompressedStream);
                }

                // Verify size
                var decompressedInfo = new FileInfo(tempDecompressedPath);
                if (decompressedInfo.Length != originalSize)
                {
                    _logger?.LogError($"Size mismatch: {decompressedInfo.Length} != {originalSize}");
                    File.Delete(tempDecompressedPath);
                    return false;
                }

                // Verify hash
                string decompressedHash = await CalculateFileHashAsync(tempDecompressedPath);
                if (decompressedHash != originalHash)
                {
                    _logger?.LogError("Hash mismatch - data corrupted!");
                    File.Delete(tempDecompressedPath);
                    return false;
                }

                // Cleanup
                File.Delete(tempDecompressedPath);
                
                _logger?.LogInformation("✅ Verification passed");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Verification failed: {ex.Message}");
                return false;
            }
        }

        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var sha512 = SHA512.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await sha512.ComputeHashAsync(stream);
            return Convert.ToBase64String(hash);
        }

        #endregion

        #region Helpers

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

        #region Cleanup

        public void CleanupAllBackups()
        {
            _backupManager.CleanupAllBackups();
        }

        #endregion
    }
}
