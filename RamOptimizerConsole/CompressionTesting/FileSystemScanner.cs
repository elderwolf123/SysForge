using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RamOptimizerConsole.CompressionTesting;

/// <summary>
* Comprehensive file system scanner for compression analysis
/// </summary>
public class FileSystemScanner
{
    private readonly ComprehensiveLogger _logger;
    private readonly CompressionDatabase _database;
    
    public FileSystemScanner(ComprehensiveLogger logger)
    {
        _logger = logger;
        _database = new CompressionDatabase();
    }

    /// <summary>
    * Analyze entire file system for compression potential
    /// </summary>
    public async Task<FileSystemAnalysisReport> AnalyzeFileSystemAsync(string rootPath = null)
    {
        if (string.IsNullOrEmpty(rootPath))
        {
            rootPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        _logger.LogInfo($"Starting file system analysis from: {rootPath}");
        
        var report = new FileSystemAnalysisReport
        {
            ScanStartTime = DateTime.Now,
            RootPath = rootPath,
            FileCategories = new Dictionary<string, FileCategoryStats>(),
            TotalFiles = 0,
            TotalSize = 0,
            CompressionPotential = new CompressionPotential()
        };

        try
        {
            // Scan directories
            var directories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);
            _logger.LogInfo($"Found {directories.Length} directories to scan");

            // Process each directory
            foreach (var directory in directories)
            {
                await ScanDirectoryAsync(directory, report);
            }

            // Calculate compression potential
            CalculateCompressionPotential(report);

            report.ScanEndTime = DateTime.Now;
            report.ScanDuration = report.ScanEndTime - report.ScanStartTime;

            _logger.LogInfo($"File system analysis completed. Found {report.TotalFiles} files totaling {FormatFileSize(report.TotalSize)}");
            
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during file system analysis: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    * Scan a specific directory
    /// </summary>
    private async Task ScanDirectoryAsync(string directoryPath, FileSystemAnalysisReport report)
    {
        try
        {
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly);
            
            foreach (var file in files)
            {
                await AnalyzeFileAsync(file, report);
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogDebug($"Access denied to directory: {directoryPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error scanning directory {directoryPath}: {ex.Message}");
        }
    }

    /// <summary>
    * Analyze individual file
    /// </summary>
    private async Task AnalyzeFileAsync(string filePath, FileSystemAnalysisReport report)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileCategory = CategorizeFile(filePath, fileInfo.Extension);
            
            // Update category statistics
            if (!report.FileCategories.ContainsKey(fileCategory))
            {
                report.FileCategories[fileCategory] = new FileCategoryStats
                {
                    CategoryName = fileCategory,
                    FileCount = 0,
                    TotalSize = 0,
                    AverageSize = 0,
                    CompressionRatio = 0,
                    Files = new List<FileAnalysis>()
                };
            }

            var categoryStats = report.FileCategories[fileCategory];
            
            // Create file analysis
            var fileAnalysis = new FileAnalysis
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                Extension = fileInfo.Extension,
                Size = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                Created = fileInfo.CreationTime,
                Category = fileCategory,
                CompressionPotential = EstimateCompressionPotential(fileInfo.Extension, fileInfo.Length)
            };

            // Update category statistics
            categoryStats.FileCount++;
            categoryStats.TotalSize += fileInfo.Length;
            categoryStats.Files.Add(fileAnalysis);
            
            // Update overall statistics
            report.TotalFiles++;
            report.TotalSize += fileInfo.Length;

            // Log progress
            if (report.TotalFiles % 100 == 0)
            {
                _logger.LogDebug($"Scanned {report.TotalFiles} files, {FormatFileSize(report.TotalSize)} total");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error analyzing file {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    * Categorize file based on extension
    /// </summary>
    private string CategorizeFile(string filePath, string extension)
    {
        extension = extension.ToLower();
        
        var categories = new Dictionary<string, List<string>>
        {
            ["Documents"] = new List<string> { ".txt", ".doc", ".docx", ".pdf", ".rtf", ".md", ".log", ".csv", ".xls", ".xlsx", ".ppt", ".pptx" },
            ["Images"] = new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico", ".svg", ".webp" },
            ["Audio"] = new List<string> { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".aiff" },
            ["Video"] = new List<string> { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v" },
            ["Code"] = new List<string> { ".cs", ".js", ".py", ".cpp", ".h", ".java", ".php", ".rb", ".go", ".rs", ".ts", ".html", ".css", ".sql", ".xml", ".json", ".yaml" },
            ["Archives"] = new List<string> { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz" },
            ["Executables"] = new List<string> { ".exe", ".dll", ".msi", ".bat", ".cmd", ".ps1", ".sh", ".app" },
            ["System"] = new List<string> { ".sys", ".dll", ".drv", ".ocx", ".cpl" },
            ["Configuration"] = new List<string> { ".ini", ".cfg", ".conf", ".config", ".yaml", ".yml", ".toml" },
            ["Database"] = new List<string> { ".db", ".sqlite", ".mdb", ".accdb", ".dbf", ".csv" },
            ["Temporary"] = new List<string> { ".tmp", ".temp", ".bak", ".backup", ".old" },
            ["Unknown"] = new List<string> { "" }
        };

        foreach (var category in categories)
        {
            if (category.Value.Contains(extension))
            {
                return category.Key;
            }
        }

        return "Unknown";
    }

    /// <summary>
    * Estimate compression potential based on file type and size
    /// </summary>
    private double EstimateCompressionPotential(string extension, long fileSize)
    {
        extension = extension.ToLower();
        
        // Base compression ratios by file type
        var compressionRatios = new Dictionary<string, double>
        {
            [".txt"] = 0.7,      // Text files compress very well
            [".doc"] = 0.6,      // Documents compress well
            [".pdf"] = 0.4,      // PDFs are already compressed
            [".jpg"] = 0.1,      // JPEG images are already compressed
            [".png"] = 0.2,      // PNG has some compression
            [".mp3"] = 0.05,     // MP3 is highly compressed
            [".wav"] = 0.6,      // Uncompressed audio compresses well
            [".mp4"] = 0.1,      // Video is already compressed
            [".zip"] = 0.0,      // Archives are already compressed
            [".exe"] = 0.3,      // Executables compress moderately
            [".dll"] = 0.3,      // DLLs compress moderately
            [".log"] = 0.8,      // Log files compress extremely well
            [".csv"] = 0.7,      // CSV files compress well
            [".json"] = 0.6,     // JSON compresses well
            [".xml"] = 0.6,      // XML compresses well
            [".html"] = 0.6,     // HTML compresses well
            [".css"] = 0.7,      // CSS compresses well
            [".js"] = 0.6,       // JavaScript compresses well
            [".py"] = 0.6,       // Python code compresses well
            [".cs"] = 0.6,       // C# code compresses well
            [".java"] = 0.6,     // Java code compresses well
            [".tmp"] = 0.9,      // Temporary files often compress extremely well
            [".bak"] = 0.8,      // Backup files compress well
            [""].Add(0.5)        // Unknown files get average compression
        };

        // Get base ratio
        double baseRatio = compressionRatios.ContainsKey(extension) ? 
            compressionRatios[extension] : 0.5;

        // Adjust based on file size
        if (fileSize < 1024) // Very small files
        {
            baseRatio *= 0.5;
        }
        else if (fileSize > 100 * 1024 * 1024) // Large files
        {
            baseRatio *= 1.1;
        }

        return Math.Max(0, Math.Min(1, baseRatio));
    }

    /// <summary>
    * Calculate overall compression potential
    /// </summary>
    private void CalculateCompressionPotential(FileSystemAnalysisReport report)
    {
        if (report.TotalFiles == 0)
        {
            report.CompressionPotential.OverallRatio = 0;
            return;
        }

        double totalCompressionPotential = 0;
        double weightedSum = 0;

        foreach (var category in report.FileCategories.Values)
        {
            if (category.FileCount > 0)
            {
                double avgCompression = category.Files.Average(f => f.CompressionPotential);
                double weight = (double)category.TotalSize / report.TotalSize;
                
                totalCompressionPotential += avgCompression * weight;
                weightedSum += weight;
            }
        }

        report.CompressionPotential.OverallRatio = weightedSum > 0 ? totalCompressionPotential / weightedSum : 0;
        report.CompressionPotential.TotalSavings = (long)(report.TotalSize * report.CompressionPotential.OverallRatio);
        report.CompressionPotential.FilesWithHighPotential = report.FileCategories.Values
            .SelectMany(c => c.Files)
            .Count(f => f.CompressionPotential > 0.7);
    }

    /// <summary>
    * Format file size in human readable format
    /// </summary>
    private string FormatFileSize(long bytes)
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

    /// <summary>
    * Get top compression candidates
    /// </summary>
    public List<FileAnalysis> GetTopCompressionCandidates(FileSystemAnalysisReport report, int count = 100)
    {
        return report.FileCategories.Values
            .SelectMany(c => c.Files)
            .Where(f => f.CompressionPotential > 0.5)
            .OrderByDescending(f => f.CompressionPotential)
            .ThenByDescending(f => f.Size)
            .Take(count)
            .ToList();
    }

    /// <summary>
    * Get largest files
    /// </summary>
    public List<FileAnalysis> GetLargestFiles(FileSystemAnalysisReport report, int count = 50)
    {
        return report.FileCategories.Values
            .SelectMany(c => c.Files)
            .OrderByDescending(f => f.Size)
            .Take(count)
            .ToList();
    }

    /// <summary>
    * Get files by category
    /// </summary>
    public Dictionary<string, List<FileAnalysis>> GetFilesByCategory(FileSystemAnalysisReport report)
    {
        return report.FileCategories.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Files.ToList()
        );
    }
}

/// <summary>
* File system analysis report
/// </summary>
public class FileSystemAnalysisReport
{
    public DateTime ScanStartTime { get; set; }
    public DateTime ScanEndTime { get; set; }
    public TimeSpan ScanDuration { get; set; }
    public string RootPath { get; set; }
    public int TotalFiles { get; set; }
    public long TotalSize { get; set; }
    public Dictionary<string, FileCategoryStats> FileCategories { get; set; }
    public CompressionPotential CompressionPotential { get; set; }
}

/// <summary>
* File category statistics
/// </summary>
public class FileCategoryStats
{
    public string CategoryName { get; set; }
    public int FileCount { get; set; }
    public long TotalSize { get; set; }
    public double AverageSize { get; set; }
    public double CompressionRatio { get; set; }
    public List<FileAnalysis> Files { get; set; }
}

/// <summary>
* Individual file analysis
/// </summary>
public class FileAnalysis
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public string Extension { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime Created { get; set; }
    public string Category { get; set; }
    public double CompressionPotential { get; set; }
}

/// <summary>
* Compression potential analysis
/// </summary>
public class CompressionPotential
{
    public double OverallRatio { get; set; }
    public long TotalSavings { get; set; }
    public int FilesWithHighPotential { get; set; }
    public int FilesWithMediumPotential { get; set; }
    public int FilesWithLowPotential { get; set; }
}