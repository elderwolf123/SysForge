using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.Services;
    /// <summary>
    /// Windows Compact compression algorithms
    /// </summary>
    public enum CompactAlgorithm
    {
        XPRESS4K,   // Fast, ~45% compression
        XPRESS8K,   // Balanced, ~50% compression
        XPRESS16K,  // Better, ~52% compression
        LZX         // Best, ~55% compression (recommended for games)
    }

    /// <summary>
    /// Result of Windows Compact compression operation
    /// </summary>
    public class CompactResult
    {
        public bool Success { get; set; }
        public long TotalFiles { get; set; }
        public long CompressedFiles { get; set; }
        public long SkippedFiles { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio => CompressedSize / (double)OriginalSize;
        public double SpaceSaved => 1.0 - CompressionRatio;
        public TimeSpan Duration { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Windows Compact transparent compression integration
    /// Uses built-in Windows compression for transparent, instant-access compression
    /// </summary>
    public class WindowsCompactCompression
    {
        private readonly ILogger? _logger;

        private readonly FileLogger _fileLogger = FileLogger.Instance;

        public WindowsCompactCompression(ILogger? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Compress files or directory using Windows Compact
        /// </summary>
        public async Task<CompactResult> CompressAsync(
            string path,
            CompactAlgorithm algorithm = CompactAlgorithm.LZX,
            bool recursive = true)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new FileNotFoundException($"Path not found: {path}");
            }

            _logger?.LogInformation($"Starting Windows Compact compression: {path}");
            _logger?.LogInformation($"Algorithm: {algorithm}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Build compact.exe arguments
                string algorithmArg = algorithm switch
                {
                    CompactAlgorithm.XPRESS4K => "XPRESS4K",
                    CompactAlgorithm.XPRESS8K => "XPRESS8K",
                    CompactAlgorithm.XPRESS16K => "XPRESS16K",
                    CompactAlgorithm.LZX => "LZX",
                    _ => "LZX"
                };

                string recursiveArg = recursive ? "/S" : "";
            // Escape paths properly - escape quotes in path
            string escapedPath = path.Replace("\"", "\"\"");
            // Use CompactGUI approach: /C /EXE:algorithm /I /Q /S
            // /EXE:algorithm = Use specified compression algorithm (works on ALL files, not just .exe!)
            // /I = Continue on errors (don't stop if one file fails)
            // /Q = Quiet mode (reduces output verbosity)
            // /S = Recursive (compress subdirectories)
            string arguments = $"/C /EXE:{algorithmArg} {recursiveArg} /I /Q \"{escapedPath}\"";
            
            _fileLogger.Log($"[COMPACT] Target path: {path}");
            _fileLogger.Log($"[COMPACT] Escaped path: {escapedPath}");
            _fileLogger.Log($"[COMPACT] Algorithm: {algorithmArg}");
            _fileLogger.Log($"[COMPACT] Full arguments: compact.exe {arguments}");

                // Execute compact.exe
                var processInfo = new ProcessStartInfo
                {
                    FileName = "compact.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.Exists(path) ? path : Path.GetDirectoryName(path)
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start compact.exe");
                }

                string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            stopwatch.Stop();
            
            // Log compact.exe output for debugging
            _fileLogger.Log($"[COMPACT] Exit code: {process.ExitCode}");
            _fileLogger.Log($"[COMPACT] Duration: {stopwatch.Elapsed.TotalSeconds:F2}s");
            _fileLogger.Log($"[COMPACT] STDOUT ({output.Length} chars):");
            foreach (var line in output.Split('\n').Take(20))
            {
                _fileLogger.Log($"  {line.TrimEnd()}");
            }
            if (!string.IsNullOrEmpty(error))
            {
                _fileLogger.Log($"[COMPACT] STDERR: {error}");
            }

                // Parse output
                var result = ParseCompactOutput(output, stopwatch.Elapsed);

                if (process.ExitCode != 0)
                {
                    result.Success = false;
                    result.Error = $"Compact.exe failed with exit code {process.ExitCode}: {error}";
                    _logger?.LogError(result.Error);
                }
                else
                {
                    result.Success = true;
                    _logger?.LogInformation($"Compression complete: {result.CompressedFiles} files, {FormatSize(result.OriginalSize - result.CompressedSize)} saved ({result.SpaceSaved:P2})");
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError($"Windows Compact compression failed: {ex.Message}");
                
                return new CompactResult
                {
                    Success = false,
                    Error = ex.Message,
                    Duration = stopwatch.Elapsed
                };
            }
        }

        /// <summary>
        /// Decompress files or directory
        /// </summary>
        public async Task<CompactResult> DecompressAsync(string path, bool recursive = true)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new FileNotFoundException($"Path not found: {path}");
            }

            _logger?.LogInformation($"Decompressing: {path}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                string recursiveArg = recursive ? "/s" : "";
                string arguments = $"/u {recursiveArg} \"{path}\"";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "compact.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.Exists(path) ? path : Path.GetDirectoryName(path)
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start compact.exe");
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                stopwatch.Stop();

                var result = ParseCompactOutput(output, stopwatch.Elapsed);
                result.Success = process.ExitCode == 0;

                _logger?.LogInformation($"Decompression complete");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError($"Decompression failed: {ex.Message}");
                
                return new CompactResult
                {
                    Success = false,
                    Error = ex.Message,
                    Duration = stopwatch.Elapsed
                };
            }
        }

        /// <summary>
/// Query compression status of a file or directory
/// </summary>
public async Task<bool> IsCompressedAsync(string path)
{
    try
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "compact.exe",
            Arguments = $"\"{path}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null) return false;

        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Log first few lines for debugging
        var lines = output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Take(5).ToList();
        _fileLogger.Log($"[COMPACT] IsCompressed output for {Path.GetFileName(path)}:");
        foreach (var line in lines)
        {
            _fileLogger.Log($"  {line.Trim()}");
        }

        // Check if output says "N are compressed" where N > 0
        // Example: "5 are compressed and 0 are not compressed" = True
        // Example: "0 are compressed and 1 are not compressed" = False
        var compressedMatch = Regex.Match(output, @"(\d+)\s+(?:are|is)\s+compressed");
        bool isCompressed = false;
        
        if (compressedMatch.Success)
        {
            var count = int.Parse(compressedMatch.Groups[1].Value);
            isCompressed = count > 0;
            _fileLogger.Log($"[COMPACT] Compressed count: {count}");
        }
        else if (output.Contains("Of the files listed"))
        {
            // Fallback for directory listing format
            isCompressed = true;
        }
        
        _fileLogger.Log($"[COMPACT] Result: {isCompressed}");
        return isCompressed;
    }
    catch (Exception ex)
    {
        _fileLogger.Log($"[COMPACT] Error checking {path}: {ex.Message}");
        return false;
    }
}

        #region Output Parsing

        private CompactResult ParseCompactOutput(string output, TimeSpan duration)
{
    var result = new CompactResult { Duration = duration };

    try
    {
        // Parse compact.exe output - summary can be ANYWHERE in the output
        // For large outputs (20k+ lines), the summary lines are scattered throughout
        // Example summary lines:
        // "123 files within 456 directories  were compressed."
        // "789,012 total bytes of data are stored in 345,678 bytes."
        // "The compression ratio is 2.3 to 1."

        var lines = output.Split('\n');
        _fileLogger.Log($"[COMPACT] Parsing output ({lines.Length} total lines, {output.Length} chars)");

        // Extract file counts from summary - search ENTIRE output
        var filesMatch = Regex.Match(output, @"(\d+(?:,\d+)*)\s+files?\s+within\s+\d+\s+directories\s+were\s+compressed", RegexOptions.IgnoreCase);
        if (filesMatch.Success)
        {
            var filesStr = filesMatch.Groups[1].Value.Replace(",", "");
            result.TotalFiles = long.Parse(filesStr);
            result.CompressedFiles = result.TotalFiles;
            _fileLogger.Log($"[COMPACT] Found: {result.TotalFiles} files compressed");
        }
        else
        {
            // Try alternative format: "X files were compressed"
            var altMatch = Regex.Match(output, @"(\d+(?:,\d+)*)\s+files?\s+were\s+compressed", RegexOptions.IgnoreCase);
            if (altMatch.Success)
            {
                var filesStr = altMatch.Groups[1].Value.Replace(",", "");
                result.TotalFiles = long.Parse(filesStr);
               result.CompressedFiles = result.TotalFiles;
                _fileLogger.Log($"[COMPACT] Found (alt format): {result.TotalFiles} files compressed");
            }
            else
            {
                _fileLogger.Log($"[COMPACT] No 'files were compressed' summary found - files may already be compressed or skipped");
            }
        }

        // Extract sizes - search entire output
        var sizesMatch = Regex.Match(output, @"(\d+(?:,\d+)*)\s+total bytes.*?stored in\s+(\d+(?:,\d+)*)\s+bytes", RegexOptions.IgnoreCase);
        if (sizesMatch.Success)
        {
            result.OriginalSize = long.Parse(sizesMatch.Groups[1].Value.Replace(",", ""));
            result.CompressedSize = long.Parse(sizesMatch.Groups[2].Value.Replace(",", ""));
            _fileLogger.Log($"[COMPACT] Found sizes: {result.OriginalSize} -> {result.CompressedSize}");
        }

        // Calculate skipped files
        var skippedMatch = Regex.Match(output, @"(\d+(?:,\d+)*)\s+files?\s+(?:are|were)\s+skipped", RegexOptions.IgnoreCase);
        if (skippedMatch.Success)
        {
            result.SkippedFiles = long.Parse(skippedMatch.Groups[1].Value.Replace(",", ""));
            _fileLogger.Log($"[COMPACT] Found: {result.SkippedFiles} files skipped (already compressed or excluded)");
        }
    }
    catch (Exception ex)
    {
        _fileLogger.LogError("Failed to parse compact.exe output", ex);
    }

    return result;
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
    }
