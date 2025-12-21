using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RamOptimizerNova.Services
{
    public class CompressionAnalysisService
    {
        private readonly FileLogger _logger;

        // File extensions categorized by compressibility
        private static readonly HashSet<string> HighlyCompressible = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".log", ".xml", ".json", ".csv", ".ini", ".cfg", ".yaml", ".yml",
            ".md", ".rst", ".html", ".css", ".js", ".ts", ".sql", ".sh", ".bat", ".ps1",
            ".wav", ".aiff", ".bmp", ".tga", ".psd", ".svg", ".cpp", ".h", ".cs", ".java"
        };

        private static readonly HashSet<string> ModeratelyCompressible = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".dat", ".bin", ".sys", ".db", ".sqlite", ".mdb",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
        };

        private static readonly HashSet<string> AlreadyCompressed = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".dds",
            ".mp3", ".ogg", ".m4a", ".aac", ".wma", ".opus",
            ".mp4", ".avi", ".mkv", ".webm", ".mov", ".flv",
            ".zip", ".rar", ".7z", ".gz", ".bz2", ".xz", ".pak",
            ".unity3d", ".unitypackage", ".apk", ".ipa"
        };

        public CompressionAnalysisService()
        {
            _logger = FileLogger.Instance;
        }

        public async Task<AnalysisResult> AnalyzeAsync(string path, IProgress<AnalysisProgress>? progress = null)
        {
            _logger.Log($"[ANALYSIS] Starting analysis of: {path}");
            
            var result = new AnalysisResult();
            var fileTypeStats = new Dictionary<string, FileTypeStats>();
            long processedFiles = 0;

            try
            {
                // Check if path exists
                if (!Directory.Exists(path) && !File.Exists(path))
                {
                    _logger.LogError($"[ANALYSIS] Path does not exist: {path}", null);
                    return result;
                }

                // Get all files
                var files = Directory.Exists(path)
                    ? Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                    : new[] { path };

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var extension = fileInfo.Extension.ToLowerInvariant();
                        
                        result.TotalFiles++;
                        result.TotalBytes += fileInfo.Length;

                        // Categorize file
                        var category = GetCompressionCategory(extension);
                        var estimatedSavings = CalculateEstimatedSavings(fileInfo.Length, category);

                        if (category != CompressionCategory.AlreadyCompressed)
                        {
                            result.CompressibleFiles++;
                            result.CompressibleBytes += fileInfo.Length;
                            result.EstimatedSavingsBytes += estimatedSavings;
                        }

                        // Track by extension
                        if (!fileTypeStats.ContainsKey(extension))
                        {
                            fileTypeStats[extension] = new FileTypeStats
                            {
                                Extension = extension,
                                Category = category
                            };
                        }

                        var stats = fileTypeStats[extension];
                        stats.FileCount++;
                        stats.TotalBytes += fileInfo.Length;
                        stats.EstimatedSavingsBytes += estimatedSavings;

                        processedFiles++;

                        // Report progress every 100 files
                        if (processedFiles % 100 == 0 && progress != null)
                        {
                            progress.Report(new AnalysisProgress
                            {
                                FilesProcessed = processedFiles,
                                CurrentFile = Path.GetFileName(file),
                                CompressibleBytes = result.CompressibleBytes,
                                EstimatedSavingsBytes = result.EstimatedSavingsBytes
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"[ANALYSIS] Error processing file {file}: {ex.Message}");
                    }
                }

                result.FileTypeBreakdown = fileTypeStats;

                _logger.Log($"[ANALYSIS] Complete - {result.TotalFiles} files, {FormatBytes(result.EstimatedSavingsBytes)} estimated savings");
            }
            catch (Exception ex)
            {
                _logger.LogError("[ANALYSIS] Failed to analyze", ex);
            }

            return result;
        }

        private CompressionCategory GetCompressionCategory(string extension)
        {
            if (HighlyCompressible.Contains(extension))
                return CompressionCategory.HighlyCompressible;
            
            if (ModeratelyCompressible.Contains(extension))
                return CompressionCategory.ModeratelyCompressible;
            
            if (AlreadyCompressed.Contains(extension))
                return CompressionCategory.AlreadyCompressed;

            // Unknown extensions - assume moderately compressible
            return CompressionCategory.ModeratelyCompressible;
        }

        private long CalculateEstimatedSavings(long fileSize, CompressionCategory category)
        {
            double savingsPercent = category switch
            {
                CompressionCategory.HighlyCompressible => 0.50,      // 50% savings
                CompressionCategory.ModeratelyCompressible => 0.30,  // 30% savings
                CompressionCategory.AlreadyCompressed => 0.0,        // 0% savings
                _ => 0.0
            };

            return (long)(fileSize * savingsPercent);
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public enum CompressionCategory
    {
        HighlyCompressible,
        ModeratelyCompressible,
        AlreadyCompressed
    }

    public class AnalysisResult
    {
        public long TotalFiles { get; set; }
        public long TotalBytes { get; set; }
        public long CompressibleFiles { get; set; }
        public long CompressibleBytes { get; set; }
        public long EstimatedSavingsBytes { get; set; }
        public Dictionary<string, FileTypeStats> FileTypeBreakdown { get; set; } = new();
    }

    public class FileTypeStats
    {
        public string Extension { get; set; } = "";
        public CompressionCategory Category { get; set; }
        public long FileCount { get; set; }
        public long TotalBytes { get; set; }
        public long EstimatedSavingsBytes { get; set; }
    }

    public class AnalysisProgress
    {
        public long FilesProcessed { get; set; }
        public string CurrentFile { get; set; } = "";
        public long CompressibleBytes { get; set; }
        public long EstimatedSavingsBytes { get; set; }
    }
}
