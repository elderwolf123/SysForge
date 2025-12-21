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
            string arguments = $"/C /EXE:{algorithmArg} {recursiveArg} \"{escapedPath}\"";
            
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

                // Check if output contains compression indicators
                // Look for "C" attribute in compact.exe output (proper format: "C" flag)
                bool isCompressed = System.Text.RegularExpressions.Regex.IsMatch(
                    output, 
                    @"^\s*C\s+",  // Line starting with C (compressed attribute)
                    System.Text.RegularExpressions.RegexOptions.Multiline
                ) || output.Contains("are compressed");
                
                _fileLogger.Log($"[COMPACT] IsCompressed check for {path}: {isCompressed}");
                return isCompressed;
            }
            catch
            {
                return false;
            }
        }

        #region Output Parsing

        private CompactResult ParseCompactOutput(string output, TimeSpan duration)
        {
            var result = new CompactResult { Duration = duration };

            try
            {
                // Parse compact.exe output
                // Example output:
                // "Compressing files in C:\Games\Skyrim
                //  123 files within 456 directories were compressed.
                //  789,012 total bytes of data are stored in 345,678 bytes.
                //  The compression ratio is 2.3 to 1."

                // Extract file counts
                var filesMatch = Regex.Match(output, @"(\d+)\s+files?\s+(?:within|were)");
                if (filesMatch.Success)
                {
                    result.TotalFiles = long.Parse(filesMatch.Groups[1].Value.Replace(",", ""));
                    result.CompressedFiles = result.TotalFiles;
                }

                // Extract sizes
                var sizesMatch = Regex.Match(output, @"(\d+(?:,\d+)*)\s+total bytes.*?stored in\s+(\d+(?:,\d+)*)\s+bytes");
                if (sizesMatch.Success)
                {
                    result.OriginalSize = long.Parse(sizesMatch.Groups[1].Value.Replace(",", ""));
                    result.CompressedSize = long.Parse(sizesMatch.Groups[2].Value.Replace(",", ""));
                }

                // Calculate skipped files
                var skippedMatch = Regex.Match(output, @"(\d+)\s+files?\s+are skipped");
                if (skippedMatch.Success)
                {
                    result.SkippedFiles = long.Parse(skippedMatch.Groups[1].Value.Replace(",", ""));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Failed to parse compact.exe output: {ex.Message}");
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
