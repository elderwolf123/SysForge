using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RamOptimizer.Compression.Transparent
{
    /// <summary>
    /// Per-file compression manager that uses learned algorithms for optimal results
    /// </summary>
    public class PerFileCompressor
    {
        private readonly WindowsCompactCompression _compressor;
        private readonly CompressionLearningDatabase _learningDB;
        private readonly ILogger? _logger;

        public PerFileCompressor(ILogger? logger = null)
        {
            _logger = logger;
            _compressor = new WindowsCompactCompression(logger);
            _learningDB = new CompressionLearningDatabase(logger);
        }

        /// <summary>
        /// Compress a game/program folder using per-file algorithm selection
        /// </summary>
        public async Task<PerFileCompressionResult> CompressWithLearningAsync(
            string targetPath,
            string gameName,
            bool learnFromBenchmark = true)
        {
            var result = new PerFileCompressionResult { GameName = gameName };
            
            _logger?.LogInformation($"Starting per-file compression for: {gameName}");

            // Get all files
            var allFiles = Directory.Exists(targetPath)
                ? Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories).ToList()
                : new List<string> { targetPath };

            // Group by file extension
            var filesByExtension = allFiles
                .Where(f => new FileInfo(f).Length > 0)
                .GroupBy(f => Path.GetExtension(f).ToLowerInvariant())
                .ToList();

            _logger?.LogInformation($"Found {filesByExtension.Count} file types across {allFiles.Count} files");

            // Process each file type
            foreach (var extensionGroup in filesByExtension)
            {
                string extension = extensionGroup.Key;
                var files = extensionGroup.ToList();

                _logger?.LogInformation($"Processing {extension}: {files.Count} files");

                // Get best algorithm (from learning database or benchmark)
                CompactAlgorithm bestAlgorithm;
                
                var learned = _learningDB.GetBestAlgorithm(gameName, extension);
                if (learned.HasValue)
                {
                    bestAlgorithm = learned.Value;
                    _logger?.LogInformation($"  Using learned algorithm: {bestAlgorithm}");
                }
                else if (learnFromBenchmark)
                {
                    // Benchmark on sample files
                    _logger?.LogInformation($"  No learned data, benchmarking...");
                    bestAlgorithm = await BenchmarkFileTypeAsync(files, extension, gameName);
                }
                else
                {
                    // Use smart default
                    bestAlgorithm = _learningDB.GetRecommendedAlgorithmForFile(files.First(), gameName);
                    _logger?.LogInformation($"  Using smart default: {bestAlgorithm}");
                }

                // Compress all files of this type with the chosen algorithm
                long typeOriginalSize = 0;
                long typeCompressedSize = 0;
                int typeFilesCompressed = 0;

                foreach (var file in files)
                {
                    try
                    {
                        long originalSize = new FileInfo(file).Length;
                        
                        var fileResult = await _compressor.CompressAsync(file, bestAlgorithm, recursive: false);
                        
                        if (fileResult.Success)
                        {
                            long compressedSize = new FileInfo(file).Length;
                            typeOriginalSize += originalSize;
                            typeCompressedSize += compressedSize;
                            typeFilesCompressed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"Failed to compress {Path.GetFileName(file)}: {ex.Message}");
                    }
                }

                // Record results for learning
                if (typeFilesCompressed > 0)
                {
                    double avgRatio = typeCompressedSize / (double)typeOriginalSize;
                    _learningDB.RecordResult(gameName, extension, bestAlgorithm, avgRatio, TimeSpan.Zero);
                    
                    result.FileTypeResults.Add(new FileTypeCompressionResult
                    {
                        Extension = extension,
                        Algorithm = bestAlgorithm,
                        FilesCompressed = typeFilesCompressed,
                        OriginalSize = typeOriginalSize,
                        CompressedSize = typeCompressedSize
                    });
                }
            }

            result.Success = true;
            result.TotalFilesCompressed = result.FileTypeResults.Sum(r => r.FilesCompressed);
            result.TotalOriginalSize = result.FileTypeResults.Sum(r => r.OriginalSize);
            result.TotalCompressedSize = result.FileTypeResults.Sum(r => r.CompressedSize);

            _logger?.LogInformation($"Compression complete!");
            _logger?.LogInformation($"  Files: {result.TotalFilesCompressed}");
            _logger?.LogInformation($"  Space saved: {FormatSize(result.TotalOriginalSize - result.TotalCompressedSize)} ({(1 - result.TotalCompressedSize / (double)result.TotalOriginalSize):P2})");

            return result;
        }

        private async Task<CompactAlgorithm> BenchmarkFileTypeAsync(
            List<string> files,
            string extension,
            string gameName)
        {
            // Take sample of 3-5 files
            var sampleFiles = files.Take(Math.Min(5, files.Count)).ToList();
            var selector = new SmartAlgorithmSelector(_logger);
            
            // Create temp directory with samples
            string tempDir = Path.Combine(Path.GetTempPath(), $"TypeBenchmark_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Copy samples
                foreach (var file in sampleFiles)
                {
                    string dest = Path.Combine(tempDir, Path.GetFileName(file));
                    File.Copy(file, dest);
                }

                // Benchmark
                var bestAlgorithm = await selector.SelectBestAlgorithmAsync(tempDir, sampleFiles.Count, prioritizeRatio: true);
                
                _logger?.LogInformation($"  Benchmarked {extension}: Best = {bestAlgorithm}");
                
                return bestAlgorithm;
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
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

    public class PerFileCompressionResult
    {
        public string GameName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int TotalFilesCompressed { get; set; }
        public long TotalOriginalSize { get; set; }
        public long TotalCompressedSize { get; set; }
        public List<FileTypeCompressionResult> FileTypeResults { get; set; } = new();

        public double CompressionRatio => TotalCompressedSize / (double)TotalOriginalSize;
        public double SpaceSaved => 1.0 - CompressionRatio;
    }

    public class FileTypeCompressionResult
    {
        public string Extension { get; set; } = string.Empty;
        public CompactAlgorithm Algorithm { get; set; }
        public int FilesCompressed { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        
        public double CompressionRatio => CompressedSize / (double)OriginalSize;
    }
}
