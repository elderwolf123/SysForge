using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RamOptimizer.Compression;
using RamOptimizer.Compression.HyperCompress;
using RamOptimizer.Compression.Transparent;
using RamOptimizer.Compression.VirtualFS;
using RamOptimizer.Logging;

namespace RamOptimizerConsole.CompressionTesting;

/// <summary>
* Comprehensive multi-tier compression testing engine
/// </summary>
public class MultiTierCompressionTester
{
    private readonly ComprehensiveLogger _logger;
    private readonly CompressionDatabase _database;
    private readonly FileSystemScanner _fileScanner;
    
    public MultiTierCompressionTester(ComprehensiveLogger logger)
    {
        _logger = logger;
        _database = new CompressionDatabase();
        _fileScanner = new FileSystemScanner(logger);
    }

    /// <summary>
    * Test all compression tiers on sample files
    /// </summary>
    public async Task<CompressionTestReport> RunComprehensiveCompressionTestAsync(string testDirectory = null)
    {
        _logger.LogInfo("Starting comprehensive compression test");
        
        var report = new CompressionTestReport
        {
            TestStartTime = DateTime.Now,
            TestResults = new Dictionary<string, TierTestResult>(),
            OverallPerformance = new PerformanceMetrics()
        };

        try
        {
            // Create test directory if not provided
            if (string.IsNullOrEmpty(testDirectory))
            {
                testDirectory = CreateTestDirectory();
            }

            // Generate test files
            var testFiles = await GenerateTestFilesAsync(testDirectory);
            _logger.LogInfo($"Generated {testFiles.Count} test files");

            // Test each compression tier
            await TestStandardCompressionAsync(testFiles, report);
            await TestHyperCompressAsync(testFiles, report);
            await TestTransparentCompressionAsync(testFiles, report);
            await TestVirtualFSAsync(testFiles, report);

            // Analyze results
            AnalyzeTestResults(report);

            report.TestEndTime = DateTime.Now;
            report.TestDuration = report.TestEndTime - report.TestStartTime;

            _logger.LogInfo($"Comprehensive compression test completed in {report.TestDuration.TotalSeconds:F2} seconds");
            
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during compression testing: {ex.Message}");
            throw;
        }
        finally
        {
            // Clean up test files
            if (!string.IsNullOrEmpty(testDirectory))
            {
                CleanupTestDirectory(testDirectory);
            }
        }
    }

    /// <summary>
    * Test Standard compression tier
    /// </summary>
    private async Task TestStandardCompressionAsync(List<string> testFiles, CompressionTestReport report)
    {
        _logger.LogInfo("Testing Standard compression tier");
        
        var result = new TierTestResult
        {
            TierName = "Standard",
            Algorithm = "Default",
            TestFiles = new List<FileTestResult>()
        };

        var compressionEngine = new AdvancedFileCompressionSystem();
        
        foreach (var file in testFiles)
        {
            try
            {
                var fileResult = await TestFileCompressionAsync(file, compressionEngine, "Standard");
                result.TestFiles.Add(fileResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error testing Standard compression on {file}: {ex.Message}");
                result.TestFiles.Add(new FileTestResult
                {
                    FilePath = file,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Calculate tier statistics
        CalculateTierStatistics(result);
        report.TestResults["Standard"] = result;
    }

    /// <summary>
    * Test HyperCompress tier
    /// </summary>
    private async Task TestHyperCompressAsync(List<string> testFiles, CompressionTestReport report)
    {
        _logger.LogInfo("Testing HyperCompress tier");
        
        var result = new TierTestResult
        {
            TierName = "HyperCompress",
            Algorithm = "Advanced",
            TestFiles = new List<FileTestResult>()
        };

        var compressionEngine = new AdvancedFileCompressionSystem();
        
        foreach (var file in testFiles)
        {
            try
            {
                var fileResult = await TestFileCompressionAsync(file, compressionEngine, "HyperCompress");
                result.TestFiles.Add(fileResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error testing HyperCompress on {file}: {ex.Message}");
                result.TestFiles.Add(new FileTestResult
                {
                    FilePath = file,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Calculate tier statistics
        CalculateTierStatistics(result);
        report.TestResults["HyperCompress"] = result;
    }

    /// <summary>
    * Test Transparent compression tier
    /// </summary>
    private async Task TestTransparentCompressionAsync(List<string> testFiles, CompressionTestReport report)
    {
        _logger.LogInfo("Testing Transparent compression tier");
        
        var result = new TierTestResult
        {
            TierName = "Transparent",
            Algorithm = "Real-time",
            TestFiles = new List<FileTestResult>()
        };

        var compressionEngine = new AdvancedFileCompressionSystem();
        
        foreach (var file in testFiles)
        {
            try
            {
                var fileResult = await TestFileCompressionAsync(file, compressionEngine, "Transparent");
                result.TestFiles.Add(fileResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error testing Transparent compression on {file}: {ex.Message}");
                result.TestFiles.Add(new FileTestResult
                {
                    FilePath = file,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Calculate tier statistics
        CalculateTierStatistics(result);
        report.TestResults["Transparent"] = result;
    }

    /// <summary>
    * Test VirtualFS compression tier
    /// </summary>
    private async Task TestVirtualFSAsync(List<string> testFiles, CompressionTestReport report)
    {
        _logger.LogInfo("Testing VirtualFS compression tier");
        
        var result = new TierTestResult
        {
            TierName = "VirtualFS",
            Algorithm = "Virtual File System",
            TestFiles = new List<FileTestResult>()
        };

        var compressionEngine = new AdvancedFileCompressionSystem();
        
        foreach (var file in testFiles)
        {
            try
            {
                var fileResult = await TestFileCompressionAsync(file, compressionEngine, "VirtualFS");
                result.TestFiles.Add(fileResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error testing VirtualFS compression on {file}: {ex.Message}");
                result.TestFiles.Add(new FileTestResult
                {
                    FilePath = file,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Calculate tier statistics
        CalculateTierStatistics(result);
        report.TestResults["VirtualFS"] = result;
    }

    /// <summary>
    * Test individual file compression
    /// </summary>
    private async Task<FileTestResult> TestFileCompressionAsync(string filePath, AdvancedFileCompressionSystem engine, string tier)
    {
        var fileInfo = new FileInfo(filePath);
        var result = new FileTestResult
        {
            FilePath = filePath,
            OriginalSize = fileInfo.Length,
            CompressionTier = tier,
            StartTime = DateTime.Now
        };

        try
        {
            // Test compression
            var compressionResult = await engine.CompressFileAsync(
                filePath,
                BackupPolicy.None,
                specificAlgorithm: null,
                specificLevel: null,
                allowMediaCompression: true,
                minSavingsThreshold: 0.1
            );

            result.Success = compressionResult.Success;
            result.CompressedSize = compressionResult.CompressedSize;
            result.CompressionRatio = compressionResult.CompressionRatio;
            result.CompressionTime = DateTime.Now - result.StartTime;
            result.AlgorithmUsed = compressionResult.Algorithm;
            result.ErrorMessage = compressionResult.ErrorMessage;

            // Test decompression if compression was successful
            if (compressionResult.Success)
            {
                await TestDecompressionAsync(filePath, engine, result);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        result.EndTime = DateTime.Now;
        return result;
    }

    /// <summary>
    * Test decompression
    /// </summary>
    private async Task TestDecompressionAsync(string originalFilePath, AdvancedFileCompressionSystem engine, FileTestResult compressionResult)
    {
        try
        {
            var decompressStartTime = DateTime.Now;
            
            // Decompress the file
            var decompressResult = await engine.DecompressFileAsync(originalFilePath);
            
            compressionResult.DecompressionSuccess = decompressResult.Success;
            compressionResult.DecompressionTime = DateTime.Now - decompressStartTime;
            compressionResult.DecompressionErrorMessage = decompressResult.ErrorMessage;

            // Verify data integrity
            if (decompressResult.Success)
            {
                await VerifyDataIntegrityAsync(originalFilePath, decompressResult.OutputPath, compressionResult);
            }
        }
        catch (Exception ex)
        {
            compressionResult.DecompressionSuccess = false;
            compressionResult.DecompressionErrorMessage = ex.Message;
        }
    }

    /// <summary>
    * Verify data integrity after decompression
    /// </summary>
    private async Task VerifyDataIntegrityAsync(string originalFile, string decompressedFile, FileTestResult result)
    {
        try
        {
            var originalBytes = await File.ReadAllBytesAsync(originalFile);
            var decompressedBytes = await File.ReadAllBytesAsync(decompressedFile);

            result.DataIntegrityVerified = originalBytes.SequenceEqual(decompressedBytes);
            result.DataIntegrityCheckTime = DateTime.Now;

            if (!result.DataIntegrityVerified)
            {
                result.DataIntegrityErrorMessage = "Data integrity check failed: files do not match";
            }
        }
        catch (Exception ex)
        {
            result.DataIntegrityVerified = false;
            result.DataIntegrityErrorMessage = $"Data integrity check failed: {ex.Message}";
        }
    }

    /// <summary>
    * Calculate tier statistics
    /// </summary>
    private void CalculateTierStatistics(TierTestResult tierResult)
    {
        var successfulTests = tierResult.TestFiles.Where(f => f.Success).ToList();
        
        if (successfulTests.Any())
        {
            tierResult.FilesTested = tierResult.TestFiles.Count;
            tierResult.FilesSuccessful = successfulTests.Count;
            tierResult.FilesFailed = tierResult.TestFiles.Count - successfulTests.Count;
            tierResult.SuccessRate = (double)successfulTests.Count / tierResult.TestFiles.Count;
            
            tierResult.TotalOriginalSize = successfulTests.Sum(f => f.OriginalSize);
            tierResult.TotalCompressedSize = successfulTests.Sum(f => f.CompressedSize);
            tierResult.TotalSpaceSaved = tierResult.TotalOriginalSize - tierResult.TotalCompressedSize;
            tierResult.AverageCompressionRatio = successfulTests.Average(f => f.CompressionRatio);
            tierResult.AverageCompressionTime = successfulTests.Average(f => f.CompressionTime);
            tierResult.AverageDecompressionTime = successfulTests.Average(f => f.DecompressionTime);
            
            tierResult.BestCompressionRatio = successfulTests.Max(f => f.CompressionRatio);
            tierResult.WorstCompressionRatio = successfulTests.Min(f => f.CompressionRatio);
        }
    }

    /// <summary>
    * Analyze test results
    /// </summary>
    private void AnalyzeTestResults(CompressionTestReport report)
    {
        // Find best performing tier
        var bestTier = report.TestResults.Values
            .OrderByDescending(t => t.AverageCompressionRatio)
            .FirstOrDefault();
        
        if (bestTier != null)
        {
            report.BestPerformingTier = bestTier.TierName;
            report.BestCompressionRatio = bestTier.AverageCompressionRatio;
        }

        // Calculate overall performance metrics
        var allSuccessfulTests = report.TestResults.Values
            .SelectMany(t => t.TestFiles)
            .Where(f => f.Success)
            .ToList();

        if (allSuccessfulTests.Any())
        {
            report.OverallPerformance.TotalFilesTested = allSuccessfulTests.Count;
            report.OverallPerformance.TotalOriginalSize = allSuccessfulTests.Sum(f => f.OriginalSize);
            report.OverallPerformance.TotalCompressedSize = allSuccessfulTests.Sum(f => f.CompressedSize);
            report.OverallPerformance.TotalSpaceSaved = report.OverallPerformance.TotalOriginalSize - report.OverallPerformance.TotalCompressedSize;
            report.OverallPerformance.AverageCompressionRatio = allSuccessfulTests.Average(f => f.CompressionRatio);
            report.OverallPerformance.AverageCompressionTime = allSuccessfulTests.Average(f => f.CompressionTime);
            report.OverallPerformance.AverageDecompressionTime = allSuccessfulTests.Average(f => f.DecompressionTime);
            report.OverallPerformance.SuccessRate = (double)allSuccessfulTests.Count / report.TestResults.Values.Sum(t => t.TestFiles.Count);
        }

        // Generate recommendations
        GenerateRecommendations(report);
    }

    /// <summary>
    * Generate recommendations based on test results
    /// </summary>
    private void GenerateRecommendations(CompressionTestReport report)
    {
        report.Recommendations = new List<string>();

        // Overall recommendations
        if (report.OverallPerformance.AverageCompressionRatio > 0.7)
        {
            report.Recommendations.Add("Excellent compression potential detected across all file types");
        }
        else if (report.OverallPerformance.AverageCompressionRatio > 0.5)
        {
            report.Recommendations.Add("Good compression potential detected");
        }
        else
        {
            report.Recommendations.Add("Limited compression potential detected");
        }

        // Tier-specific recommendations
        foreach (var tier in report.TestResults.Values)
        {
            if (tier.SuccessRate > 0.9)
            {
                report.Recommendations.Add($"{tier.TierName} tier shows excellent reliability ({tier.SuccessRate:P0})");
            }
            else if (tier.SuccessRate > 0.7)
            {
                report.Recommendations.Add($"{tier.TierName} tier shows good reliability ({tier.SuccessRate:P0})");
            }
            else
            {
                report.Recommendations.Add($"{tier.TierName} tier shows reliability issues ({tier.SuccessRate:P0})");
            }
        }

        // Performance recommendations
        if (report.OverallPerformance.AverageCompressionTime.TotalSeconds > 10)
        {
            report.Recommendations.Add("Compression performance may be slow for large files");
        }

        if (report.OverallPerformance.AverageDecompressionTime.TotalSeconds > 5)
        {
            report.Recommendations.Add("Decompression performance may be slow for large files");
        }
    }

    /// <summary>
    * Create test directory
    /// </summary>
    private string CreateTestDirectory()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "RamOptimizer_Compression_Test");
        Directory.CreateDirectory(testDir);
        return testDir;
    }

    /// <summary>
    * Generate test files
    /// </summary>
    private async Task<List<string>> GenerateTestFilesAsync(string testDirectory)
    {
        var testFiles = new List<string>();
        
        // Generate various file types for testing
        var fileGenerators = new List<Func<string, Task<string>>>
        {
            GenerateTextFileAsync,
            GenerateJsonFileAsync,
            GenerateXmlFileAsync,
            GenerateCsvFileAsync,
            GenerateBinaryFileAsync
        };

        for (int i = 0; i < 20; i++) // Generate 20 test files
        {
            var generator = fileGenerators[i % fileGenerators.Count];
            var testFile = await generator(Path.Combine(testDirectory, $"test_{i + 1}"));
            testFiles.Add(testFile);
        }

        return testFiles;
    }

    /// <summary>
    * Generate text test file
    /// </summary>
    private async Task<string> GenerateTextFileAsync(string baseName)
    {
        var filePath = baseName + ".txt";
        var content = new string('A', 1024 * 10); // 10KB of text
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    /// <summary>
    * Generate JSON test file
    /// </summary>
    private async Task<string> GenerateJsonFileAsync(string baseName)
    {
        var filePath = baseName + ".json";
        var content = @"{
    ""name"": ""test"",
    ""value"": 12345,
    ""array"": [1, 2, 3, 4, 5],
    ""nested"": {
        ""inner"": ""data""
    }
}";
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    /// <summary>
    * Generate XML test file
    /// </summary>
    private async Task<string> GenerateXmlFileAsync(string baseName)
    {
        var filePath = baseName + ".xml";
        var content = @"<root>
    <item id=""1"">Test data</item>
    <item id=""2"">More data</item>
    <item id=""3"">Even more data</item>
</root>";
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    /// <summary>
    * Generate CSV test file
    /// </summary>
    private async Task<string> GenerateCsvFileAsync(string baseName)
    {
        var filePath = baseName + ".csv";
        var content = "Name,Age,City\nJohn,25,New York\nJane,30,Los Angeles\nBob,35,Chicago";
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    /// <summary>
    * Generate binary test file
    /// </summary>
    private async Task<string> GenerateBinaryFileAsync(string baseName)
    {
        var filePath = baseName + ".bin";
        var randomData = new byte[1024 * 5]; // 5KB of random data
        new Random().NextBytes(randomData);
        await File.WriteAllBytesAsync(filePath, randomData);
        return filePath;
    }

    /// <summary>
    * Clean up test directory
    /// </summary>
    private void CleanupTestDirectory(string testDirectory)
    {
        try
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cleaning up test directory: {ex.Message}");
        }
    }
}

/// <summary>
* Compression test report
/// </summary>
public class CompressionTestReport
{
    public DateTime TestStartTime { get; set; }
    public DateTime TestEndTime { get; set; }
    public TimeSpan TestDuration { get; set; }
    public Dictionary<string, TierTestResult> TestResults { get; set; }
    public PerformanceMetrics OverallPerformance { get; set; }
    public string BestPerformingTier { get; set; }
    public double BestCompressionRatio { get; set; }
    public List<string> Recommendations { get; set; }
}

/// <summary>
* Tier test result
/// </summary>
public class TierTestResult
{
    public string TierName { get; set; }
    public string Algorithm { get; set; }
    public List<FileTestResult> TestFiles { get; set; }
    public int FilesTested { get; set; }
    public int FilesSuccessful { get; set; }
    public int FilesFailed { get; set; }
    public double SuccessRate { get; set; }
    public long TotalOriginalSize { get; set; }
    public long TotalCompressedSize { get; set; }
    public long TotalSpaceSaved { get; set; }
    public double AverageCompressionRatio { get; set; }
    public TimeSpan AverageCompressionTime { get; set; }
    public TimeSpan AverageDecompressionTime { get; set; }
    public double BestCompressionRatio { get; set; }
    public double WorstCompressionRatio { get; set; }
}

/// <summary>
* Individual file test result
/// </summary>
public class FileTestResult
{
    public string FilePath { get; set; }
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public string CompressionTier { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public string AlgorithmUsed { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan CompressionTime { get; set; }
    public bool DecompressionSuccess { get; set; }
    public TimeSpan DecompressionTime { get; set; }
    public string DecompressionErrorMessage { get; set; }
    public bool DataIntegrityVerified { get; set; }
    public DateTime? DataIntegrityCheckTime { get; set; }
    public string DataIntegrityErrorMessage { get; set; }
}

/// <summary>
* Performance metrics
/// </summary>
public class PerformanceMetrics
{
    public int TotalFilesTested { get; set; }
    public long TotalOriginalSize { get; set; }
    public long TotalCompressedSize { get; set; }
    public long TotalSpaceSaved { get; set; }
    public double AverageCompressionRatio { get; set; }
    public TimeSpan AverageCompressionTime { get; set; }
    public TimeSpan AverageDecompressionTime { get; set; }
    public double SuccessRate { get; set; }
}