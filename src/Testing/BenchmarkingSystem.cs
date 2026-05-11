using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RamOptimizer.Logging;
using RamOptimizer.Monitoring;

namespace RamOptimizer.Testing
{
    public class BenchmarkingSystem
    {
        private readonly ComprehensiveLogger _logger;
        private readonly RealTimePerformanceMonitor _monitor;
        private readonly List<BenchmarkResult> _benchmarkResults;
        private readonly object _lockObject = new object();

        public event EventHandler<BenchmarkProgressEventArgs> BenchmarkProgressUpdated;
        public event EventHandler<BenchmarkResult> BenchmarkCompleted;

        public BenchmarkingSystem(ComprehensiveLogger logger, RealTimePerformanceMonitor monitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _benchmarkResults = new List<BenchmarkResult>();
        }

        public async Task<BenchmarkResult> RunCpuBenchmarkAsync(int durationSeconds = 30)
        {
            _logger.LogInfo($"Starting CPU benchmark for {durationSeconds} seconds");
            
            var result = new BenchmarkResult
            {
                BenchmarkName = "CPU Benchmark",
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                Metrics = new List<PerformanceMetrics>()
            };

            try
            {
                // Start monitoring
                void OnMetricsUpdated(object sender, PerformanceMetrics e) => result.Metrics.Add(e);
                _monitor.PerformanceMetricsUpdated += OnMetricsUpdated;
                
                var stopwatch = Stopwatch.StartNew();
                var calculations = 0L;
                
                // Perform CPU-intensive calculations
                while (stopwatch.Elapsed < result.Duration)
                {
                    // Perform complex mathematical operations
                    for (int i = 0; i < 10000; i++)
                    {
                        var value = Math.Sqrt(Math.Pow(i, 2.5) + Math.Sin(i) * Math.Cos(i));
                        calculations++;
                    }
                    
                    // Report progress
                    var progress = (int)((stopwatch.Elapsed.TotalSeconds / result.Duration.TotalSeconds) * 100);
                    BenchmarkProgressUpdated?.Invoke(this, new BenchmarkProgressEventArgs
                    {
                        BenchmarkName = result.BenchmarkName,
                        ProgressPercentage = progress,
                        ElapsedTime = stopwatch.Elapsed
                    });
                }
                
                stopwatch.Stop();
                _monitor.PerformanceMetricsUpdated -= OnMetricsUpdated;
                
                result.EndTime = DateTime.UtcNow;
                result.Score = calculations / stopwatch.Elapsed.TotalSeconds; // Operations per second
                result.Status = BenchmarkStatus.Completed;
                
                // Calculate statistics
                if (result.Metrics.Any())
                {
                    result.AverageCpuUsage = result.Metrics.Average(m => m.CpuUsage);
                    result.PeakCpuUsage = result.Metrics.Max(m => m.CpuUsage);
                    result.AverageMemoryUsage = result.Metrics.Average(m => m.AvailableMemoryMB);
                }
                
                lock (_lockObject)
                {
                    _benchmarkResults.Add(result);
                }
                
                BenchmarkCompleted?.Invoke(this, result);
                _logger.LogInfo($"CPU benchmark completed with score: {result.Score:F2} operations/second");
                
                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.Status = BenchmarkStatus.Failed;
                result.ErrorMessage = ex.Message;
                
                lock (_lockObject)
                {
                    _benchmarkResults.Add(result);
                }
                
                BenchmarkCompleted?.Invoke(this, result);
                _logger.LogError($"CPU benchmark failed: {ex.Message}");
                
                return result;
            }
        }

        public async Task<BenchmarkResult> RunMemoryBenchmarkAsync(int memorySizeMB = 512)
        {
            _logger.LogInfo($"Starting memory benchmark with {memorySizeMB} MB");
            
            var result = new BenchmarkResult
            {
                BenchmarkName = "Memory Benchmark",
                StartTime = DateTime.UtcNow,
                MemorySizeMB = memorySizeMB,
                Metrics = new List<PerformanceMetrics>()
            };

            try
            {
                // Start monitoring
                void OnMetricsUpdated(object sender, PerformanceMetrics e) => result.Metrics.Add(e);
                _monitor.PerformanceMetricsUpdated += OnMetricsUpdated;
                
                var stopwatch = Stopwatch.StartNew();
                var memoryBlocks = new List<byte[]>();
                var blockSize = 10 * 1024 * 1024; // 10 MB blocks
                var totalBlocks = (int)(memorySizeMB * 1024 * 1024 / blockSize);
                var successfulAllocations = 0;
                
                // Allocate memory blocks
                for (int i = 0; i < totalBlocks; i++)
                {
                    try
                    {
                        var block = new byte[blockSize];
                        // Fill with data to prevent optimization
                        new Random().NextBytes(block);
                        memoryBlocks.Add(block);
                        successfulAllocations++;
                        
                        // Report progress
                        var progress = (int)(((i + 1) / (double)totalBlocks) * 100);
                        BenchmarkProgressUpdated?.Invoke(this, new BenchmarkProgressEventArgs
                        {
                            BenchmarkName = result.BenchmarkName,
                            ProgressPercentage = progress,
                            AllocatedMemoryMB = successfulAllocations * blockSize / (1024.0 * 1024.0)
                        });
                    }
                    catch (OutOfMemoryException)
                    {
                        _logger.LogWarning("Out of memory during memory benchmark");
                        break;
                    }
                }
                
                stopwatch.Stop();
                _monitor.PerformanceMetricsUpdated -= OnMetricsUpdated;
                
                result.EndTime = DateTime.UtcNow;
                result.Duration = stopwatch.Elapsed;
                result.Score = successfulAllocations * blockSize / stopwatch.Elapsed.TotalSeconds; // Bytes per second
                result.Status = BenchmarkStatus.Completed;
                
                // Calculate statistics
                if (result.Metrics.Any())
                {
                    result.AverageMemoryUsage = result.Metrics.Average(m => m.AvailableMemoryMB);
                    result.MinAvailableMemoryMB = result.Metrics.Min(m => m.AvailableMemoryMB);
                    result.PeakMemoryUsage = result.Metrics.Max(m => m.AvailableMemoryMB);
                }
                
                // Clean up
                memoryBlocks.Clear();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                lock (_lockObject)
                {
                    _benchmarkResults.Add(result);
                }
                
                BenchmarkCompleted?.Invoke(this, result);
                _logger.LogInfo($"Memory benchmark completed with score: {result.Score / (1024 * 1024):F2} MB/second");
                
                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.Status = BenchmarkStatus.Failed;
                result.ErrorMessage = ex.Message;
                
                lock (_lockObject)
                {
                    _benchmarkResults.Add(result);
                }
                
                BenchmarkCompleted?.Invoke(this, result);
                _logger.LogError($"Memory benchmark failed: {ex.Message}");
                
                return result;
            }
        }

        public async Task<BenchmarkResult> RunDiskBenchmarkAsync(string testDirectory = null, int fileSizeMB = 100)
        {
            _logger.LogInfo($"Starting disk benchmark with {fileSizeMB} MB file");
            
            var result = new BenchmarkResult
            {
                BenchmarkName = "Disk Benchmark",
                StartTime = DateTime.UtcNow,
                FileSizeMB = fileSizeMB,
                Metrics = new List<PerformanceMetrics>()
            };

            try
            {
                // Use temp directory if none specified
                if (string.IsNullOrEmpty(testDirectory))
                {
                    testDirectory = Path.GetTempPath();
                }
                
                var testFile = Path.Combine(testDirectory, $"disk_benchmark_{Guid.NewGuid()}.dat");
                
                // Start monitoring
                void OnMetricsUpdated(object sender, PerformanceMetrics e) => result.Metrics.Add(e);
                _monitor.PerformanceMetricsUpdated += OnMetricsUpdated;
                
                var stopwatch = Stopwatch.StartNew();
                var buffer = new byte[1024 * 1024]; // 1 MB buffer
                
                // Write test
                var (totalBytesWritten, writeElapsedSeconds) = await ExecuteDiskWritePhaseAsync(testFile, buffer, fileSizeMB, result.BenchmarkName);
                
                // Read test
                var (totalBytesRead, readElapsedSeconds) = await ExecuteDiskReadPhaseAsync(testFile, buffer, fileSizeMB, result.BenchmarkName);
                
                stopwatch.Stop();
                _monitor.PerformanceMetricsUpdated -= OnMetricsUpdated;
                
                result.EndTime = DateTime.UtcNow;
                result.Duration = stopwatch.Elapsed;
                
                // Calculate scores
                var writeSpeed = (totalBytesWritten / writeElapsedSeconds); // Bytes per second
                var readSpeed = (totalBytesRead / readElapsedSeconds); // Bytes per second
                result.Score = (writeSpeed + readSpeed) / 2; // Average speed
                
                result.WriteSpeedMBps = writeSpeed / (1024 * 1024);
                result.ReadSpeedMBps = readSpeed / (1024 * 1024);
                result.Status = BenchmarkStatus.Completed;
                
                // Calculate statistics
                if (result.Metrics.Any())
                {
                    result.AverageDiskUsage = result.Metrics.Average(m => m.DiskUsage);
                    result.PeakDiskUsage = result.Metrics.Max(m => m.DiskUsage);
                }
                
                // Clean up
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
                
                lock (_lockObject)
                {
                    _benchmarkResults.Add(result);
                }
                
                BenchmarkCompleted?.Invoke(this, result);
                _logger.LogInfo($"Disk benchmark completed. Write: {result.WriteSpeedMBps:F2} MB/s, Read: {result.ReadSpeedMBps:F2} MB/s");
                
                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.Status = BenchmarkStatus.Failed;
                result.ErrorMessage = ex.Message;
                
                lock (_lockObject)
                {
                    _benchmarkResults.Add(result);
                }
                
                BenchmarkCompleted?.Invoke(this, result);
                _logger.LogError($"Disk benchmark failed: {ex.Message}");
                
                return result;
            }
        }

        private async Task<(long bytesWritten, double elapsedSeconds)> ExecuteDiskWritePhaseAsync(string testFile, byte[] buffer, int fileSizeMB, string benchmarkName)
        {
            var writeStopwatch = Stopwatch.StartNew();
            var random = new Random();
            var totalBytesWritten = 0L;

            using (var fileStream = new FileStream(testFile, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, FileOptions.SequentialScan))
            {
                while (totalBytesWritten < fileSizeMB * 1024L * 1024L)
                {
                    random.NextBytes(buffer);
                    await fileStream.WriteAsync(buffer, 0, buffer.Length);
                    totalBytesWritten += buffer.Length;

                    // Report progress
                    var progress = (int)((totalBytesWritten / (double)(fileSizeMB * 1024L * 1024L)) * 100);
                    BenchmarkProgressUpdated?.Invoke(this, new BenchmarkProgressEventArgs
                    {
                        BenchmarkName = benchmarkName,
                        ProgressPercentage = progress,
                        BytesProcessed = totalBytesWritten
                    });
                }

                await fileStream.FlushAsync();
            }

            writeStopwatch.Stop();
            return (totalBytesWritten, writeStopwatch.Elapsed.TotalSeconds);
        }

        private async Task<(long bytesRead, double elapsedSeconds)> ExecuteDiskReadPhaseAsync(string testFile, byte[] buffer, int fileSizeMB, string benchmarkName)
        {
            var readStopwatch = Stopwatch.StartNew();
            var totalBytesRead = 0L;

            using (var fileStream = new FileStream(testFile, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, FileOptions.SequentialScan))
            {
                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytesRead += bytesRead;

                    // Report progress
                    var progress = 50 + (int)((totalBytesRead / (double)(fileSizeMB * 1024L * 1024L)) * 50);
                    BenchmarkProgressUpdated?.Invoke(this, new BenchmarkProgressEventArgs
                    {
                        BenchmarkName = benchmarkName,
                        ProgressPercentage = Math.Min(progress, 100),
                        BytesProcessed = totalBytesRead
                    });
                }
            }

            readStopwatch.Stop();
            return (totalBytesRead, readStopwatch.Elapsed.TotalSeconds);
        }

        public async Task<BenchmarkResult> RunCompressionBenchmarkAsync(string testDirectory = null)
        {
            _logger.LogInfo("Starting compression benchmark");
            
            var result = new BenchmarkResult
            {
                BenchmarkName = "Compression Benchmark",
                StartTime = DateTime.UtcNow,
                Metrics = new List<PerformanceMetrics>()
            };

            try
            {
                // Use temp directory if none specified
                if (string.IsNullOrEmpty(testDirectory))
                {
                    testDirectory = Path.GetTempPath();
                }
                
                var testFile = Path.Combine(testDirectory, $"compression_benchmark_{Guid.NewGuid()}.txt");
                var compressedFile = testFile + ".gz";
                
                // Create test data
                var testData = new string('A', 10 * 1024 * 1024); // 10 MB of data
                await File.WriteAllTextAsync(testFile, testData);
                
                // Start monitoring
                void OnMetricsUpdated(object sender, PerformanceMetrics e) => result.Metrics.Add(e);
                _monitor.PerformanceMetricsUpdated += OnMetricsUpdated;
                
                var stopwatch = Stopwatch.StartNew();
                
                // Compression test
                using (var originalFileStream = File.OpenRead(testFile))
                using (var compressedFileStream = File.Create(compressedFile))
                using (var compressionStream = new System.IO.Compression.GZipStream(compressedFileStream, System.IO.Compression.CompressionLevel.Optimal))
                {
                    await originalFileStream.CopyToAsync(compressionStream);
                }
                
                stopwatch.Stop();
                _monitor.PerformanceMetricsUpdated -= OnMetricsUpdated;
                
                result.EndTime = DateTime.UtcNow;
                result.Duration = stopwatch.Elapsed;
                
                // Calculate score
                var originalSize = new FileInfo(testFile).Length;
                var compressedSize = new FileInfo(compressedFile).Length;
                var compressionRatio = (double)compressedSize / originalSize;
                result.Score = originalSize / stopwatch.Elapsed.TotalSeconds; // Bytes per second
                result.CompressionRatio = compressionRatio;
                result.Status = BenchmarkStatus.Completed;
                
                // Clean up
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
                if (File.Exists(compressedFile))
                {
                    File.Delete(compressedFile);
                }
                
                lock (_lockObject)
                {
                    _benchmarkResults.Add(result);
                }
                
                BenchmarkCompleted?.Invoke(this, result);
                _logger.LogInfo($"Compression benchmark completed. Ratio: {compressionRatio:P2}, Speed: {result.Score / (1024 * 1024):F2} MB/s");
                
                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.Status = BenchmarkStatus.Failed;
                result.ErrorMessage = ex.Message;
                
                lock (_lockObject)
                {
                    _benchmarkResults.Add(result);
                }
                
                BenchmarkCompleted?.Invoke(this, result);
                _logger.LogError($"Compression benchmark failed: {ex.Message}");
                
                return result;
            }
        }

        public async Task<BenchmarkSuiteResult> RunBenchmarkSuiteAsync()
        {
            _logger.LogInfo("Starting benchmark suite");
            
            var suiteResult = new BenchmarkSuiteResult
            {
                SuiteName = "Full Benchmark Suite",
                StartTime = DateTime.UtcNow,
                IndividualResults = new List<BenchmarkResult>()
            };

            try
            {
                // Run all benchmarks
                var benchmarks = new List<Func<Task<BenchmarkResult>>>
                {
                    () => RunCpuBenchmarkAsync(30),
                    () => RunMemoryBenchmarkAsync(256),
                    () => RunDiskBenchmarkAsync(null, 50),
                    () => RunCompressionBenchmarkAsync()
                };

                var results = new List<BenchmarkResult>();
                
                for (int i = 0; i < benchmarks.Count; i++)
                {
                    var benchmark = benchmarks[i];
                    var result = await benchmark();
                    results.Add(result);
                    suiteResult.IndividualResults.Add(result);
                    
                    // Report progress
                    var progress = (int)(((i + 1) / (double)benchmarks.Count) * 100);
                    BenchmarkProgressUpdated?.Invoke(this, new BenchmarkProgressEventArgs
                    {
                        BenchmarkName = suiteResult.SuiteName,
                        ProgressPercentage = progress,
                        CompletedBenchmarks = i + 1,
                        TotalBenchmarks = benchmarks.Count
                    });
                }

                suiteResult.EndTime = DateTime.UtcNow;
                suiteResult.Duration = suiteResult.EndTime - suiteResult.StartTime;
                suiteResult.Status = BenchmarkStatus.Completed;
                
                // Calculate overall score
                suiteResult.OverallScore = results.Where(r => r.Status == BenchmarkStatus.Completed)
                    .Average(r => r.Score);
                
                lock (_lockObject)
                {
                    _benchmarkResults.AddRange(results);
                }
                
                BenchmarkCompleted?.Invoke(this, new BenchmarkResult
                {
                    BenchmarkName = suiteResult.SuiteName,
                    StartTime = suiteResult.StartTime,
                    EndTime = suiteResult.EndTime,
                    Duration = suiteResult.Duration,
                    Score = suiteResult.OverallScore,
                    Status = suiteResult.Status
                });
                
                _logger.LogInfo($"Benchmark suite completed. Overall score: {suiteResult.OverallScore:F2}");
                return suiteResult;
            }
            catch (Exception ex)
            {
                suiteResult.EndTime = DateTime.UtcNow;
                suiteResult.Status = BenchmarkStatus.Failed;
                suiteResult.ErrorMessage = ex.Message;
                
                BenchmarkCompleted?.Invoke(this, new BenchmarkResult
                {
                    BenchmarkName = suiteResult.SuiteName,
                    StartTime = suiteResult.StartTime,
                    EndTime = suiteResult.EndTime,
                    Status = suiteResult.Status,
                    ErrorMessage = suiteResult.ErrorMessage
                });
                
                _logger.LogError($"Benchmark suite failed: {ex.Message}");
                return suiteResult;
            }
        }

        public List<BenchmarkResult> GetBenchmarkResults()
        {
            lock (_lockObject)
            {
                return new List<BenchmarkResult>(_benchmarkResults);
            }
        }

        public void ClearBenchmarkResults()
        {
            lock (_lockObject)
            {
                _benchmarkResults.Clear();
            }
            _logger.LogInfo("Benchmark results cleared");
        }

        public async Task ExportBenchmarkResultsAsync(string filePath)
        {
            try
            {
                var content = "Benchmark Results\n";
                content += "================\n\n";

                lock (_lockObject)
                {
                    foreach (var result in _benchmarkResults)
                    {
                        content += $"Benchmark: {result.BenchmarkName}\n";
                        content += $"Start Time: {result.StartTime:yyyy-MM-dd HH:mm:ss}\n";
                        content += $"End Time: {result.EndTime:yyyy-MM-dd HH:mm:ss}\n";
                        content += $"Duration: {result.Duration}\n";
                        content += $"Status: {result.Status}\n";
                        content += $"Score: {result.Score:F2}\n";
                        
                        if (!string.IsNullOrEmpty(result.ErrorMessage))
                        {
                            content += $"Error: {result.ErrorMessage}\n";
                        }
                        
                        if (result.MemorySizeMB > 0)
                        {
                            content += $"Memory Size: {result.MemorySizeMB} MB\n";
                        }
                        
                        if (result.FileSizeMB > 0)
                        {
                            content += $"File Size: {result.FileSizeMB} MB\n";
                        }
                        
                        if (result.WriteSpeedMBps > 0)
                        {
                            content += $"Write Speed: {result.WriteSpeedMBps:F2} MB/s\n";
                            content += $"Read Speed: {result.ReadSpeedMBps:F2} MB/s\n";
                        }
                        
                        if (result.CompressionRatio > 0)
                        {
                            content += $"Compression Ratio: {result.CompressionRatio:P2}\n";
                        }
                        
                        if (result.Metrics.Any())
                        {
                            content += $"Average CPU Usage: {result.AverageCpuUsage:F2}%\n";
                            content += $"Peak CPU Usage: {result.PeakCpuUsage:F2}%\n";
                            content += $"Average Memory Usage: {result.AverageMemoryUsage:F2} MB\n";
                            content += $"Min Available Memory: {result.MinAvailableMemoryMB:F2} MB\n";
                            content += $"Average Disk Usage: {result.AverageDiskUsage:F2}%\n";
                            content += $"Peak Disk Usage: {result.PeakDiskUsage:F2}%\n";
                        }
                        
                        content += "\n";
                    }
                }
                
                await File.WriteAllTextAsync(filePath, content);
                _logger.LogInfo($"Benchmark results exported to {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to export benchmark results: {ex.Message}");
                throw;
            }
        }

        public BenchmarkComparison CompareWithBaseline(BenchmarkResult currentResult, BenchmarkResult baselineResult)
        {
            var comparison = new BenchmarkComparison
            {
                CurrentResult = currentResult,
                BaselineResult = baselineResult,
                ComparisonTime = DateTime.UtcNow
            };

            if (baselineResult != null && currentResult != null)
            {
                comparison.ScoreImprovement = ((currentResult.Score - baselineResult.Score) / baselineResult.Score) * 100;
                
                if (currentResult.AverageCpuUsage > 0 && baselineResult.AverageCpuUsage > 0)
                {
                    comparison.CpuEfficiency = ((baselineResult.AverageCpuUsage - currentResult.AverageCpuUsage) / baselineResult.AverageCpuUsage) * 100;
                }
                
                if (currentResult.AverageMemoryUsage > 0 && baselineResult.AverageMemoryUsage > 0)
                {
                    comparison.MemoryEfficiency = ((baselineResult.AverageMemoryUsage - currentResult.AverageMemoryUsage) / baselineResult.AverageMemoryUsage) * 100;
                }
            }

            return comparison;
        }
    }

    public class BenchmarkResult
    {
        public string BenchmarkName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public BenchmarkStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public double Score { get; set; }
        public List<PerformanceMetrics> Metrics { get; set; }
        
        // Specific benchmark properties
        public int MemorySizeMB { get; set; }
        public int FileSizeMB { get; set; }
        public double WriteSpeedMBps { get; set; }
        public double ReadSpeedMBps { get; set; }
        public double CompressionRatio { get; set; }
        
        // Statistics
        public double AverageCpuUsage { get; set; }
        public double PeakCpuUsage { get; set; }
        public double AverageMemoryUsage { get; set; }
        public double MinAvailableMemoryMB { get; set; }
        public double AverageDiskUsage { get; set; }
        public double PeakDiskUsage { get; set; }
    }

    public class BenchmarkSuiteResult
    {
        public string SuiteName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public BenchmarkStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public double OverallScore { get; set; }
        public List<BenchmarkResult> IndividualResults { get; set; }
    }

    public class BenchmarkProgressEventArgs : EventArgs
    {
        public string BenchmarkName { get; set; }
        public int ProgressPercentage { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public double AllocatedMemoryMB { get; set; }
        public long BytesProcessed { get; set; }
        public int CompletedBenchmarks { get; set; }
        public int TotalBenchmarks { get; set; }
    }

    public class BenchmarkComparison
    {
        public BenchmarkResult CurrentResult { get; set; }
        public BenchmarkResult BaselineResult { get; set; }
        public DateTime ComparisonTime { get; set; }
        public double ScoreImprovement { get; set; } // Percentage improvement
        public double CpuEfficiency { get; set; } // Percentage reduction in CPU usage
        public double MemoryEfficiency { get; set; } // Percentage reduction in memory usage
    }

    public enum BenchmarkStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed
    }
}