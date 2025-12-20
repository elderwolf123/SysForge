using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RamOptimizer.Monitoring
{
    public class CpuUsagePatternMonitor : IDisposable
    {
        private readonly PerformanceCounter _cpuCounter;
        private readonly Timer _timer;
        private readonly int _intervalMs;
        private readonly List<CpuUsageHistoryData> _usageHistory;
        private readonly int _historySize;
        private bool _isMonitoring = false;
        private readonly object _lockObject = new object();

        public event EventHandler<CpuUsagePatternData> CpuUsagePatternUpdated;

        public CpuUsagePatternMonitor(int intervalMs = 1000, int historySize = 60)
        {
            _intervalMs = intervalMs;
            _historySize = historySize;
            _usageHistory = new List<CpuUsageHistoryData>();
            
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _timer = new Timer(OnTimerTick, null, Timeout.Infinite, _intervalMs);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize CPU Usage Pattern Monitor", ex);
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
                var data = new CpuUsageHistoryData
                {
                    UsagePercentage = cpuUsage,
                    Timestamp = DateTime.UtcNow
                };

                // Add to history
                lock (_lockObject)
                {
                    _usageHistory.Add(data);
                    if (_usageHistory.Count > _historySize)
                    {
                        _usageHistory.RemoveAt(0);
                    }
                }

                // Analyze pattern and raise event
                var patternData = AnalyzeUsagePattern();
                CpuUsagePatternUpdated?.Invoke(this, patternData);
            }
            catch (Exception)
            {
                // Silently ignore errors in timer callback
            }
        }

        private CpuUsagePatternData AnalyzeUsagePattern()
        {
            lock (_lockObject)
            {
                if (_usageHistory.Count == 0)
                {
                    return new CpuUsagePatternData
                    {
                        CurrentUsage = 0,
                        AverageUsage = 0,
                        PeakUsage = 0,
                        Trend = CpuUsageTrend.Stable,
                        Pattern = CpuUsagePattern.Idle,
                        Timestamp = DateTime.UtcNow
                    };
                }

                var currentUsage = _usageHistory.Last().UsagePercentage;
                var averageUsage = _usageHistory.Average(d => d.UsagePercentage);
                var peakUsage = _usageHistory.Max(d => d.UsagePercentage);
                
                // Determine pattern based on usage levels
                CpuUsagePattern pattern;
                if (averageUsage < 20)
                    pattern = CpuUsagePattern.Idle;
                else if (averageUsage < 50)
                    pattern = CpuUsagePattern.Light;
                else if (averageUsage < 80)
                    pattern = CpuUsagePattern.Moderate;
                else
                    pattern = CpuUsagePattern.Heavy;

                // Determine trend based on recent history
                CpuUsageTrend trend = CpuUsageTrend.Stable;
                if (_usageHistory.Count >= 5)
                {
                    var recent = _usageHistory.Skip(Math.Max(0, _usageHistory.Count - 5)).Take(5).ToList();
                    var oldest = recent.First().UsagePercentage;
                    var newest = recent.Last().UsagePercentage;
                    
                    if (newest > oldest + 10)
                        trend = CpuUsageTrend.Increasing;
                    else if (newest < oldest - 10)
                        trend = CpuUsageTrend.Decreasing;
                }

                return new CpuUsagePatternData
                {
                    CurrentUsage = currentUsage,
                    AverageUsage = averageUsage,
                    PeakUsage = peakUsage,
                    Trend = trend,
                    Pattern = pattern,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public CpuUsagePatternData GetCurrentUsagePattern()
        {
            lock (_lockObject)
            {
                return AnalyzeUsagePattern();
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

    public class CpuUsageHistoryData
    {
        public float UsagePercentage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CpuUsagePatternData
    {
        public float CurrentUsage { get; set; }
        public float AverageUsage { get; set; }
        public float PeakUsage { get; set; }
        public CpuUsageTrend Trend { get; set; }
        public CpuUsagePattern Pattern { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum CpuUsageTrend
    {
        Stable,
        Increasing,
        Decreasing
    }

    public enum CpuUsagePattern
    {
        Idle,
        Light,
        Moderate,
        Heavy
    }
}