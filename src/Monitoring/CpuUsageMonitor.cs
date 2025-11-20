using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RamOptimizer.Monitoring
{
    public class CpuUsageMonitor : IDisposable
    {
        private readonly PerformanceCounter _cpuCounter;
        private readonly Timer _timer;
        private readonly int _intervalMs;
        private bool _isMonitoring = false;
        private readonly object _lockObject = new object();

        public event EventHandler<CpuUsageInfo> CpuUsageUpdated;

        public CpuUsageMonitor(int intervalMs = 1000)
        {
            _intervalMs = intervalMs;
            
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _timer = new Timer(OnTimerTick, null, Timeout.Infinite, intervalMs);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize CPU Usage Monitor", ex);
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
                float cpuUsage = _cpuCounter.NextValue();
                var data = new CpuUsageInfo
                {
                    UsagePercentage = cpuUsage,
                    Timestamp = DateTime.UtcNow
                };

                CpuUsageUpdated?.Invoke(this, data);
            }
            catch (Exception)
            {
                // Silently ignore errors in timer callback
            }
        }

        public CpuUsageInfo GetCurrentCpuUsage()
        {
            try
            {
                float cpuUsage = _cpuCounter.NextValue();
                return new CpuUsageInfo
                {
                    UsagePercentage = cpuUsage,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                return new CpuUsageInfo
                {
                    UsagePercentage = 0,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<CpuUsageInfo> GetAverageCpuUsageAsync(int sampleCount = 10, int sampleIntervalMs = 100)
        {
            try
            {
                float totalUsage = 0;
                for (int i = 0; i < sampleCount; i++)
                {
                    totalUsage += _cpuCounter.NextValue();
                    await Task.Delay(sampleIntervalMs);
                }

                return new CpuUsageInfo
                {
                    UsagePercentage = totalUsage / sampleCount,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                return new CpuUsageInfo
                {
                    UsagePercentage = 0,
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
                _timer?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class CpuUsageInfo
    {
        public float UsagePercentage { get; set; }
        public DateTime Timestamp { get; set; }
    }
}