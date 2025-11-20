using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RamOptimizer.Monitoring
{
    public class PerformanceMonitor : IDisposable
    {
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;
        private readonly PerformanceCounter _diskCounter;
        private readonly Timer _timer;
        private readonly int _intervalMs;
        private bool _isMonitoring = false;
        private readonly object _lockObject = new object();

        public event EventHandler<PerformanceData> PerformanceDataUpdated;

        public PerformanceMonitor(int intervalMs = 1000)
        {
            _intervalMs = intervalMs;
            
            try
            {
                // Initialize performance counters
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                
                // Initialize timer for periodic updates
                _timer = new Timer(OnTimerTick, null, Timeout.Infinite, _intervalMs);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize Performance Monitor", ex);
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

        private void OnTimerTick(object state)
        {
            try
            {
                var data = new PerformanceData
                {
                    CpuUsage = _cpuCounter.NextValue(),
                    AvailableMemoryMB = _memoryCounter.NextValue(),
                    DiskUsage = _diskCounter.NextValue(),
                    Timestamp = DateTime.UtcNow
                };

                PerformanceDataUpdated?.Invoke(this, data);
            }
            catch (Exception)
            {
                // Silently ignore errors in timer callback
            }
        }

        public PerformanceData GetCurrentPerformanceData()
        {
            try
            {
                return new PerformanceData
                {
                    CpuUsage = _cpuCounter.NextValue(),
                    AvailableMemoryMB = _memoryCounter.NextValue(),
                    DiskUsage = _diskCounter.NextValue(),
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                return new PerformanceData
                {
                    CpuUsage = 0,
                    AvailableMemoryMB = 0,
                    DiskUsage = 0,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<PerformanceData> GetAveragePerformanceDataAsync(int sampleCount = 10, int sampleIntervalMs = 100)
        {
            try
            {
                float totalCpu = 0, totalMemory = 0, totalDisk = 0;
                
                for (int i = 0; i < sampleCount; i++)
                {
                    totalCpu += _cpuCounter.NextValue();
                    totalMemory += _memoryCounter.NextValue();
                    totalDisk += _diskCounter.NextValue();
                    await Task.Delay(sampleIntervalMs);
                }

                return new PerformanceData
                {
                    CpuUsage = totalCpu / sampleCount,
                    AvailableMemoryMB = totalMemory / sampleCount,
                    DiskUsage = totalDisk / sampleCount,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                return new PerformanceData
                {
                    CpuUsage = 0,
                    AvailableMemoryMB = 0,
                    DiskUsage = 0,
                    Timestamp = DateTime.UtcNow
                };
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
                _timer?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class PerformanceData
    {
        public float CpuUsage { get; set; }
        public float AvailableMemoryMB { get; set; }
        public float DiskUsage { get; set; }
        public DateTime Timestamp { get; set; }
    }
}