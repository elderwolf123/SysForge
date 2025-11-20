using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using RamOptimizer.Monitoring;
using RamOptimizer.Logging;

namespace RamOptimizer.Testing
{
    public class StressTestingFramework
    {
        private readonly ComprehensiveLogger _logger;
        private readonly RealTimePerformanceMonitor _monitor;
        private readonly List<StressTestResult> _testResults;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isTestRunning = false;

        public event EventHandler<TestProgressEventArgs> TestProgressUpdated;
        public event EventHandler<StressTestResult> TestCompleted;

        public StressTestingFramework(ComprehensiveLogger logger, RealTimePerformanceMonitor monitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _testResults = new List<StressTestResult>();
        }

        public async Task RunCpuStressTestAsync(int durationMinutes = 10, int cpuLoadPercentage = 100)
        {
            if (_isTestRunning)
            {
                throw new InvalidOperationException("A test is already running");
            }

            _isTestRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            var testName = $"CPU Stress Test ({cpuLoadPercentage}%)";
            _logger.LogInfo($"Starting {testName} for {durationMinutes} minutes");
            
            var result = new StressTestResult
            {
                TestName = testName,
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(durationMinutes),
                Metrics = new List<PerformanceMetrics>()
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Start monitoring
                _monitor.PerformanceMetricsUpdated += (s, e) => result.Metrics.Add(e);
                
                // Run CPU stress workers
                var workers = new List<Task>();
                var workerCount = Environment.ProcessorCount;
                
                for (int i = 0; i < workerCount; i++)
                {
                    workers.Add(RunCpuWorkerAsync(cpuLoadPercentage, _cancellationTokenSource.Token));
                }
                
                // Monitor progress
                while (stopwatch.Elapsed < result.Duration && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    
                    var progress = (int)((stopwatch.Elapsed.TotalSeconds / result.Duration.TotalSeconds) * 100);
                    TestProgressUpdated?.Invoke(this, new TestProgressEventArgs
                    {
                        TestName = testName,
                        ProgressPercentage = progress,
                        ElapsedTime = stopwatch.Elapsed
                    });
                }
                
                // Stop workers
                _cancellationTokenSource.Cancel();
                await Task.WhenAll(workers);
                
                result.EndTime = DateTime.UtcNow;
                result.Status = TestStatus.Completed;
                
                // Calculate statistics
                if (result.Metrics.Any())
                {
                    result.AverageCpuUsage = result.Metrics.Average(m => m.CpuUsage);
                    result.PeakCpuUsage = result.Metrics.Max(m => m.CpuUsage);
                    result.MinAvailableMemoryMB = result.Metrics.Min(m => m.AvailableMemoryMB);
                }
                
                _testResults.Add(result);
                TestCompleted?.Invoke(this, result);
                
                _logger.LogInfo($"{testName} completed successfully");
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.Status = TestStatus.Failed;
                result.ErrorMessage = ex.Message;
                
                _testResults.Add(result);
                TestCompleted?.Invoke(this, result);
                
                _logger.LogError($"Failed to complete {testName}: {ex.Message}");
            }
            finally
            {
                _isTestRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task RunMemoryStressTestAsync(int durationMinutes = 10, long memorySizeMB = 1024)
        {
            if (_isTestRunning)
            {
                throw new InvalidOperationException("A test is already running");
            }

            _isTestRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            var testName = $"Memory Stress Test ({memorySizeMB} MB)";
            _logger.LogInfo($"Starting {testName} for {durationMinutes} minutes");
            
            var result = new StressTestResult
            {
                TestName = testName,
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(durationMinutes),
                Metrics = new List<PerformanceMetrics>()
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var memoryBlocks = new List<byte[]>();
                
                // Start monitoring
                _monitor.PerformanceMetricsUpdated += (s, e) => result.Metrics.Add(e);
                
                // Allocate memory blocks
                var blockSize = 10 * 1024 * 1024; // 10 MB blocks
                var totalBlocks = (int)(memorySizeMB * 1024 * 1024 / blockSize);
                
                for (int i = 0; i < totalBlocks && !_cancellationTokenSource.Token.IsCancellationRequested; i++)
                {
                    try
                    {
                        var block = new byte[blockSize];
                        // Fill with random data to prevent optimization
                        new Random().NextBytes(block);
                        memoryBlocks.Add(block);
                    }
                    catch (OutOfMemoryException)
                    {
                        _logger.LogWarning("Out of memory during stress test");
                        break;
                    }
                }
                
                // Monitor progress
                while (stopwatch.Elapsed < result.Duration && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    
                    var progress = (int)((stopwatch.Elapsed.TotalSeconds / result.Duration.TotalSeconds) * 100);
                    TestProgressUpdated?.Invoke(this, new TestProgressEventArgs
                    {
                        TestName = testName,
                        ProgressPercentage = progress,
                        ElapsedTime = stopwatch.Elapsed
                    });
                }
                
                // Clean up memory
                memoryBlocks.Clear();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                result.EndTime = DateTime.UtcNow;
                result.Status = TestStatus.Completed;
                
                // Calculate statistics
                if (result.Metrics.Any())
                {
                    result.AverageMemoryUsage = result.Metrics.Average(m => m.AvailableMemoryMB);
                    result.MinAvailableMemoryMB = result.Metrics.Min(m => m.AvailableMemoryMB);
                    result.PeakMemoryUsage = result.Metrics.Max(m => m.AvailableMemoryMB);
                }
                
                _testResults.Add(result);
                TestCompleted?.Invoke(this, result);
                
                _logger.LogInfo($"{testName} completed successfully");
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.Status = TestStatus.Failed;
                result.ErrorMessage = ex.Message;
                
                _testResults.Add(result);
                TestCompleted?.Invoke(this, result);
                
                _logger.LogError($"Failed to complete {testName}: {ex.Message}");
            }
            finally
            {
                _isTestRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task RunDiskStressTestAsync(int durationMinutes = 10, string testDirectory = null)
        {
            if (_isTestRunning)
            {
                throw new InvalidOperationException("A test is already running");
            }

            _isTestRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            var testName = "Disk Stress Test";
            _logger.LogInfo($"Starting {testName} for {durationMinutes} minutes");
            
            var result = new StressTestResult
            {
                TestName = testName,
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(durationMinutes),
                Metrics = new List<PerformanceMetrics>()
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Use temp directory if none specified
                if (string.IsNullOrEmpty(testDirectory))
                {
                    testDirectory = Path.GetTempPath();
                }
                
                var testFile = Path.Combine(testDirectory, $"disk_stress_test_{Guid.NewGuid()}.dat");
                
                // Start monitoring
                _monitor.PerformanceMetricsUpdated += (s, e) => result.Metrics.Add(e);
                
                // Run disk stress in background
                var diskTask = Task.Run(async () =>
                {
                    var random = new Random();
                    var buffer = new byte[1024 * 1024]; // 1 MB buffer
                    
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // Write random data
                            random.NextBytes(buffer);
                            await File.WriteAllBytesAsync(testFile, buffer, _cancellationTokenSource.Token);
                            
                            // Read data back
                            var readData = await File.ReadAllBytesAsync(testFile, _cancellationTokenSource.Token);
                            
                            // Delete file
                            if (File.Exists(testFile))
                            {
                                File.Delete(testFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Disk stress test iteration failed: {ex.Message}");
                        }
                    }
                }, _cancellationTokenSource.Token);
                
                // Monitor progress
                while (stopwatch.Elapsed < result.Duration && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    
                    var progress = (int)((stopwatch.Elapsed.TotalSeconds / result.Duration.TotalSeconds) * 100);
                    TestProgressUpdated?.Invoke(this, new TestProgressEventArgs
                    {
                        TestName = testName,
                        ProgressPercentage = progress,
                        ElapsedTime = stopwatch.Elapsed
                    });
                }
                
                // Stop disk stress
                _cancellationTokenSource.Cancel();
                await diskTask;
                
                // Clean up
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
                
                result.EndTime = DateTime.UtcNow;
                result.Status = TestStatus.Completed;
                
                // Calculate statistics
                if (result.Metrics.Any())
                {
                    result.AverageDiskUsage = result.Metrics.Average(m => m.DiskUsage);
                    result.PeakDiskUsage = result.Metrics.Max(m => m.DiskUsage);
                }
                
                _testResults.Add(result);
                TestCompleted?.Invoke(this, result);
                
                _logger.LogInfo($"{testName} completed successfully");
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.Status = TestStatus.Failed;
                result.ErrorMessage = ex.Message;
                
                _testResults.Add(result);
                TestCompleted?.Invoke(this, result);
                
                _logger.LogError($"Failed to complete {testName}: {ex.Message}");
            }
            finally
            {
                _isTestRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task RunGpuStressTestAsync(int durationMinutes = 10)
        {
            if (_isTestRunning)
            {
                throw new InvalidOperationException("A test is already running");
            }

            _isTestRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            var testName = "GPU Stress Test";
            _logger.LogInfo($"Starting {testName} for {durationMinutes} minutes");
            
            var result = new StressTestResult
            {
                TestName = testName,
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(durationMinutes),
                Metrics = new List<PerformanceMetrics>()
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Start monitoring
                _monitor.PerformanceMetricsUpdated += (s, e) => result.Metrics.Add(e);
                
                // In a real implementation, this would run actual GPU stress workloads
                // For now, we'll simulate GPU usage
                var gpuTask = Task.Run(async () =>
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        // Simulate GPU work
                        var data = new float[1000000];
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i] = (float)Math.Sin(i * 0.001) * (float)Math.Cos(i * 0.002);
                        }
                        
                        await Task.Delay(10, _cancellationTokenSource.Token);
                    }
                }, _cancellationTokenSource.Token);
                
                // Monitor progress
                while (stopwatch.Elapsed < result.Duration && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    
                    var progress = (int)((stopwatch.Elapsed.TotalSeconds / result.Duration.TotalSeconds) * 100);
                    TestProgressUpdated?.Invoke(this, new TestProgressEventArgs
                    {
                        TestName = testName,
                        ProgressPercentage = progress,
                        ElapsedTime = stopwatch.Elapsed
                    });
                }
                
                // Stop GPU stress
                _cancellationTokenSource.Cancel();
                await gpuTask;
                
                result.EndTime = DateTime.UtcNow;
                result.Status = TestStatus.Completed;
                
                // Calculate statistics
                if (result.Metrics.Any())
                {
                    result.AverageGpuUsage = result.Metrics.Average(m => m.GpuUsage);
                    result.PeakGpuUsage = result.Metrics.Max(m => m.GpuUsage);
                }
                
                _testResults.Add(result);
                TestCompleted?.Invoke(this, result);
                
                _logger.LogInfo($"{testName} completed successfully");
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.Status = TestStatus.Failed;
                result.ErrorMessage = ex.Message;
                
                _testResults.Add(result);
                TestCompleted?.Invoke(this, result);
                
                _logger.LogError($"Failed to complete {testName}: {ex.Message}");
            }
            finally
            {
                _isTestRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task RunCpuWorkerAsync(int loadPercentage, CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            
            while (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Restart();
                
                // Work for the specified percentage of time
                while (stopwatch.ElapsedMilliseconds < loadPercentage)
                {
                    // Perform some CPU-intensive work
                    var result = 0;
                    for (int i = 0; i < 1000; i++)
                    {
                        result += i * i;
                    }
                }
                
                // Idle for the remaining time (100 - loadPercentage)
                if (loadPercentage < 100)
                {
                    await Task.Delay(100 - loadPercentage, cancellationToken);
                }
            }
        }

        public List<StressTestResult> GetTestResults()
        {
            return new List<StressTestResult>(_testResults);
        }

        public void ClearTestResults()
        {
            _testResults.Clear();
            _logger.LogInfo("Stress test results cleared");
        }

        public async Task ExportTestResultsAsync(string filePath)
        {
            try
            {
                var content = "Stress Test Results\n";
                content += "==================\n\n";
                
                foreach (var result in _testResults)
                {
                    content += $"Test: {result.TestName}\n";
                    content += $"Start Time: {result.StartTime:yyyy-MM-dd HH:mm:ss}\n";
                    content += $"End Time: {result.EndTime:yyyy-MM-dd HH:mm:ss}\n";
                    content += $"Duration: {result.Duration}\n";
                    content += $"Status: {result.Status}\n";
                    
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        content += $"Error: {result.ErrorMessage}\n";
                    }
                    
                    if (result.Metrics.Any())
                    {
                        content += $"Average CPU Usage: {result.AverageCpuUsage:F2}%\n";
                        content += $"Peak CPU Usage: {result.PeakCpuUsage:F2}%\n";
                        content += $"Min Available Memory: {result.MinAvailableMemoryMB:F2} MB\n";
                        content += $"Average GPU Usage: {result.AverageGpuUsage:F2}%\n";
                        content += $"Peak GPU Usage: {result.PeakGpuUsage:F2}%\n";
                        content += $"Average Disk Usage: {result.AverageDiskUsage:F2}%\n";
                        content += $"Peak Disk Usage: {result.PeakDiskUsage:F2}%\n";
                    }
                    
                    content += "\n";
                }
                
                await File.WriteAllTextAsync(filePath, content);
                _logger.LogInfo($"Test results exported to {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to export test results: {ex.Message}");
                throw;
            }
        }

        public void CancelRunningTest()
        {
            if (_isTestRunning && _cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _logger.LogInfo("Running stress test cancelled");
            }
        }
    }

    public class StressTestResult
    {
        public string TestName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public TestStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public List<PerformanceMetrics> Metrics { get; set; }
        
        // Statistics
        public double AverageCpuUsage { get; set; }
        public double PeakCpuUsage { get; set; }
        public double AverageMemoryUsage { get; set; }
        public double PeakMemoryUsage { get; set; }
        public double MinAvailableMemoryMB { get; set; }
        public double AverageGpuUsage { get; set; }
        public double PeakGpuUsage { get; set; }
        public double AverageDiskUsage { get; set; }
        public double PeakDiskUsage { get; set; }
    }

    public class TestProgressEventArgs : EventArgs
    {
        public string TestName { get; set; }
        public int ProgressPercentage { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }

    public enum TestStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Cancelled
    }
}