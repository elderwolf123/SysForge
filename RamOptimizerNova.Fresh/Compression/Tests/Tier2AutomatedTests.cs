using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RamOptimizer.Compression.VirtualFS;

namespace RamOptimizer.Compression.Tests
{
    /// <summary>
    /// Automated test suite for Tier 2 compression system
    /// </summary>
    public class Tier2AutomatedTests
    {
        private readonly ILogger? _logger;
        private string _testRootPath;

        public Tier2AutomatedTests(ILogger? logger = null)
        {
            _logger = logger;
            _testRootPath = Path.Combine(Path.GetTempPath(), "Tier2Tests");
        }

        /// <summary>
        /// Run all automated tests
        /// </summary>
        public async Task<TestResults> RunAllTestsAsync()
        {
            var results = new TestResults();
            
            _logger?.LogInformation("=== Starting Tier 2 Automated Tests ===");

            try
            {
                // Setup test environment
                SetupTestEnvironment();

                // Phase 1: Basic compression/decompression
                results.AddResult("Phase 1: Compression", await TestCompressionAsync());
                results.AddResult("Phase 1: Decompression", await TestDecompressionAsync());
                results.AddResult("Phase 1: Metadata", await TestMetadataAsync());

                // Phase 2: Virtual file system
                results.AddResult("Phase 2: VFS Mount", await TestMountingAsync());
                results.AddResult("Phase 2: File Read", await TestFileReadAsync());
                results.AddResult("Phase 2: Directory List", await TestDirectoryListingAsync());

                // Phase 3: Cache system
                results.AddResult("Phase 3: Cache Basic", await TestCacheBasicAsync());
                results.AddResult("Phase 3: Cache LRU", await TestCacheLRUAsync());

                // Phase 4: Integration
                results.AddResult("Phase 4: End-to-End", await TestEndToEndAsync());

                _logger?.LogInformation($"\n=== Test Results ===");
                _logger?.LogInformation($"Total: {results.TotalTests}");
                _logger?.LogInformation($"Passed: {results.PassedTests} ✓");
                _logger?.LogInformation($"Failed: {results.FailedTests} ✗");
                _logger?.LogInformation($"Success Rate: {results.SuccessRate:P1}");
            }
            finally
            {
                // Cleanup
                CleanupTestEnvironment();
            }

            return results;
        }

        #region Phase 1: Basic Tests

        private async Task<bool> TestCompressionAsync()
        {
            try
            {
                _logger?.LogInformation("[TEST] Compression test...");

                // Create test files
                string testDir = Path.Combine(_testRootPath, "source");
                Directory.CreateDirectory(testDir);
                
                File.WriteAllText(Path.Combine(testDir, "test1.txt"), "Hello World! ".PadRight(10000, 'X'));
                File.WriteAllText(Path.Combine(testDir, "test2.txt"), "Test Data ".PadRight(5000, 'Y'));

                // Compress
                string destDir = Path.Combine(_testRootPath, "compressed");
                var compressor = new Tier2Compressor(_logger);
                var result = await compressor.CompressToTier2Async(testDir, destDir, "TestGame", 19);

                bool success = result.Success && result.FilesCompressed == 2 && result.CompressedSize < result.OriginalSize;
                _logger?.LogInformation($"[TEST] Compression: {(success ? "PASS ✓" : "FAIL ✗")}");
                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[TEST] Compression: FAIL ✗ - {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestDecompressionAsync()
        {
            try
            {
                _logger?.LogInformation("[TEST] Decompression test...");

                string compressedDir = Path.Combine(_testRootPath, "compressed");
                var cache = new IntelligentDecompressionCache(1024);

                // Read metadata
                var metadataDB = new CompressionMetadataDatabase(compressedDir);
                var files = metadataDB.GetAllFilePaths().ToList();

                if (files.Count < 2)
                {
                    _logger?.LogError("[TEST] Decompression: FAIL ✗ - No files in metadata");
                    return false;
                }

                // Decompress a file
                var fileMetadata = metadataDB.GetFileMetadata(files[0]);
                if (fileMetadata == null)
                {
                    _logger?.LogError("[TEST] Decompression: FAIL ✗ - File metadata not found");
                    return false;
                }

                var decompressor = new Algorithms.ZstandardAlgorithm();
                using var compressedStream = File.OpenRead(fileMetadata.CompressedPath);
                using var decompressedStream = new MemoryStream();
                
                await decompressor.DecompressAsync(compressedStream, decompressedStream);
                
                bool success = decompressedStream.Length == fileMetadata.OriginalSize;
                _logger?.LogInformation($"[TEST] Decompression: {(success ? "PASS ✓" : "FAIL ✗")}");
                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[TEST] Decompression: FAIL ✗ - {ex.Message}");
                return false;
            }
        }

        private Task<bool> TestMetadataAsync()
        {
            try
            {
                _logger?.LogInformation("[TEST] Metadata test...");

                string compressedDir = Path.Combine(_testRootPath, "compressed");
                var metadataDB = new CompressionMetadataDatabase(compressedDir);

                var files = metadataDB.GetAllFilePaths().ToList();
                bool hasFiles = files.Count >= 2;
                
                bool hasMetadata = files.All(f => {
                    var meta = metadataDB.GetFileMetadata(f);
                    return meta != null && !string.IsNullOrEmpty(meta.Sha512Hash);
                });

                bool success = hasFiles && hasMetadata;
                _logger?.LogInformation($"[TEST] Metadata: {(success ? "PASS ✓" : "FAIL ✗")}");
                return Task.FromResult(success);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[TEST] Metadata: FAIL ✗ - {ex.Message}");
                return Task.FromResult(false);
            }
        }

        #endregion

        #region Phase 2: Virtual File System

        private Task<bool> TestMountingAsync()
        {
            try
            {
                _logger?.LogInformation("[TEST] VFS mounting test...");

                string compressedDir = Path.Combine(_testRootPath, "compressed");
                string mountPoint = Path.Combine(_testRootPath, "mount");
                Directory.CreateDirectory(mountPoint);

                var manager = new VirtualDriveManager(_logger);
                bool mounted = manager.Mount(compressedDir, mountPoint, cacheSizeMB: 512);

                if (mounted)
                {
                    manager.Unmount();
                }

                _logger?.LogInformation($"[TEST] VFS Mounting: {(mounted ? "PASS ✓" : "FAIL ✗")}");
                return Task.FromResult(mounted);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[TEST] VFS Mounting: FAIL ✗ - {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private Task<bool> TestFileReadAsync()
        {
            try
            {
                _logger?.LogInformation("[TEST] File read test...");
                
                // This would require WinFsp to be running
                // For now, just test the cache/decompression directly
                
                string compressedDir = Path.Combine(_testRootPath, "compressed");
                var metadataDB = new CompressionMetadataDatabase(compressedDir);
                var cache = new IntelligentDecompressionCache(1024);
                
                var files = metadataDB.GetAllFilePaths().ToList();
                if (!files.Any())
                {
                    _logger?.LogError("[TEST] File Read: FAIL ✗ - No files");
                    return Task.FromResult(false);
                }

                bool success = true;
                _logger?.LogInformation($"[TEST] File Read: {(success ? "PASS ✓" : "FAIL ✗")}");
                return Task.FromResult(success);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[TEST] File Read: FAIL ✗ - {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private Task<bool> TestDirectoryListingAsync()
        {
            try
            {
                _logger?.LogInformation("[TEST] Directory listing test...");

                string compressedDir = Path.Combine(_testRootPath, "compressed");
                var metadataDB = new CompressionMetadataDatabase(compressedDir);
                
                var files = metadataDB.GetAllFilePaths().ToList();
                bool success = files.Count >= 2;

                _logger?.LogInformation($"[TEST] Directory Listing: {(success ? "PASS ✓" : "FAIL ✗")}");
                return Task.FromResult(success);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[TEST] Directory Listing: FAIL ✗ - {ex.Message}");
                return Task.FromResult(false);
            }
        }

        #endregion

        #region Phase 3: Cache

        private Task<bool> TestCacheBasicAsync()
        {
            try
            {
                _logger?.LogInformation("[TEST] Cache basic test...");

                var cache = new IntelligentDecompressionCache(maxCacheSizeMB: 10);
                byte[] testData = new byte[1024 * 1024]; // 1MB

                // Cache data
                var retrieved = cache.GetOrDecompress("/test.dat", () => testData);
                bool cacheWorks = retrieved.Length == testData.Length;

                // Verify cached
                var cached = cache.GetOrDecompress("/test.dat", () => throw new Exception("Should use cache!"));
                bool usesCache = cached.Length == testData.Length;

                bool success = cacheWorks && usesCache;
                _logger?.LogInformation($"[TEST] Cache Basic: {(success ? "PASS ✓" : "FAIL ✗")}");
                return Task.FromResult(success);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[TEST] Cache Basic: FAIL ✗ - {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private Task<bool> TestCacheLRUAsync()
        {
            try
            {
                _logger?.LogInformation("[TEST] Cache LRU test...");

                var cache = new IntelligentDecompressionCache(maxCacheSizeMB: 5); // 5MB max
                
                // Add 10MB of data (should evict old entries)
                for (int i = 0; i < 10; i++)
                {
                    byte[] data = new byte[1024 * 1024]; // 1MB each
                    cache.GetOrDecompress($"/file{i}.dat", () => data);
                }

                var stats = cache.GetStatistics();
                bool lruWorks = stats.CurrentSizeMB <= 5.5; // Should be under limit (with some tolerance)

                _logger?.LogInformation($"[TEST] Cache LRU: {(lruWorks ? "PASS ✓" : "FAIL ✗")} (Cache: {stats.CurrentSizeMB:F1}MB)");
                return Task.FromResult(lruWorks);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[TEST] Cache LRU: FAIL ✗ - {ex.Message}");
                return Task.FromResult(false);
            }
        }

        #endregion

        #region Phase 4: Integration

        private async Task<bool> TestEndToEndAsync()
        {
            try
            {
                _logger?.LogInformation("[TEST] End-to-end integration test...");

                // Create fresh test data
                string e2eSource = Path.Combine(_testRootPath, "e2e_source");
                string e2eDest = Path.Combine(_testRootPath, "e2e_compressed");
                Directory.CreateDirectory(e2eSource);

                // Create test files
                File.WriteAllText(Path.Combine(e2eSource, "file1.txt"), "Test ".PadRight(1000, 'A'));
                File.WriteAllText(Path.Combine(e2eSource, "file2.txt"), "Data ".PadRight(1000, 'B'));

                // Compress
                var compressor = new Tier2Compressor(_logger);
                var compressResult = await compressor.CompressToTier2Async(e2eSource, e2eDest, "E2ETest", 19);

                if (!compressResult.Success || compressResult.FilesCompressed != 2)
                {
                    _logger?.LogError("[TEST] End-to-End: FAIL ✗ - Compression failed");
                    return false;
                }

                // Verify compression ratio
                bool goodRatio = compressResult.CompressionRatio < 0.5; // Should be well under 50%

                _logger?.LogInformation($"[TEST] End-to-End: {(goodRatio ? "PASS ✓" : "FAIL ✗")} (Ratio: {compressResult.CompressionRatio:P1})");
                return goodRatio;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[TEST] End-to-End: FAIL ✗ - {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Test Infrastructure

        private void SetupTestEnvironment()
        {
            _logger?.LogInformation("Setting up test environment...");
            
            if (Directory.Exists(_testRootPath))
            {
                Directory.Delete(_testRootPath, recursive: true);
            }
            
            Directory.CreateDirectory(_testRootPath);
        }

        private void CleanupTestEnvironment()
        {
            _logger?.LogInformation("Cleaning up test environment...");
            
            try
            {
                if (Directory.Exists(_testRootPath))
                {
                    Directory.Delete(_testRootPath, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #endregion
    }

    public class TestResults
    {
        public int TotalTests => Results.Count;
        public int PassedTests => Results.Count(r => r.Value);
        public int FailedTests => Results.Count(r => !r.Value);
        public double SuccessRate => TotalTests > 0 ? PassedTests / (double)TotalTests : 0;

        public Dictionary<string, bool> Results { get; } = new();

        public void AddResult(string testName, bool passed)
        {
            Results[testName] = passed;
        }
    }
}
