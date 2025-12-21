using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RamOptimizerNova.Services;
    /// <summary>
    /// Benchmark result for a compression algorithm
    /// </summary>
    public class AlgorithmBenchmarkResult
    {
        public CompactAlgorithm Algorithm { get; set; }
        public double CompressionRatio { get; set; }
        public TimeSpan CompressionTime { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double Score { get; set; } // Combined metric for ranking
    }

    /// <summary>
    /// Smart algorithm selector for Windows Compact compression
    /// Tests all algorithms on sample files and picks the best one
    /// </summary>
    public class SmartAlgorithmSelector
    {
        private readonly ILogger? _logger;
        private readonly WindowsCompactCompression _compressor;

        public SmartAlgorithmSelector(ILogger? logger = null)
        {
            _logger = logger;
            _compressor = new WindowsCompactCompression(logger);
        }

        /// <summary>
        /// Benchmark all algorithms and select the best one
        /// </summary>
        /// <param name="targetPath">Path to benchmark (file or directory)</param>
        /// <param name="sampleSize">Number of files to test (for directories)</param>
        /// <param name="prioritizeRatio">If true, prioritize compression ratio over speed</param>
        public async Task<CompactAlgorithm> SelectBestAlgorithmAsync(
            string targetPath,
            int sampleSize = 10,
            bool prioritizeRatio = true)
        {
            _logger?.LogInformation("Starting smart algorithm selection...");

            // Get sample files
            var sampleFiles = GetSampleFiles(targetPath, sampleSize);
            
            if (!sampleFiles.Any())
            {
                _logger?.LogWarning("No files to benchmark, defaulting to LZX");
                return CompactAlgorithm.LZX;
            }

            _logger?.LogInformation($"Testing {sampleFiles.Count} sample files");

            // Create temporary test directory
            string tempTestDir = Path.Combine(Path.GetTempPath(), $"CompactBenchmark_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempTestDir);

            try
            {
                var results = new List<AlgorithmBenchmarkResult>();

                // Test each algorithm
                foreach (var algorithm in new[] { 
                    CompactAlgorithm.XPRESS4K, 
                    CompactAlgorithm.XPRESS8K, 
                    CompactAlgorithm.XPRESS16K, 
                    CompactAlgorithm.LZX 
                })
                {
                    _logger?.LogInformation($"Testing {algorithm}...");
                    var result = await BenchmarkAlgorithmAsync(sampleFiles, algorithm, tempTestDir);
                    results.Add(result);
                    
                    _logger?.LogInformation($"  {algorithm}: {result.CompressionRatio:P2} compression in {result.CompressionTime.TotalSeconds:F2}s");
                }

                // Calculate scores and select best
                foreach (var result in results)
                {
                    // Score combines compression ratio and speed
                    // Higher ratio is better (more compression)
                    // Lower time is better (faster)
                    if (prioritizeRatio)
                    {
                        // 80% weight on ratio, 20% on speed
                        result.Score = (result.CompressionRatio * 0.8) + 
                                      ((1.0 / result.CompressionTime.TotalSeconds) * 0.2);
                    }
                    else
                    {
                        // 50% weight on ratio, 50% on speed
                        result.Score = (result.CompressionRatio * 0.5) + 
                                      ((1.0 / result.CompressionTime.TotalSeconds) * 0.5);
                    }
                }

                var bestResult = results.OrderByDescending(r => r.Score).First();
                
                _logger?.LogInformation($"✅ Selected: {bestResult.Algorithm} (Score: {bestResult.Score:F3})");
                _logger?.LogInformation($"   Compression: {bestResult.CompressionRatio:P2}");
                _logger?.LogInformation($"   Speed: {bestResult.CompressionTime.TotalSeconds:F2}s");

                return bestResult.Algorithm;
            }
            finally
            {
                // Cleanup temp directory
                try
                {
                    if (Directory.Exists(tempTestDir))
                    {
                        Directory.Delete(tempTestDir, recursive: true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Get detailed benchmark results for all algorithms
        /// </summary>
        public async Task<List<AlgorithmBenchmarkResult>> BenchmarkAllAlgorithmsAsync(
            string targetPath,
            int sampleSize = 10)
        {
            var sampleFiles = GetSampleFiles(targetPath, sampleSize);
            string tempTestDir = Path.Combine(Path.GetTempPath(), $"CompactBenchmark_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempTestDir);

            try
            {
                var results = new List<AlgorithmBenchmarkResult>();

                foreach (var algorithm in new[] { 
                    CompactAlgorithm.XPRESS4K, 
                    CompactAlgorithm.XPRESS8K, 
                    CompactAlgorithm.XPRESS16K, 
                    CompactAlgorithm.LZX 
                })
                {
                    var result = await BenchmarkAlgorithmAsync(sampleFiles, algorithm, tempTestDir);
                    results.Add(result);
                }

                return results;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempTestDir))
                    {
                        Directory.Delete(tempTestDir, recursive: true);
                    }
                }
                catch { }
            }
        }

        #region Private Helpers

        private List<string> GetSampleFiles(string path, int sampleSize)
        {
            var files = new List<string>();

            if (File.Exists(path))
            {
                // Single file
                files.Add(path);
            }
            else if (Directory.Exists(path))
            {
                // Directory - get representative sample
                var allFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    .Where(f => new FileInfo(f).Length > 0) // Exclude empty files
                    .ToList();

                if (allFiles.Count <= sampleSize)
                {
                    files = allFiles;
                }
                else
                {
                    // Get stratified sample:
                    // - Mix of large and small files
                    // - Different file types
                    var sorted = allFiles.OrderBy(f => new FileInfo(f).Length).ToList();
                    
                    // Take files from different size ranges
                    int step = sorted.Count / sampleSize;
                    for (int i = 0; i < sampleSize && i * step < sorted.Count; i++)
                    {
                        files.Add(sorted[i * step]);
                    }
                }
            }

            return files;
        }

        private async Task<AlgorithmBenchmarkResult> BenchmarkAlgorithmAsync(
            List<string> sampleFiles,
            CompactAlgorithm algorithm,
            string tempTestDir)
        {
            // Create test subdirectory for this algorithm
            string testDir = Path.Combine(tempTestDir, algorithm.ToString());
            Directory.CreateDirectory(testDir);

            // Copy sample files to test directory
            foreach (var file in sampleFiles)
            {
                string destFile = Path.Combine(testDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            // Measure original size
            long originalSize = sampleFiles.Sum(f => new FileInfo(f).Length);

            // Compress and measure time
            var stopwatch = Stopwatch.StartNew();
            var result = await _compressor.CompressAsync(testDir, algorithm, recursive: true);
            stopwatch.Stop();

            // Measure compressed size
            long compressedSize = Directory.GetFiles(testDir, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);

            return new AlgorithmBenchmarkResult
            {
                Algorithm = algorithm,
                OriginalSize = originalSize,
                CompressedSize = compressedSize,
                CompressionRatio = compressedSize / (double)originalSize,
                CompressionTime = stopwatch.Elapsed
            };
        }

        #endregion
    }
}
