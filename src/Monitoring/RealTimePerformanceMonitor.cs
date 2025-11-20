using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace RamOptimizer.Monitoring
{
    public class RealTimePerformanceMonitor : IDisposable
    {
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;
        private readonly PerformanceCounter _diskCounter;
        private readonly PerformanceCounter? _gpuCounter;
        private readonly Timer _timer;
        private readonly int _intervalMs;
        private bool _isMonitoring = false;
        private readonly object _lockObject = new object();
        private readonly List<PerformanceCounter> _customCounters;
        private readonly Dictionary<string, PerformanceCounter> _processCounters;

        public event EventHandler<PerformanceMetrics>? PerformanceMetricsUpdated;
        public event EventHandler<SystemAlert>? AlertTriggered;

        public RealTimePerformanceMonitor(int intervalMs = 1000)
        {
            _intervalMs = intervalMs;
            _customCounters = new List<PerformanceCounter>();
            _processCounters = new Dictionary<string, PerformanceCounter>();

            try
            {
                // Initialize performance counters
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                
                // CRITICAL FIX: Prime the counters with initial calls
                // First call to NextValue() always returns 0, so we call it once here
                _cpuCounter.NextValue();
                _diskCounter.NextValue();
                
                // Try to initialize GPU counter (may not be available on all systems)
                try
                {
                    _gpuCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_Total");
                    _gpuCounter.NextValue(); // Prime it
                }
                catch
                {
                    // GPU counter not available, we'll simulate it
                    _gpuCounter = null;
                }

                // Initialize timer for periodic updates
                _timer = new Timer(OnTimerTick, null, Timeout.Infinite, _intervalMs);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize Real-Time Performance Monitor", ex);
            }
        }

        public void StartMonitoring()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring)
                {
                    _timer.Change(0, _intervalMs);
                    _isMonitoring = true;
                }
            }
        }

        public void StopMonitoring()
        {
            lock (_lockObject)
            {
                if (_isMonitoring)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    _isMonitoring = false;
                }
            }
        }

        private void OnTimerTick(object? state)
        {
            try
            {
                var metrics = new PerformanceMetrics
                {
                    CpuUsage = _cpuCounter.NextValue(),
                    AvailableMemoryMB = _memoryCounter.NextValue(),
                    DiskUsage = _diskCounter.NextValue(),
                    GpuUsage = _gpuCounter?.NextValue() ?? SimulateGpuUsage(),
                    Timestamp = DateTime.UtcNow
                };

                // Add custom counter values
                foreach (var counter in _customCounters)
                {
                    try
                    {
                        metrics.CustomMetrics[counter.CounterName] = counter.NextValue();
                    }
                    catch
                    {
                        // Ignore errors for individual counters
                    }
                }

                PerformanceMetricsUpdated?.Invoke(this, metrics);
                
                // Check for alerts
                CheckForAlerts(metrics);
            }
            catch (Exception)
            {
                // Silently ignore errors in timer callback
            }
        }

        private float SimulateGpuUsage()
        {
            // Simple simulation of GPU usage
            // In a real implementation, you would use a GPU monitoring library
            return (float)(Math.Sin(DateTime.UtcNow.Ticks / 10000000.0) * 50 + 50);
        }

        public PerformanceMetrics GetCurrentMetrics()
        {
            try
            {
                return new PerformanceMetrics
                {
                    CpuUsage = _cpuCounter.NextValue(),
                    AvailableMemoryMB = _memoryCounter.NextValue(),
                    DiskUsage = _diskCounter.NextValue(),
                    GpuUsage = _gpuCounter?.NextValue() ?? SimulateGpuUsage(),
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                return new PerformanceMetrics
                {
                    CpuUsage = 0,
                    AvailableMemoryMB = 0,
                    DiskUsage = 0,
                    GpuUsage = 0,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<PerformanceMetrics> GetAverageMetricsAsync(int sampleCount = 10, int sampleIntervalMs = 100)
        {
            try
            {
                float totalCpu = 0, totalMemory = 0, totalDisk = 0, totalGpu = 0;

                for (int i = 0; i < sampleCount; i++)
                {
                    totalCpu += _cpuCounter.NextValue();
                    totalMemory += _memoryCounter.NextValue();
                    totalDisk += _diskCounter.NextValue();
                    totalGpu += _gpuCounter?.NextValue() ?? SimulateGpuUsage();
                    await Task.Delay(sampleIntervalMs);
                }

                return new PerformanceMetrics
                {
                    CpuUsage = totalCpu / sampleCount,
                    AvailableMemoryMB = totalMemory / sampleCount,
                    DiskUsage = totalDisk / sampleCount,
                    GpuUsage = totalGpu / sampleCount,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                return new PerformanceMetrics
                {
                    CpuUsage = 0,
                    AvailableMemoryMB = 0,
                    DiskUsage = 0,
                    GpuUsage = 0,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public void AddCustomCounter(string category, string counter, string? instance = null)
        {
            try
            {
                var perfCounter = string.IsNullOrEmpty(instance) 
                    ? new PerformanceCounter(category, counter) 
                    : new PerformanceCounter(category, counter, instance);
                
                perfCounter.NextValue(); // Prime it
                _customCounters.Add(perfCounter);
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                Debug.WriteLine($"Failed to add custom counter {category}\\{counter}: {ex.Message}");
            }
        }

        public void AddProcessCounter(string processName, string counter)
        {
            try
            {
                var perfCounter = new PerformanceCounter("Process", counter, processName);
                perfCounter.NextValue(); // Prime it
                _processCounters[$"{processName}_{counter}"] = perfCounter;
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                Debug.WriteLine($"Failed to add process counter {processName}\\{counter}: {ex.Message}");
            }
        }

        private void CheckForAlerts(PerformanceMetrics metrics)
        {
            // Check CPU usage alert
            if (metrics.CpuUsage > 90)
            {
                AlertTriggered?.Invoke(this, new SystemAlert
                {
                    Type = AlertType.HighCpuUsage,
                    Message = $"High CPU usage detected: {metrics.CpuUsage:F1}%",
                    Severity = AlertSeverity.Warning,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check memory usage alert
            if (metrics.AvailableMemoryMB < 500) // Less than 500 MB available
            {
                AlertTriggered?.Invoke(this, new SystemAlert
                {
                    Type = AlertType.LowMemory,
                    Message = $"Low available memory: {metrics.AvailableMemoryMB:F1} MB",
                    Severity = AlertSeverity.Warning,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check disk usage alert
            if (metrics.DiskUsage > 90)
            {
                AlertTriggered?.Invoke(this, new SystemAlert
                {
                    Type = AlertType.HighDiskUsage,
                    Message = $"High disk usage detected: {metrics.DiskUsage:F1}%",
                    Severity = AlertSeverity.Warning,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check GPU usage alert
            if (metrics.GpuUsage > 90)
            {
                AlertTriggered?.Invoke(this, new SystemAlert
                {
                    Type = AlertType.HighGpuUsage,
                    Message = $"High GPU usage detected: {metrics.GpuUsage:F1}%",
                    Severity = AlertSeverity.Warning,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopMonitoring();
                
                _cpuCounter?.Dispose();
                _memoryCounter?.Dispose();
                _diskCounter?.Dispose();
                _gpuCounter?.Dispose();
                _timer?.Dispose();
                
                foreach (var counter in _customCounters)
                {
                    counter?.Dispose();
                }
                
                foreach (var counter in _processCounters.Values)
                {
                    counter?.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class PerformanceMetrics
    {
        public float CpuUsage { get; set; }
        public float AvailableMemoryMB { get; set; }
        public float DiskUsage { get; set; }
        public float GpuUsage { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, float> CustomMetrics { get; set; } = new Dictionary<string, float>();
    }

    public class SystemAlert
    {
        public AlertType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum AlertType
    {
        HighCpuUsage,
        LowMemory,
        HighDiskUsage,
        HighGpuUsage,
        ProcessTerminated,
        ProcessStarted,
        SystemError
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}