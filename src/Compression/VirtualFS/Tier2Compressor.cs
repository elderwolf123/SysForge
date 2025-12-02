using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RamOptimizer.Compression.Algorithms;

namespace RamOptimizer.Compression.VirtualFS
{
    /// <summary>
    /// Compresses a game folder to Tier 2 format (Zstandard compressed storage)
    /// </summary>
    public class Tier2Compressor
    {
        private readonly ZstandardAlgorithm _compressor;
        private readonly ILogger? _logger;

        public Tier2Compressor(ILogger? logger = null)
        {
            _compressor = new ZstandardAlgorithm();
            _logger = logger;
        }

        /// <summary>
        /// Compress a game folder to Tier 2 compressed storage
        /// </summary>
        public async Task<Tier2CompressionResult> CompressToTier2Async(
            string sourcePath,
            string destinationPath,
            string gameName,
            int zstdLevel = 19)
        {
            _logger?.LogInformation($"Starting Tier 2 compression: {sourcePath} -> {destinationPath}");

            var result = new Tier2CompressionResult
            {
                GameName = gameName,
                SourcePath = sourcePath,
                DestinationPath = destinationPath
            };

            try
            {
                // Create destination directory
                Directory.CreateDirectory(destinationPath);

                // Initialize metadata database
                var metadataDB = new CompressionMetadataDatabase(destinationPath);
                metadataDB.SetGameInfo(gameName, sourcePath, DateTime.UtcNow);

                // Get all files
                var allFiles = Directory.Exists(sourcePath)
                    ? Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories).ToList()
                    : new[] { sourcePath }.ToList();

                result.TotalFiles = allFiles.Count;
                _logger?.LogInformation($"Found {allFiles.Count} files to compress");

                // Compress each file
                foreach (var file in allFiles)
                {
                    try
                    {
                        await CompressFileToTier2(file, sourcePath, destinationPath, metadataDB, zstdLevel);
                        result.FilesCompressed++;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"Failed to compress {file}: {ex.Message}");
                        result.FilesFailed++;
                    }
                }

                // Calculate totals
                var allCompressedFiles = Directory.GetFiles(destinationPath, "*.zst", SearchOption.AllDirectories);
                result.OriginalSize = allFiles.Sum(f => new FileInfo(f).Length);
                result.CompressedSize = allCompressedFiles.Sum(f => new FileInfo(f).Length);

                result.Success = true;
                _logger?.LogInformation($"Tier 2 compression complete!");
                _logger?.LogInformation($"  Files: {result.FilesCompressed}/{result.TotalFiles}");
                _logger?.LogInformation($"  Size: {FormatSize(result.OriginalSize)} -> {FormatSize(result.CompressedSize)} ({result.CompressionRatio:P2})");

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Tier 2 compression failed: {ex.Message}");
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }

        private async Task CompressFileToTier2(
            string filePath,
            string sourceRoot,
            string destRoot,
            CompressionMetadataDatabase metadataDB,
            int zstdLevel)
        {
            // Calculate virtual path (relative to source root)
            string virtualPath = Path.GetRelativePath(sourceRoot, filePath);
            
            // Calculate compressed storage path
            string compressedPath = Path.Combine(destRoot, virtualPath + ".zst");
            Directory.CreateDirectory(Path.GetDirectoryName(compressedPath)!);

            // Get original file info
            var fileInfo = new FileInfo(filePath);
            long originalSize = fileInfo.Length;

            // Compute hash
            string hash = await ComputeSha512HashAsync(filePath);

            // Compress file
            using var inputStream = File.OpenRead(filePath);
            using var outputStream = File.Create(compressedPath);
            
            await _compressor.CompressAsync(inputStream, outputStream, zstdLevel);

            long compressedSize = new FileInfo(compressedPath).Length;

            // Save metadata
            metadataDB.SetFileMetadata("\\" + virtualPath, new FileMetadata
            {
                VirtualPath = "\\" + virtualPath,
                CompressedPath = compressedPath,
                OriginalSize = originalSize,
                CompressedSize = compressedSize,
                Sha512Hash = hash,
                LastModified = fileInfo.LastWriteTimeUtc,
                IsDirectory = false
            });

            _logger?.LogDebug($"Compressed: {virtualPath} ({FormatSize(originalSize)} -> {FormatSize(compressedSize)})");
        }

        private async Task<string> ComputeSha512HashAsync(string filePath)
        {
            using var sha512 = System.Security.Cryptography.SHA512.Create();
            using var stream = File.OpenRead(filePath);
            
            var hashBytes = await sha512.ComputeHashAsync(stream);
            return Convert.ToBase64String(hashBytes);
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

    public class Tier2CompressionResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string GameName { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int FilesCompressed { get; set; }
        public int FilesFailed { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        
        public double CompressionRatio => CompressedSize / (double)OriginalSize;
        public double SpaceSaved => 1.0 - CompressionRatio;
    }
}
