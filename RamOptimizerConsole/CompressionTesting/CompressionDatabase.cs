using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite;

namespace RamOptimizerConsole.CompressionTesting;

/// <summary>
* SQLite database for storing compression performance data
/// </summary>
public class CompressionDatabase
{
    private readonly string _databasePath;
    private SQLiteAsyncConnection _connection;

    public CompressionDatabase(string databasePath = null)
    {
        _databasePath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RamOptimizer",
            "compression_performance.db"
        );

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_databasePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    * Initialize database connection and create tables
    /// </summary>
    public async Task InitializeAsync()
    {
        _connection = new SQLiteAsyncConnection(_databasePath);
        
        await _connection.CreateTableAsync<CompressionTestRecord>();
        await _connection.CreateTableAsync<FileTestRecord>();
        await _connection.CreateTableAsync<PerformanceMetricRecord>();
        await _connection.CreateTableAsync<SystemInfoRecord>();
        await _connection.CreateTableAsync<RecommendationRecord>();
    }

    /// <summary>
    * Save compression test results
    /// </summary>
    public async Task SaveCompressionTestAsync(CompressionTestReport report)
    {
        if (_connection == null)
        {
            await InitializeAsync();
        }

        using (var transaction = await _connection.BeginTransactionAsync())
        {
            try
            {
                // Save main test record
                var testRecord = new CompressionTestRecord
                {
                    TestId = Guid.NewGuid().ToString(),
                    TestStartTime = report.TestStartTime,
                    TestEndTime = report.TestEndTime,
                    TestDuration = report.TestDuration.TotalMilliseconds,
                    BestPerformingTier = report.BestPerformingTier,
                    BestCompressionRatio = report.BestCompressionRatio,
                    TotalFilesTested = report.OverallPerformance.TotalFilesTested,
                    TotalOriginalSize = report.OverallPerformance.TotalOriginalSize,
                    TotalCompressedSize = report.OverallPerformance.TotalCompressedSize,
                    TotalSpaceSaved = report.OverallPerformance.TotalSpaceSaved,
                    AverageCompressionRatio = report.OverallPerformance.AverageCompressionRatio,
                    AverageCompressionTime = report.OverallPerformance.AverageCompressionTime.TotalMilliseconds,
                    AverageDecompressionTime = report.OverallPerformance.AverageDecompressionTime.TotalMilliseconds,
                    SuccessRate = report.OverallPerformance.SuccessRate,
                    SystemInfo = await GetSystemInfoAsync(),
                    CreatedAt = DateTime.Now
                };

                await _connection.InsertAsync(testRecord);

                // Save tier results
                foreach (var tierResult in report.TestResults.Values)
                {
                    foreach (var fileResult in tierResult.TestFiles)
                    {
                        var fileRecord = new FileTestRecord
                        {
                            TestId = testRecord.TestId,
                            FilePath = fileResult.FilePath,
                            FileName = Path.GetFileName(fileResult.FilePath),
                            Extension = Path.GetExtension(fileResult.FilePath),
                            OriginalSize = fileResult.OriginalSize,
                            CompressedSize = fileResult.CompressedSize,
                            CompressionRatio = fileResult.CompressionRatio,
                            CompressionTier = fileResult.CompressionTier,
                            Success = fileResult.Success,
                            ErrorMessage = fileResult.ErrorMessage,
                            AlgorithmUsed = fileResult.AlgorithmUsed,
                            CompressionTime = fileResult.CompressionTime.TotalMilliseconds,
                            DecompressionSuccess = fileResult.DecompressionSuccess,
                            DecompressionTime = fileResult.DecompressionTime.TotalMilliseconds,
                            DataIntegrityVerified = fileResult.DataIntegrityVerified,
                            DataIntegrityCheckTime = fileResult.DataIntegrityCheckTime,
                            DataIntegrityErrorMessage = fileResult.DataIntegrityErrorMessage,
                            TestedAt = fileResult.StartTime
                        };

                        await _connection.InsertAsync(fileRecord);
                    }
                }

                // Save performance metrics
                var perfRecord = new PerformanceMetricRecord
                {
                    TestId = testRecord.TestId,
                    TotalFilesTested = report.OverallPerformance.TotalFilesTested,
                    TotalOriginalSize = report.OverallPerformance.TotalOriginalSize,
                    TotalCompressedSize = report.OverallPerformance.TotalCompressedSize,
                    TotalSpaceSaved = report.OverallPerformance.TotalSpaceSaved,
                    AverageCompressionRatio = report.OverallPerformance.AverageCompressionRatio,
                    AverageCompressionTime = report.OverallPerformance.AverageCompressionTime.TotalMilliseconds,
                    AverageDecompressionTime = report.OverallPerformance.AverageDecompressionTime.TotalMilliseconds,
                    SuccessRate = report.OverallPerformance.SuccessRate,
                    CreatedAt = DateTime.Now
                };

                await _connection.InsertAsync(perfRecord);

                // Save recommendations
                foreach (var recommendation in report.Recommendations)
                {
                    var recRecord = new RecommendationRecord
                    {
                        TestId = testRecord.TestId,
                        Recommendation = recommendation,
                        CreatedAt = DateTime.Now
                    };

                    await _connection.InsertAsync(recRecord);
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    /// <summary>
    * Get compression test history
    /// </summary>
    public async Task<List<CompressionTestRecord>> GetTestHistoryAsync(int limit = 100)
    {
        if (_connection == null)
        {
            await InitializeAsync();
        }

        return await _connection.Table<CompressionTestRecord>()
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    * Get test details by ID
    /// </summary>
    public async Task<CompressionTestDetails> GetTestDetailsAsync(string testId)
    {
        if (_connection == null)
        {
            await InitializeAsync();
        }

        var testRecord = await _connection.Table<CompressionTestRecord>()
            .FirstOrDefaultAsync(t => t.TestId == testId);

        if (testRecord == null)
        {
            return null;
        }

        var fileRecords = await _connection.Table<FileTestRecord>()
            .Where(f => f.TestId == testId)
            .ToListAsync();

        var perfRecord = await _connection.Table<PerformanceMetricRecord>()
            .FirstOrDefaultAsync(p => p.TestId == testId);

        var recommendations = await _connection.Table<RecommendationRecord>()
            .Where(r => r.TestId == testId)
            .Select(r => r.Recommendation)
            .ToListAsync();

        return new CompressionTestDetails
        {
            TestRecord = testRecord,
            FileRecords = fileRecords,
            PerformanceRecord = perfRecord,
            Recommendations = recommendations
        };
    }

    /// <summary>
    * Get performance statistics
    /// </summary>
    public async Task<PerformanceStatistics> GetPerformanceStatisticsAsync()
    {
        if (_connection == null)
        {
            await InitializeAsync();
        }

        var tests = await _connection.Table<CompressionTestRecord>().ToListAsync();
        
        if (!tests.Any())
        {
            return new PerformanceStatistics();
        }

        return new PerformanceStatistics
        {
            TotalTests = tests.Count,
            AverageCompressionRatio = tests.Average(t => t.AverageCompressionRatio),
            AverageCompressionTime = TimeSpan.FromMilliseconds(tests.Average(t => t.AverageCompressionTime)),
            AverageDecompressionTime = TimeSpan.FromMilliseconds(tests.Average(t => t.AverageDecompressionTime)),
            AverageSuccessRate = tests.Average(t => t.SuccessRate),
            TotalSpaceSaved = tests.Sum(t => t.TotalSpaceSaved),
            BestCompressionRatio = tests.Max(t => t.BestCompressionRatio),
            LatestTestDate = tests.Max(t => t.CreatedAt)
        };
    }

    /// <summary>
    * Get tier performance comparison
    /// </summary>
    public async Task<Dictionary<string, TierPerformance>> GetTierPerformanceAsync()
    {
        if (_connection == null)
        {
            await InitializeAsync();
        }

        var tierPerformance = new Dictionary<string, TierPerformance>();
        
        var fileRecords = await _connection.Table<FileTestRecord>().ToListAsync();
        
        var tiers = fileRecords.Select(f => f.CompressionTier).Distinct();
        
        foreach (var tier in tiers)
        {
            var tierFiles = fileRecords.Where(f => f.CompressionTier == tier).ToList();
            
            if (tierFiles.Any())
            {
                tierPerformance[tier] = new TierPerformance
                {
                    TierName = tier,
                    FilesTested = tierFiles.Count,
                    FilesSuccessful = tierFiles.Count(f => f.Success),
                    AverageCompressionRatio = tierFiles.Average(f => f.CompressionRatio),
                    AverageCompressionTime = TimeSpan.FromMilliseconds(tierFiles.Average(f => f.CompressionTime)),
                    TotalSpaceSaved = tierFiles.Sum(f => f.OriginalSize - f.CompressedSize),
                    SuccessRate = (double)tierFiles.Count(f => f.Success) / tierFiles.Count
                };
            }
        }

        return tierPerformance;
    }

    /// <summary>
    * Get file type performance
    /// </summary>
    public async Task<Dictionary<string, FileTypePerformance>> GetFileTypePerformanceAsync()
    {
        if (_connection == null)
        {
            await InitializeAsync();
        }

        var fileTypePerformance = new Dictionary<string, FileTypePerformance>();
        
        var fileRecords = await _connection.Table<FileTestRecord>().ToListAsync();
        
        var fileTypes = fileRecords.Select(f => f.Extension).Distinct();
        
        foreach (var fileType in fileTypes)
        {
            var typeFiles = fileRecords.Where(f => f.Extension == fileType).ToList();
            
            if (typeFiles.Any())
            {
                fileTypePerformance[fileType] = new FileTypePerformance
                {
                    Extension = fileType,
                    FilesTested = typeFiles.Count,
                    FilesSuccessful = typeFiles.Count(f => f.Success),
                    AverageCompressionRatio = typeFiles.Average(f => f.CompressionRatio),
                    TotalSpaceSaved = typeFiles.Sum(f => f.OriginalSize - f.CompressedSize),
                    SuccessRate = (double)typeFiles.Count(f => f.Success) / typeFiles.Count
                };
            }
        }

        return fileTypePerformance;
    }

    /// <summary>
    * Export database to CSV
    /// </summary>
    public async Task ExportToCsvAsync(string outputPath)
    {
        if (_connection == null)
        {
            await InitializeAsync();
        }

        var testRecords = await _connection.Table<CompressionTestRecord>().ToListAsync();
        var fileRecords = await _connection.Table<FileTestRecord>().ToListAsync();

        // Export test records
        var testCsv = "TestId,TestStartTime,TestEndTime,TestDuration,BestPerformingTier,BestCompressionRatio,TotalFilesTested,TotalOriginalSize,TotalCompressedSize,TotalSpaceSaved,AverageCompressionRatio,AverageCompressionTime,AverageDecompressionTime,SuccessRate,CreatedAt\n";
        
        foreach (var record in testRecords)
        {
            testCsv += $"{record.TestId},{record.TestStartTime:yyyy-MM-dd HH:mm:ss},{record.TestEndTime:yyyy-MM-dd HH:mm:ss},{record.TestDuration},{record.BestPerformingTier},{record.BestCompressionRatio},{record.TotalFilesTested},{record.TotalOriginalSize},{record.TotalCompressedSize},{record.TotalSpaceSaved},{record.AverageCompressionRatio},{record.AverageCompressionTime},{record.AverageDecompressionTime},{record.SuccessRate},{record.CreatedAt:yyyy-MM-dd HH:mm:ss}\n";
        }

        await File.WriteAllTextAsync(Path.Combine(outputPath, "compression_tests.csv"), testCsv);

        // Export file records
        var fileCsv = "TestId,FilePath,FileName,Extension,OriginalSize,CompressedSize,CompressionRatio,CompressionTier,Success,ErrorMessage,AlgorithmUsed,CompressionTime,DecompressionSuccess,DecompressionTime,DataIntegrityVerified,TestedAt\n";
        
        foreach (var record in fileRecords)
        {
            fileCsv += $"{record.TestId},{record.FilePath},{record.FileName},{record.Extension},{record.OriginalSize},{record.CompressedSize},{record.CompressionRatio},{record.CompressionTier},{record.Success},{record.ErrorMessage},{record.AlgorithmUsed},{record.CompressionTime},{record.DecompressionSuccess},{record.DecompressionTime},{record.DataIntegrityVerified},{record.TestedAt:yyyy-MM-dd HH:mm:ss}\n";
        }

        await File.WriteAllTextAsync(Path.Combine(outputPath, "file_tests.csv"), fileCsv);
    }

    /// <summary>
    * Get system information
    /// </summary>
    private async Task<string> GetSystemInfoAsync()
    {
        try
        {
            var systemInfo = new SystemInfo();
            return $"OS: {systemInfo.OSVersion}, CPU: {systemInfo.ProcessorCount} cores, Memory: {systemInfo.TotalMemoryMB}MB";
        }
        catch
        {
            return "System info unavailable";
        }
    }

    /// <summary>
    * Clean up old records
    /// </summary>
    public async Task CleanupOldRecordsAsync(int daysToKeep = 30)
    {
        if (_connection == null)
        {
            await InitializeAsync();
        }

        var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
        
        // Delete old test records
        var oldTests = await _connection.Table<CompressionTestRecord>()
            .Where(t => t.CreatedAt < cutoffDate)
            .ToListAsync();

        foreach (var test in oldTests)
        {
            await _connection.DeleteAsync(test);
        }
    }
}

/// <summary>
* Database records
/// </summary>
public class CompressionTestRecord
{
    [PrimaryKey]
    public string TestId { get; set; }
    public DateTime TestStartTime { get; set; }
    public DateTime TestEndTime { get; set; }
    public double TestDuration { get; set; }
    public string BestPerformingTier { get; set; }
    public double BestCompressionRatio { get; set; }
    public int TotalFilesTested { get; set; }
    public long TotalOriginalSize { get; set; }
    public long TotalCompressedSize { get; set; }
    public long TotalSpaceSaved { get; set; }
    public double AverageCompressionRatio { get; set; }
    public double AverageCompressionTime { get; set; }
    public double AverageDecompressionTime { get; set; }
    public double SuccessRate { get; set; }
    public string SystemInfo { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FileTestRecord
{
    [PrimaryKey]
    public int Id { get; set; }
    public string TestId { get; set; }
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public string Extension { get; set; }
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public string CompressionTier { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public string AlgorithmUsed { get; set; }
    public double CompressionTime { get; set; }
    public bool DecompressionSuccess { get; set; }
    public double DecompressionTime { get; set; }
    public bool DataIntegrityVerified { get; set; }
    public DateTime? DataIntegrityCheckTime { get; set; }
    public string DataIntegrityErrorMessage { get; set; }
    public DateTime TestedAt { get; set; }
}

public class PerformanceMetricRecord
{
    [PrimaryKey]
    public int Id { get; set; }
    public string TestId { get; set; }
    public int TotalFilesTested { get; set; }
    public long TotalOriginalSize { get; set; }
    public long TotalCompressedSize { get; set; }
    public long TotalSpaceSaved { get; set; }
    public double AverageCompressionRatio { get; set; }
    public double AverageCompressionTime { get; set; }
    public double AverageDecompressionTime { get; set; }
    public double SuccessRate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SystemInfoRecord
{
    [PrimaryKey]
    public int Id { get; set; }
    public string TestId { get; set; }
    public string OSVersion { get; set; }
    public int ProcessorCount { get; set; }
    public long TotalMemoryMB { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecommendationRecord
{
    [PrimaryKey]
    public int Id { get; set; }
    public string TestId { get; set; }
    public string Recommendation { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
* Data structures for analysis
/// </summary>
public class CompressionTestDetails
{
    public CompressionTestRecord TestRecord { get; set; }
    public List<FileTestRecord> FileRecords { get; set; }
    public PerformanceMetricRecord PerformanceRecord { get; set; }
    public List<string> Recommendations { get; set; }
}

public class PerformanceStatistics
{
    public int TotalTests { get; set; }
    public double AverageCompressionRatio { get; set; }
    public TimeSpan AverageCompressionTime { get; set; }
    public TimeSpan AverageDecompressionTime { get; set; }
    public double AverageSuccessRate { get; set; }
    public long TotalSpaceSaved { get; set; }
    public double BestCompressionRatio { get; set; }
    public DateTime LatestTestDate { get; set; }
}

public class TierPerformance
{
    public string TierName { get; set; }
    public int FilesTested { get; set; }
    public int FilesSuccessful { get; set; }
    public double AverageCompressionRatio { get; set; }
    public TimeSpan AverageCompressionTime { get; set; }
    public long TotalSpaceSaved { get; set; }
    public double SuccessRate { get; set; }
}

public class FileTypePerformance
{
    public string Extension { get; set; }
    public int FilesTested { get; set; }
    public int FilesSuccessful { get; set; }
    public double AverageCompressionRatio { get; set; }
    public long TotalSpaceSaved { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
* System information helper
/// </summary>
public class SystemInfo
{
    public string OSVersion { get; set; }
    public int ProcessorCount { get; set; }
    public long TotalMemoryMB { get; set; }

    public SystemInfo()
    {
        OSVersion = Environment.OSVersion.VersionString;
        ProcessorCount = Environment.ProcessorCount;
        
        try
        {
            var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
            TotalMemoryMB = computerInfo.TotalPhysicalMemory / (1024 * 1024);
        }
        catch
        {
            TotalMemoryMB = 0;
        }
    }
}