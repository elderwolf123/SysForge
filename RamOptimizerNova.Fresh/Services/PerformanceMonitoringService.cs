using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RamOptimizerNova.Models;

namespace RamOptimizerNova.Services;

/// <summary>
/// Service for monitoring application performance and system metrics
/// </summary>
public class PerformanceMonitoringService : IPerformanceMonitoringService, IDisposable
{
    private readonly IMetricsService _metricsService;
    private readonly ILoggerService _loggerService;
    private readonly IHardwareService _hardwareService;
    private readonly INetworkService _networkService;
    private readonly ICompressionService _compressionService;
    private readonly IOptimizationService _optimizationService;

    private readonly PerformanceMetricsModel _currentMetrics = new();
    private readonly List<PerformanceSnapshot> _performanceHistory = new();
    private readonly object _lock = new();
    private readonly Timer _performanceTimer;
    private readonly Timer _historyCleanupTimer;
    private readonly int _maxHistoryEntries = 100;
    private bool _isRunning = false;
    private bool _isDisposed = false;
    private long _totalMemoryAllocated = 0;
    private long _totalMemoryFreed = 0;
    private int _totalOperations = 0;
    private int _errorCount = 0;
    private DateTime _lastErrorTime = DateTime.MinValue;
    private readonly TimeSpan _errorResetInterval = TimeSpan.FromMinutes(5);

    // Performance thresholds
    private const double HighMemoryThreshold = 85.0;
    private const double HighCpuThreshold = 80.0;
    private const double HighNetworkThreshold = 75.0;
    private const double HighTemperatureThreshold = 85.0;

    public event EventHandler<PerformanceMetricsUpdatedEventArgs>? PerformanceMetricsUpdated;
    public event EventHandler<PerformanceAlertEventArgs>? PerformanceAlert;
    public event EventHandler<PerformanceErrorEventArgs>? PerformanceError;

    public PerformanceMonitoringService(
        IMetricsService metricsService,
        ILoggerService loggerService,
        IHardwareService hardwareService,
        INetworkService networkService,
        ICompressionService compressionService,
        IOptimizationService optimizationService)
    {
        _metricsService = metricsService;
        _loggerService = loggerService;
        _hardwareService = hardwareService;
        _networkService = networkService;
        _compressionService = compressionService;
        _optimizationService = optimizationService;

        // Initialize timers
        _performanceTimer = new Timer(OnPerformanceTimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        _historyCleanupTimer = new Timer(OnHistoryCleanupTimerTick, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        // Subscribe to events
        _metricsService.MetricsUpdated += OnMetricsUpdated;
        _metricsService.MetricsError += OnMetricsError;
        _hardwareService.HardwareStatusChanged += OnHardwareStatusChanged;
        _hardwareService.HardwareError += OnHardwareError;
        _networkService.NetworkStatusChanged += OnNetworkStatusChanged;
        _networkService.NetworkError += OnNetworkError;
        _compressionService.CompressionStatusChanged += OnCompressionStatusChanged;
        _compressionService.CompressionError += OnCompressionError;
        _optimizationService.OptimizationStatusChanged += OnOptimizationStatusChanged;
        _optimizationService.OptimizationError += OnOptimizationError;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _loggerService.LogAsync("Initializing Performance Monitoring Service");
            
            // Start monitoring
            _isRunning = true;
            
            // Take initial snapshot
            await TakePerformanceSnapshotAsync();
            
            await _loggerService.LogAsync("Performance Monitoring Service initialized successfully");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error initializing Performance Monitoring Service: {ex.Message}");
            throw;
        }
    }

    public async Task StartMonitoringAsync()
    {
        try
        {
            if (!_isRunning)
            {
                _isRunning = true;
                await _loggerService.LogAsync("Performance monitoring started");
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error starting performance monitoring: {ex.Message}");
            throw;
        }
    }

    public async Task StopMonitoringAsync()
    {
        try
        {
            if (_isRunning)
            {
                _isRunning = false;
                await _loggerService.LogAsync("Performance monitoring stopped");
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error stopping performance monitoring: {ex.Message}");
            throw;
        }
    }

    public PerformanceMetricsModel GetCurrentMetrics()
    {
        lock (_lock)
        {
            return new PerformanceMetricsModel
            {
                Timestamp = _currentMetrics.Timestamp,
                MemoryUsage = _currentMetrics.MemoryUsage,
                CpuUsage = _currentMetrics.CpuUsage,
                NetworkUsage = _currentMetrics.NetworkUsage,
                Temperature = _currentMetrics.Temperature,
                DiskUsage = _currentMetrics.DiskUsage,
                ProcessCount = _currentMetrics.ProcessCount,
                ThreadCount = _currentMetrics.ThreadCount,
                HandleCount = _currentMetrics.HandleCount,
                GcCollections = _currentMetrics.GcCollections,
                GcMemory = _currentMetrics.GcMemory,
                TotalMemoryAllocated = _totalMemoryAllocated,
                TotalMemoryFreed = _totalMemoryFreed,
                TotalOperations = _totalOperations,
                ErrorCount = _errorCount,
                Uptime = _currentMetrics.Uptime,
                PerformanceScore = _currentMetrics.PerformanceScore,
                AlertCount = _currentMetrics.AlertCount,
                LastErrorTime = _lastErrorTime
            };
        }
    }

    public List<PerformanceSnapshot> GetPerformanceHistory(int count = 10)
    {
        lock (_lock)
        {
            return _performanceHistory
                .OrderByDescending(s => s.Timestamp)
                .Take(count)
                .ToList();
        }
    }

    public async Task<PerformanceAnalysisReport> AnalyzePerformanceAsync(TimeSpan timeRange)
    {
        try
        {
            var endTime = DateTime.Now;
            var startTime = endTime - timeRange;
            
            lock (_lock)
            {
                var relevantSnapshots = _performanceHistory
                    .Where(s => s.Timestamp >= startTime && s.Timestamp <= endTime)
                    .ToList();
                
                if (relevantSnapshots.Count == 0)
                {
                    return new PerformanceAnalysisReport
                    {
                        TimeRange = timeRange,
                        SnapshotCount = 0,
                        AverageMemoryUsage = 0,
                        AverageCpuUsage = 0,
                        AverageNetworkUsage = 0,
                        AverageTemperature = 0,
                        PeakMemoryUsage = 0,
                        PeakCpuUsage = 0,
                        PeakNetworkUsage = 0,
                        PeakTemperature = 0,
                        MemoryTrend = PerformanceTrend.Stable,
                        CpuTrend = PerformanceTrend.Stable,
                        NetworkTrend = PerformanceTrend.Stable,
                        TemperatureTrend = PerformanceTrend.Stable,
                        AlertCount = 0,
                        ErrorCount = _errorCount,
                        Recommendations = new List<string> { "No performance data available for the specified time range" }
                    };
                }

                var report = new PerformanceAnalysisReport
                {
                    TimeRange = timeRange,
                    SnapshotCount = relevantSnapshots.Count,
                    AverageMemoryUsage = relevantSnapshots.Average(s => s.MemoryUsage),
                    AverageCpuUsage = relevantSnapshots.Average(s => s.CpuUsage),
                    AverageNetworkUsage = relevantSnapshots.Average(s => s.NetworkUsage),
                    AverageTemperature = relevantSnapshots.Average(s => s.Temperature),
                    PeakMemoryUsage = relevantSnapshots.Max(s => s.MemoryUsage),
                    PeakCpuUsage = relevantSnapshots.Max(s => s.CpuUsage),
                    PeakNetworkUsage = relevantSnapshots.Max(s => s.NetworkUsage),
                    PeakTemperature = relevantSnapshots.Max(s => s.Temperature),
                    AlertCount = relevantSnapshots.Sum(s => s.AlertCount),
                    ErrorCount = _errorCount,
                    Recommendations = new List<string>()
                };

                // Calculate trends
                report.MemoryTrend = CalculateTrend(relevantSnapshots.Select(s => s.MemoryUsage));
                report.CpuTrend = CalculateTrend(relevantSnapshots.Select(s => s.CpuUsage));
                report.NetworkTrend = CalculateTrend(relevantSnapshots.Select(s => s.NetworkUsage));
                report.TemperatureTrend = CalculateTrend(relevantSnapshots.Select(s => s.Temperature));

                // Generate recommendations
                GenerateRecommendations(report);

                return report;
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error analyzing performance: {ex.Message}");
            return new PerformanceAnalysisReport
            {
                TimeRange = timeRange,
                SnapshotCount = 0,
                Recommendations = new List<string> { $"Error analyzing performance: {ex.Message}" }
            };
        }
    }

    public async Task<PerformanceHealthStatus> GetHealthStatusAsync()
    {
        try
        {
            lock (_lock)
            {
                var healthStatus = new PerformanceHealthStatus
                {
                    OverallHealth = HealthStatus.Good,
                    MemoryHealth = GetResourceHealth(_currentMetrics.MemoryUsage, HighMemoryThreshold),
                    CpuHealth = GetResourceHealth(_currentMetrics.CpuUsage, HighCpuThreshold),
                    NetworkHealth = GetResourceHealth(_currentMetrics.NetworkUsage, HighNetworkThreshold),
                    TemperatureHealth = GetResourceHealth(_currentMetrics.Temperature, HighTemperatureThreshold),
                    ErrorRate = CalculateErrorRate(),
                    Uptime = _currentMetrics.Uptime,
                    LastErrorTime = _lastErrorTime,
                    ActiveAlerts = _currentMetrics.AlertCount
                };

                // Determine overall health
                if (healthStatus.MemoryHealth == HealthStatus.Critical ||
                    healthStatus.CpuHealth == HealthStatus.Critical ||
                    healthStatus.TemperatureHealth == HealthStatus.Critical)
                {
                    healthStatus.OverallHealth = HealthStatus.Critical;
                }
                else if (healthStatus.MemoryHealth == HealthStatus.Warning ||
                         healthStatus.CpuHealth == HealthStatus.Warning ||
                         healthStatus.NetworkHealth == HealthStatus.Warning ||
                         healthStatus.TemperatureHealth == HealthStatus.Warning)
                {
                    healthStatus.OverallHealth = HealthStatus.Warning;
                }

                return healthStatus;
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting health status: {ex.Message}");
            return new PerformanceHealthStatus
            {
                OverallHealth = HealthStatus.Unknown,
                Recommendations = new List<string> { $"Error getting health status: {ex.Message}" }
            };
        }
    }

    public async Task ForceSnapshotAsync()
    {
        try
        {
            await TakePerformanceSnapshotAsync();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error forcing performance snapshot: {ex.Message}");
            throw;
        }
    }

    private async void OnPerformanceTimerTick(object? state)
    {
        try
        {
            if (_isRunning)
            {
                await TakePerformanceSnapshotAsync();
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error in performance timer tick: {ex.Message}");
        }
    }

    private async Task TakePerformanceSnapshotAsync()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Get current metrics
            var metrics = await _metricsService.GetCurrentMetricsAsync();
            var hardwareInfo = await _hardwareService.GetHardwareInfoAsync();
            var networkStats = await _networkService.GetNetworkStatsAsync();
            
            // Calculate performance score
            var performanceScore = CalculatePerformanceScore(
                metrics.MemoryUsagePercentage,
                metrics.CpuUsagePercentage,
                networkStats.NetworkLoadPercentage,
                hardwareInfo.Temperature
            );

            // Update current metrics
            lock (_lock)
            {
                _currentMetrics.Timestamp = DateTime.Now;
                _currentMetrics.MemoryUsage = metrics.MemoryUsagePercentage;
                _currentMetrics.CpuUsage = metrics.CpuUsagePercentage;
                _currentMetrics.NetworkUsage = networkStats.NetworkLoadPercentage;
                _currentMetrics.Temperature = hardwareInfo.Temperature;
                _currentMetrics.DiskUsage = metrics.DiskUsagePercentage;
                _currentMetrics.ProcessCount = metrics.ProcessCount;
                _currentMetrics.ThreadCount = metrics.ThreadCount;
                _currentMetrics.HandleCount = metrics.HandleCount;
                _currentMetrics.GcCollections = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
                _currentMetrics.GcMemory = GC.GetTotalMemory(false);
                _currentMetrics.Uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
                _currentMetrics.PerformanceScore = performanceScore;
                _currentMetrics.AlertCount = _performanceHistory.Sum(s => s.AlertCount);

                // Create snapshot
                var snapshot = new PerformanceSnapshot
                {
                    Timestamp = _currentMetrics.Timestamp,
                    MemoryUsage = _currentMetrics.MemoryUsage,
                    CpuUsage = _currentMetrics.CpuUsage,
                    NetworkUsage = _currentMetrics.NetworkUsage,
                    Temperature = _currentMetrics.Temperature,
                    DiskUsage = _currentMetrics.DiskUsage,
                    ProcessCount = _currentMetrics.ProcessCount,
                    ThreadCount = _currentMetrics.ThreadCount,
                    HandleCount = _currentMetrics.HandleCount,
                    GcCollections = _currentMetrics.GcCollections,
                    GcMemory = _currentMetrics.GcMemory,
                    PerformanceScore = _currentMetrics.PerformanceScore,
                    AlertCount = _currentMetrics.AlertCount
                };

                // Add to history
                _performanceHistory.Add(snapshot);
                
                // Limit history size
                if (_performanceHistory.Count > _maxHistoryEntries)
                {
                    _performanceHistory.RemoveAt(0);
                }

                stopwatch.Stop();
                _currentMetrics.SnapshotDuration = stopwatch.Elapsed;
            }

            // Check for alerts
            await CheckForAlertsAsync();

            // Raise event
            PerformanceMetricsUpdated?.Invoke(this, new PerformanceMetricsUpdatedEventArgs(_currentMetrics));
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error taking performance snapshot: {ex.Message}");
            _errorCount++;
            _lastErrorTime = DateTime.Now;
            
            PerformanceError?.Invoke(this, new PerformanceErrorEventArgs(
                "Performance Snapshot Error",
                ex.Message,
                DateTime.Now
            ));
        }
    }

    private async Task CheckForAlertsAsync()
    {
        try
        {
            var alerts = new List<PerformanceAlertModel>();

            // Check memory usage
            if (_currentMetrics.MemoryUsage > HighMemoryThreshold)
            {
                alerts.Add(new PerformanceAlertModel
                {
                    Type = PerformanceAlertType.Memory,
                    Severity = _currentMetrics.MemoryUsage > 95 ? PerformanceAlertSeverity.Critical : PerformanceAlertSeverity.High,
                    Message = $"High memory usage: {_currentMetrics.MemoryUsage:F1}%",
                    Timestamp = DateTime.Now
                });
            }

            // Check CPU usage
            if (_currentMetrics.CpuUsage > HighCpuThreshold)
            {
                alerts.Add(new PerformanceAlertModel
                {
                    Type = PerformanceAlertType.Cpu,
                    Severity = _currentMetrics.CpuUsage > 95 ? PerformanceAlertSeverity.Critical : PerformanceAlertSeverity.High,
                    Message = $"High CPU usage: {_currentMetrics.CpuUsage:F1}%",
                    Timestamp = DateTime.Now
                });
            }

            // Check network usage
            if (_currentMetrics.NetworkUsage > HighNetworkThreshold)
            {
                alerts.Add(new PerformanceAlertModel
                {
                    Type = PerformanceAlertType.Network,
                    Severity = _currentMetrics.NetworkUsage > 90 ? PerformanceAlertSeverity.Critical : PerformanceAlertSeverity.Medium,
                    Message = $"High network usage: {_currentMetrics.NetworkUsage:F1}%",
                    Timestamp = DateTime.Now
                });
            }

            // Check temperature
            if (_currentMetrics.Temperature > HighTemperatureThreshold)
            {
                alerts.Add(new PerformanceAlertModel
                {
                    Type = PerformanceAlertType.Temperature,
                    Severity = _currentMetrics.Temperature > 95 ? PerformanceAlertSeverity.Critical : PerformanceAlertSeverity.High,
                    Message = $"High temperature: {_currentMetrics.Temperature:F1}°C",
                    Timestamp = DateTime.Now
                });
            }

            // Raise alerts
            foreach (var alert in alerts)
            {
                PerformanceAlert?.Invoke(this, new PerformanceAlertEventArgs(alert));
                _currentMetrics.AlertCount++;
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error checking for alerts: {ex.Message}");
        }
    }

    private void OnHistoryCleanupTimerTick(object? state)
    {
        try
        {
            lock (_lock)
            {
                var cutoffTime = DateTime.Now.AddHours(-24);
                _performanceHistory.RemoveAll(s => s.Timestamp < cutoffTime);
            }
        }
        catch (Exception ex)
        {
            _loggerService?.LogErrorAsync($"Error cleaning up performance history: {ex.Message}").FireAndForget();
        }
    }

    private double CalculatePerformanceScore(double memoryUsage, double cpuUsage, double networkUsage, double temperature)
    {
        // Normalize each metric to 0-100 scale
        var normalizedMemory = Math.Min(100, memoryUsage);
        var normalizedCpu = Math.Min(100, cpuUsage);
        var normalizedNetwork = Math.Min(100, networkUsage);
        var normalizedTemperature = Math.Min(100, temperature);

        // Calculate weighted score (lower is better for resource usage)
        var resourceScore = (normalizedMemory * 0.3 + normalizedCpu * 0.3 + normalizedNetwork * 0.2 + normalizedTemperature * 0.2);
        
        // Convert to performance score (higher is better)
        var performanceScore = Math.Max(0, 100 - resourceScore);
        
        return Math.Round(performanceScore, 1);
    }

    private HealthStatus GetResourceHealth(double usage, double threshold)
    {
        if (usage >= threshold * 1.2)
            return HealthStatus.Critical;
        else if (usage >= threshold)
            return HealthStatus.Warning;
        else
            return HealthStatus.Good;
    }

    private double CalculateErrorRate()
    {
        if (_totalOperations == 0)
            return 0;

        var timeSinceLastError = DateTime.Now - _lastErrorTime;
        if (timeSinceLastError > _errorResetInterval)
        {
            _errorCount = 0;
        }

        return (double)_errorCount / _totalOperations * 100;
    }

    private PerformanceTrend CalculateTrend(IEnumerable<double> values)
    {
        var valueList = values.ToList();
        if (valueList.Count < 2)
            return PerformanceTrend.Stable;

        var firstHalf = valueList.Take(valueList.Count / 2).Average();
        var secondHalf = valueList.Skip(valueList.Count / 2).Average();

        var difference = secondHalf - firstHalf;
        var threshold = 5.0; // 5% threshold for trend detection

        if (Math.Abs(difference) < threshold)
            return PerformanceTrend.Stable;
        else if (difference > 0)
            return PerformanceTrend.Increasing;
        else
            return PerformanceTrend.Decreasing;
    }

    private void GenerateRecommendations(PerformanceAnalysisReport report)
    {
        // Memory recommendations
        if (report.AverageMemoryUsage > HighMemoryThreshold)
        {
            report.Recommendations.Add("Consider closing unnecessary applications or increasing system memory");
        }

        // CPU recommendations
        if (report.AverageCpuUsage > HighCpuThreshold)
        {
            report.Recommendations.Add("Consider optimizing CPU-intensive processes or upgrading CPU");
        }

        // Network recommendations
        if (report.AverageNetworkUsage > HighNetworkThreshold)
        {
            report.Recommendations.Add("Consider optimizing network usage or upgrading network connection");
        }

        // Temperature recommendations
        if (report.AverageTemperature > HighTemperatureThreshold)
        {
            report.Recommendations.Add("Consider improving cooling or reducing system load");
        }

        // Trend-based recommendations
        if (report.MemoryTrend == PerformanceTrend.Increasing)
        {
            report.Recommendations.Add("Memory usage is trending upward - monitor for potential memory leaks");
        }

        if (report.CpuTrend == PerformanceTrend.Increasing)
        {
            report.Recommendations.Add("CPU usage is trending upward - check for background processes");
        }

        // Error rate recommendations
        if (report.ErrorRate > 5)
        {
            report.Recommendations.Add("High error rate detected - investigate system stability");
        }
    }

    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        // Metrics updated, take snapshot if running
        if (_isRunning)
        {
            _ = TakePerformanceSnapshotAsync();
        }
    }

    private void OnMetricsError(object? sender, MetricsErrorEventArgs e)
    {
        _errorCount++;
        _lastErrorTime = DateTime.Now;
        
        PerformanceError?.Invoke(this, new PerformanceErrorEventArgs(
            "Metrics Error",
            e.ErrorMessage,
            DateTime.Now
        ));
    }

    private void OnHardwareStatusChanged(object? sender, HardwareStatusChangedEventArgs e)
    {
        // Hardware status changed, take snapshot if running
        if (_isRunning)
        {
            _ = TakePerformanceSnapshotAsync();
        }
    }

    private void OnHardwareError(object? sender, HardwareErrorEventArgs e)
    {
        _errorCount++;
        _lastErrorTime = DateTime.Now;
        
        PerformanceError?.Invoke(this, new PerformanceErrorEventArgs(
            "Hardware Error",
            e.ErrorMessage,
            DateTime.Now
        ));
    }

    private void OnNetworkStatusChanged(object? sender, NetworkStatusChangedEventArgs e)
    {
        // Network status changed, take snapshot if running
        if (_isRunning)
        {
            _ = TakePerformanceSnapshotAsync();
        }
    }

    private void OnNetworkError(object? sender, NetworkErrorEventArgs e)
    {
        _errorCount++;
        _lastErrorTime = DateTime.Now;
        
        PerformanceError?.Invoke(this, new PerformanceErrorEventArgs(
            "Network Error",
            e.ErrorMessage,
            DateTime.Now
        ));
    }

    private void OnCompressionStatusChanged(object? sender, CompressionStatusChangedEventArgs e)
    {
        _totalOperations++;
        
        // Track memory allocation
        if (e.BytesProcessed > 0)
        {
            Interlocked.Add(ref _totalMemoryAllocated, e.BytesProcessed);
        }
    }

    private void OnCompressionError(object? sender, CompressionErrorEventArgs e)
    {
        _errorCount++;
        _lastErrorTime = DateTime.Now;
        
        PerformanceError?.Invoke(this, new PerformanceErrorEventArgs(
            "Compression Error",
            e.ErrorMessage,
            DateTime.Now
        ));
    }

    private void OnOptimizationStatusChanged(object? sender, OptimizationStatusChangedEventArgs e)
    {
        _totalOperations++;
        
        // Track memory freed
        if (e.MemoryFreed > 0)
        {
            Interlocked.Add(ref _totalMemoryFreed, e.MemoryFreed);
        }
    }

    private void OnOptimizationError(object? sender, OptimizationErrorEventArgs e)
    {
        _errorCount++;
        _lastErrorTime = DateTime.Now;
        
        PerformanceError?.Invoke(this, new PerformanceErrorEventArgs(
            "Optimization Error",
            e.ErrorMessage,
            DateTime.Now
        ));
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            
            // Stop timers
            _performanceTimer?.Dispose();
            _historyCleanupTimer?.Dispose();
            
            // Stop monitoring
            _isRunning = false;
            
            // Unsubscribe from events
            _metricsService.MetricsUpdated -= OnMetricsUpdated;
            _metricsService.MetricsError -= OnMetricsError;
            _hardwareService.HardwareStatusChanged -= OnHardwareStatusChanged;
            _hardwareService.HardwareError -= OnHardwareError;
            _networkService.NetworkStatusChanged -= OnNetworkStatusChanged;
            _networkService.NetworkError -= OnNetworkError;
            _compressionService.CompressionStatusChanged -= OnCompressionStatusChanged;
            _compressionService.CompressionError -= OnCompressionError;
            _optimizationService.OptimizationStatusChanged -= OnOptimizationStatusChanged;
            _optimizationService.OptimizationError -= OnOptimizationError;
        }
    }
}

// Supporting classes and enums
public enum HealthStatus
{
    Good,
    Warning,
    Critical,
    Unknown
}

public enum PerformanceTrend
{
    Increasing,
    Decreasing,
    Stable
}

public enum PerformanceAlertType
{
    Memory,
    Cpu,
    Network,
    Temperature,
    Disk,
    General
}

public enum PerformanceAlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class PerformanceMetricsModel
{
    public DateTime Timestamp { get; set; }
    public double MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public double NetworkUsage { get; set; }
    public double Temperature { get; set; }
    public double DiskUsage { get; set; }
    public int ProcessCount { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public int GcCollections { get; set; }
    public long GcMemory { get; set; }
    public long TotalMemoryAllocated { get; set; }
    public long TotalMemoryFreed { get; set; }
    public int TotalOperations { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan Uptime { get; set; }
    public double PerformanceScore { get; set; }
    public int AlertCount { get; set; }
    public DateTime LastErrorTime { get; set; }
    public TimeSpan SnapshotDuration { get; set; }
}

public class PerformanceSnapshot
{
    public DateTime Timestamp { get; set; }
    public double MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public double NetworkUsage { get; set; }
    public double Temperature { get; set; }
    public double DiskUsage { get; set; }
    public int ProcessCount { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public int GcCollections { get; set; }
    public long GcMemory { get; set; }
    public double PerformanceScore { get; set; }
    public int AlertCount { get; set; }
}

public class PerformanceHealthStatus
{
    public HealthStatus OverallHealth { get; set; }
    public HealthStatus MemoryHealth { get; set; }
    public HealthStatus CpuHealth { get; set; }
    public HealthStatus NetworkHealth { get; set; }
    public HealthStatus TemperatureHealth { get; set; }
    public double ErrorRate { get; set; }
    public TimeSpan Uptime { get; set; }
    public DateTime LastErrorTime { get; set; }
    public int ActiveAlerts { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

public class PerformanceAnalysisReport
{
    public TimeSpan TimeRange { get; set; }
    public int SnapshotCount { get; set; }
    public double AverageMemoryUsage { get; set; }
    public double AverageCpuUsage { get; set; }
    public double AverageNetworkUsage { get; set; }
    public double AverageTemperature { get; set; }
    public double PeakMemoryUsage { get; set; }
    public double PeakCpuUsage { get; set; }
    public double PeakNetworkUsage { get; set; }
    public double PeakTemperature { get; set; }
    public PerformanceTrend MemoryTrend { get; set; }
    public PerformanceTrend CpuTrend { get; set; }
    public PerformanceTrend NetworkTrend { get; set; }
    public PerformanceTrend TemperatureTrend { get; set; }
    public int AlertCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

public class PerformanceAlertModel
{
    public PerformanceAlertType Type { get; set; }
    public PerformanceAlertSeverity Severity { get; set; }
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

// Event argument classes
public class PerformanceMetricsUpdatedEventArgs : EventArgs
{
    public PerformanceMetricsModel Metrics { get; }

    public PerformanceMetricsUpdatedEventArgs(PerformanceMetricsModel metrics)
    {
        Metrics = metrics;
    }
}

public class PerformanceAlertEventArgs : EventArgs
{
    public PerformanceAlertModel Alert { get; }

    public PerformanceAlertEventArgs(PerformanceAlertModel alert)
    {
        Alert = alert;
    }
}

public class PerformanceErrorEventArgs : EventArgs
{
    public string Title { get; }
    public string Message { get; }
    public DateTime Timestamp { get; }

    public PerformanceErrorEventArgs(string title, string message, DateTime timestamp)
    {
        Title = title;
        Message = message;
        Timestamp = timestamp;
    }
}