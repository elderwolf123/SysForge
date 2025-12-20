using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RamOptimizerNova.ViewModels.Pages;

namespace RamOptimizerNova.Services;

public interface IMetricsService
{
    Task InitializeAsync();
    Task CleanupAsync();
    Task<MetricsModel> GetCurrentMetricsAsync();
    Task RefreshMetricsAsync();
    event EventHandler<MetricsUpdatedEventArgs>? MetricsUpdated;
    event EventHandler<MetricsErrorEventArgs>? MetricsError;
    event EventHandler<MetricsClearedEventArgs>? MetricsCleared;
    IDisposable SubscribeToMetrics(Action<MetricsModel> callback);
    Task<MetricsHistoryModel> GetMetricsHistoryAsync(TimeSpan timeRange);
    Task<MetricsSummaryModel> GetMetricsSummaryAsync();
    Task<bool> StartMetricsCollectionAsync();
    Task<bool> StopMetricsCollectionAsync();
    bool IsCollectingMetrics { get; }
    MetricsCollectionSettings CollectionSettings { get; set; }
}

public class MetricsService : IMetricsService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsService _settingsService;
    private readonly ILoggerService _loggerService;
    private readonly ISystemService _systemService;
    private readonly IHardwareService _hardwareService;
    private readonly INetworkService _networkService;
    private readonly ICompressionService _compressionService;
    private readonly IOptimizationService _optimizationService;

    private readonly List<MetricsModel> _metricsHistory = new();
    private readonly List<Action<MetricsModel>> _subscribers = new();
    private readonly object _lock = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Timer _metricsTimer;
    private readonly Timer _cleanupTimer;
    private bool _isInitialized = false;
    private bool _isCollecting = false;
    private DateTime _lastCollectionTime = DateTime.Now;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramCounter;

    public MetricsCollectionSettings CollectionSettings { get; set; } = new();

    public bool IsCollectingMetrics => _isCollecting;

    public event EventHandler<MetricsUpdatedEventArgs>? MetricsUpdated;
    public event EventHandler<MetricsErrorEventArgs>? MetricsError;
    public event EventHandler<MetricsClearedEventArgs>? MetricsCleared;

    public MetricsService(
        IServiceProvider serviceProvider,
        ISettingsService settingsService,
        ILoggerService loggerService,
        ISystemService systemService,
        IHardwareService hardwareService,
        INetworkService networkService,
        ICompressionService compressionService,
        IOptimizationService optimizationService)
    {
        _serviceProvider = serviceProvider;
        _settingsService = settingsService;
        _loggerService = loggerService;
        _systemService = systemService;
        _hardwareService = hardwareService;
        _networkService = networkService;
        _compressionService = compressionService;
        _optimizationService = optimizationService;

        // Initialize timers
        _metricsTimer = new Timer(OnMetricsTimerTick, null, Timeout.Infinite, Timeout.Infinite);
        _cleanupTimer = new Timer(OnCleanupTimerTick, null, Timeout.Infinite, Timeout.Infinite);

        // Load collection settings
        LoadCollectionSettings();
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            await _loggerService.LogAsync("Initializing Metrics Service...");

            // Initialize performance counters
            InitializePerformanceCounters();

            // Initialize services
            await _systemService.InitializeAsync();
            await _hardwareService.InitializeAsync();
            await _networkService.InitializeAsync();
            await _compressionService.InitializeAsync();
            await _optimizationService.InitializeAsync();

            // Start metrics collection if enabled
            if (CollectionSettings.AutoCollect)
            {
                await StartMetricsCollectionAsync();
            }

            // Start cleanup timer
            _cleanupTimer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

            _isInitialized = true;
            await _loggerService.LogAsync("Metrics Service initialized successfully.");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error initializing Metrics Service: {ex.Message}");
            throw;
        }
    }

    public async Task CleanupAsync()
    {
        try
        {
            await _loggerService.LogAsync("Cleaning up Metrics Service...");

            // Stop metrics collection
            await StopMetricsCollectionAsync();

            // Stop timers
            _metricsTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _cleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // Dispose performance counters
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();

            // Clear history
            lock (_lock)
            {
                _metricsHistory.Clear();
            }

            // Unsubscribe from events
            _systemService.SystemInfoChanged -= OnSystemInfoChanged;
            _hardwareService.HardwareStatusChanged -= OnHardwareStatusChanged;
            _networkService.NetworkStatusChanged -= OnNetworkStatusChanged;
            _compressionService.CompressionStatusChanged -= OnCompressionStatusChanged;
            _optimizationService.OptimizationStatusChanged -= OnOptimizationStatusChanged;

            _isInitialized = false;
            await _loggerService.LogAsync("Metrics Service cleaned up successfully.");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error cleaning up Metrics Service: {ex.Message}");
        }
    }

    public async Task<MetricsModel> GetCurrentMetricsAsync()
    {
        try
        {
            var metrics = new MetricsModel
            {
                Timestamp = DateTime.Now,
                SystemInfo = await GetSystemInfoAsync(),
                MemoryMetrics = await GetMemoryMetricsAsync(),
                CpuMetrics = await GetCpuMetricsAsync(),
                NetworkMetrics = await GetNetworkMetricsAsync(),
                HardwareMetrics = await GetHardwareMetricsAsync(),
                CompressionMetrics = await GetCompressionMetricsAsync(),
                OptimizationMetrics = await GetOptimizationMetricsAsync(),
                PerformanceMetrics = await GetPerformanceMetricsAsync()
            };

            // Add to history
            lock (_lock)
            {
                _metricsHistory.Add(metrics);
                
                // Keep only recent history
                var maxHistory = CollectionSettings.MaxHistoryEntries;
                if (_metricsHistory.Count > maxHistory)
                {
                    _metricsHistory.RemoveRange(0, _metricsHistory.Count - maxHistory);
                }
            }

            // Notify subscribers
            NotifySubscribers(metrics);

            // Raise event
            MetricsUpdated?.Invoke(this, new MetricsUpdatedEventArgs(metrics));

            return metrics;
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting current metrics: {ex.Message}");
            MetricsError?.Invoke(this, new MetricsErrorEventArgs($"Failed to get current metrics: {ex.Message}"));
            throw;
        }
    }

    public async Task RefreshMetricsAsync()
    {
        try
        {
            await GetCurrentMetricsAsync();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error refreshing metrics: {ex.Message}");
            MetricsError?.Invoke(this, new MetricsErrorEventArgs($"Failed to refresh metrics: {ex.Message}"));
        }
    }

    public IDisposable SubscribeToMetrics(Action<MetricsModel> callback)
    {
        lock (_lock)
        {
            _subscribers.Add(callback);
        }

        return new DisposableSubscription(() =>
        {
            lock (_lock)
            {
                _subscribers.Remove(callback);
            }
        });
    }

    public async Task<MetricsHistoryModel> GetMetricsHistoryAsync(TimeSpan timeRange)
    {
        try
        {
            var endTime = DateTime.Now;
            var startTime = endTime - timeRange;

            lock (_lock)
            {
                var history = _metricsHistory
                    .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
                    .ToList();

                return new MetricsHistoryModel
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    Entries = history,
                    TotalEntries = history.Count,
                    TimeRange = timeRange
                };
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting metrics history: {ex.Message}");
            MetricsError?.Invoke(this, new MetricsErrorEventArgs($"Failed to get metrics history: {ex.Message}"));
            throw;
        }
    }

    public async Task<MetricsSummaryModel> GetMetricsSummaryAsync()
    {
        try
        {
            lock (_lock)
            {
                if (_metricsHistory.Count == 0)
                {
                    return new MetricsSummaryModel();
                }

                var memoryUsages = _metricsHistory.Select(m => m.MemoryMetrics?.MemoryUsageMB ?? 0).ToList();
                var cpuUsages = _metricsHistory.Select(m => m.CpuMetrics?.CPUUsage ?? 0).ToList();
                var networkUsages = _metricsHistory.Select(m => m.NetworkMetrics?.NetworkUsageMbps ?? 0).ToList();

                return new MetricsSummaryModel
                {
                    TotalEntries = _metricsHistory.Count,
                    TimeRange = _metricsHistory.Last().Timestamp - _metricsHistory.First().Timestamp,
                    MemorySummary = new MetricSummaryModel
                    {
                        Average = memoryUsages.Average(),
                        Minimum = memoryUsages.Min(),
                        Maximum = memoryUsages.Max(),
                        Current = memoryUsages.Last()
                    },
                    CpuSummary = new MetricSummaryModel
                    {
                        Average = cpuUsages.Average(),
                        Minimum = cpuUsages.Min(),
                        Maximum = cpuUsages.Max(),
                        Current = cpuUsages.Last()
                    },
                    NetworkSummary = new MetricSummaryModel
                    {
                        Average = networkUsages.Average(),
                        Minimum = networkUsages.Min(),
                        Maximum = networkUsages.Max(),
                        Current = networkUsages.Last()
                    },
                    LastUpdateTime = _metricsHistory.Last().Timestamp
                };
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting metrics summary: {ex.Message}");
            MetricsError?.Invoke(this, new MetricsErrorEventArgs($"Failed to get metrics summary: {ex.Message}"));
            throw;
        }
    }

    public async Task<bool> StartMetricsCollectionAsync()
    {
        try
        {
            if (_isCollecting)
                return true;

            await _loggerService.LogAsync("Starting metrics collection...");

            // Start metrics timer
            _metricsTimer.Change(CollectionSettings.CollectionInterval, CollectionSettings.CollectionInterval);

            _isCollecting = true;
            _lastCollectionTime = DateTime.Now;

            // Get initial metrics
            await GetCurrentMetricsAsync();

            await _loggerService.LogAsync("Metrics collection started successfully.");
            return true;
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error starting metrics collection: {ex.Message}");
            MetricsError?.Invoke(this, new MetricsErrorEventArgs($"Failed to start metrics collection: {ex.Message}"));
            return false;
        }
    }

    public async Task<bool> StopMetricsCollectionAsync()
    {
        try
        {
            if (!_isCollecting)
                return true;

            await _loggerService.LogAsync("Stopping metrics collection...");

            // Stop metrics timer
            _metricsTimer.Change(Timeout.Infinite, Timeout.Infinite);

            _isCollecting = false;

            await _loggerService.LogAsync("Metrics collection stopped successfully.");
            return true;
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error stopping metrics collection: {ex.Message}");
            MetricsError?.Invoke(this, new MetricsErrorEventArgs($"Failed to stop metrics collection: {ex.Message}"));
            return false;
        }
    }

    private void InitializePerformanceCounters()
    {
        try
        {
            // Initialize CPU counter
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            
            // Initialize RAM counter
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing performance counters: {ex.Message}").Wait();
        }
    }

    private void LoadCollectionSettings()
    {
        try
        {
            CollectionSettings = new MetricsCollectionSettings
            {
                AutoCollect = _settingsService.GetSettingAsync<bool>("MetricsAutoCollect", true).Result,
                CollectionInterval = TimeSpan.FromSeconds(_settingsService.GetSettingAsync<int>("MetricsCollectionInterval", 5)).Result,
                MaxHistoryEntries = _settingsService.GetSettingAsync<int>("MetricsMaxHistory", 1000).Result,
                EnableMemoryMetrics = _settingsService.GetSettingAsync<bool>("MetricsEnableMemory", true).Result,
                EnableCpuMetrics = _settingsService.GetSettingAsync<bool>("MetricsEnableCPU", true).Result,
                EnableNetworkMetrics = _settingsService.GetSettingAsync<bool>("MetricsEnableNetwork", true).Result,
                EnableHardwareMetrics = _settingsService.GetSettingAsync<bool>("MetricsEnableHardware", true).Result,
                EnableCompressionMetrics = _settingsService.GetSettingAsync<bool>("MetricsEnableCompression", true).Result,
                EnableOptimizationMetrics = _settingsService.GetSettingAsync<bool>("MetricsEnableOptimization", true).Result,
                EnablePerformanceMetrics = _settingsService.GetSettingAsync<bool>("MetricsEnablePerformance", true).Result
            };
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error loading collection settings: {ex.Message}").Wait();
        }
    }

    private async Task<SystemInfoModel> GetSystemInfoAsync()
    {
        try
        {
            return await _systemService.GetSystemInfoAsync();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting system info: {ex.Message}");
            return new SystemInfoModel();
        }
    }

    private async Task<MemoryMetricsModel> GetMemoryMetricsAsync()
    {
        try
        {
            if (!CollectionSettings.EnableMemoryMetrics)
                return new MemoryMetricsModel();

            var memoryInfo = await _systemService.GetMemoryInfoAsync();
            var availableMemory = _ramCounter?.NextValue() ?? 0;
            var totalMemory = memoryInfo.TotalMemoryMB;
            var usedMemory = totalMemory - availableMemory;
            var memoryUsage = (double)usedMemory / totalMemory * 100;

            return new MemoryMetricsModel
            {
                TotalMemoryMB = totalMemory,
                UsedMemoryMB = usedMemory,
                AvailableMemoryMB = availableMemory,
                MemoryUsage = memoryUsage,
                MemoryUsageMB = usedMemory,
                PageFileUsage = memoryInfo.PageFileUsage,
                VirtualMemoryUsage = memoryInfo.VirtualMemoryUsage
            };
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting memory metrics: {ex.Message}");
            return new MemoryMetricsModel();
        }
    }

    private async Task<CpuMetricsModel> GetCpuMetricsModel()
    {
        try
        {
            if (!CollectionSettings.EnableCpuMetrics)
                return new CpuMetricsModel();

            var cpuUsage = _cpuCounter?.NextValue() ?? 0;
            var cpuInfo = await _systemService.GetCpuInfoAsync();

            return new CpuMetricsModel
            {
                CPUUsage = cpuUsage,
                CoreCount = cpuInfo.CoreCount,
                ThreadCount = cpuInfo.ThreadCount,
                MaxClockSpeed = cpuInfo.MaxClockSpeed,
                CurrentClockSpeed = cpuInfo.CurrentClockSpeed,
                Temperature = cpuInfo.Temperature,
                LoadAverage = cpuInfo.LoadAverage
            };
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting CPU metrics: {ex.Message}");
            return new CpuMetricsModel();
        }
    }

    private async Task<NetworkMetricsModel> GetNetworkMetricsAsync()
    {
        try
        {
            if (!CollectionSettings.EnableNetworkMetrics)
                return new NetworkMetricsModel();

            var networkInfo = await _networkService.GetNetworkInfoAsync();
            var stats = await _networkService.GetNetworkStatsAsync();

            return new NetworkMetricsModel
            {
                NetworkUsageMbps = stats.DownloadSpeedMbps + stats.UploadSpeedMbps,
                DownloadSpeedMbps = stats.DownloadSpeedMbps,
                UploadSpeedMbps = stats.UploadSpeedMbps,
                NetworkLoadPercentage = stats.NetworkLoadPercentage,
                ActiveConnections = stats.ActiveConnections,
                NetworkInterfaces = networkInfo.Interfaces
            };
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting network metrics: {ex.Message}");
            return new NetworkMetricsModel();
        }
    }

    private async Task<HardwareMetricsModel> GetHardwareMetricsAsync()
    {
        try
        {
            if (!CollectionSettings.EnableHardwareMetrics)
                return new HardwareMetricsModel();

            var hardwareInfo = await _hardwareService.GetHardwareInfoAsync();
            var status = await _hardwareService.GetHardwareStatusAsync();

            return new HardwareMetricsModel
            {
                HardwareStatus = status,
                Temperature = hardwareInfo.Temperature,
                FanSpeed = hardwareInfo.FanSpeed,
                PowerUsage = hardwareInfo.PowerUsage,
                HardwareComponents = hardwareInfo.Components
            };
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting hardware metrics: {ex.Message}");
            return new HardwareMetricsModel();
        }
    }

    private async Task<CompressionMetricsModel> GetCompressionMetricsAsync()
    {
        try
        {
            if (!CollectionSettings.EnableCompressionMetrics)
                return new CompressionMetricsModel();

            var compressionInfo = await _compressionService.GetCompressionInfoAsync();
            var stats = await _compressionService.GetCompressionStatsAsync();

            return new CompressionMetricsModel
            {
                ActiveCompressions = stats.ActiveCompressions,
                CompletedCompressions = stats.CompletedCompressions,
                TotalCompressedSize = stats.TotalCompressedSize,
                TotalOriginalSize = stats.TotalOriginalSize,
                CompressionRatio = stats.CompressionRatio,
                AverageCompressionTime = stats.AverageCompressionTime,
                CompressionThroughput = stats.CompressionThroughput
            };
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting compression metrics: {ex.Message}");
            return new CompressionMetricsModel();
        }
    }

    private async Task<OptimizationMetricsModel> GetOptimizationMetricsAsync()
    {
        try
        {
            if (!CollectionSettings.EnableOptimizationMetrics)
                return new OptimizationMetricsModel();

            var optimizationInfo = await _optimizationService.GetOptimizationInfoAsync();
            var stats = await _optimizationService.GetOptimizationStatsAsync();

            return new OptimizationMetricsModel
            {
                ActiveOptimizations = stats.ActiveOptimizations,
                CompletedOptimizations = stats.CompletedOptimizations,
                TotalOptimizedMemory = stats.TotalOptimizedMemory,
                AverageOptimizationTime = stats.AverageOptimizationTime,
                OptimizationEfficiency = stats.OptimizationEfficiency,
                OptimizationProgress = stats.OptimizationProgress
            };
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting optimization metrics: {ex.Message}");
            return new OptimizationMetricsModel();
        }
    }

    private async Task<PerformanceMetricsModel> GetPerformanceMetricsAsync()
    {
        try
        {
            if (!CollectionSettings.EnablePerformanceMetrics)
                return new PerformanceMetricsModel();

            var systemInfo = await _systemService.GetSystemInfoAsync();
            var processInfo = await _systemService.GetProcessInfoAsync();

            return new PerformanceMetricsModel
            {
                ProcessCount = processInfo.ProcessCount,
                ThreadCount = processInfo.ThreadCount,
                HandleCount = processInfo.HandleCount,
                GdiObjects = processInfo.GdiObjects,
                UserObjects = processInfo.UserObjects,
                SystemUpTime = systemInfo.SystemUpTime,
                SystemResponseTime = systemInfo.SystemResponseTime
            };
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error getting performance metrics: {ex.Message}");
            return new PerformanceMetricsModel();
        }
    }

    private void OnMetricsTimerTick(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await GetCurrentMetricsAsync();
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error in metrics timer tick: {ex.Message}");
            }
        });
    }

    private void OnCleanupTimerTick(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                CleanupOldMetrics();
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error in cleanup timer tick: {ex.Message}");
            }
        });
    }

    private void CleanupOldMetrics()
    {
        try
        {
            lock (_lock)
            {
                var cutoffTime = DateTime.Now - TimeSpan.FromHours(24);
                var oldCount = _metricsHistory.Count;
                
                _metricsHistory.RemoveAll(m => m.Timestamp < cutoffTime);
                
                if (_metricsHistory.Count < oldCount)
                {
                    MetricsCleared?.Invoke(this, new MetricsClearedEventArgs(oldCount - _metricsHistory.Count));
                }
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error cleaning up old metrics: {ex.Message}").Wait();
        }
    }

    private void NotifySubscribers(MetricsModel metrics)
    {
        lock (_lock)
        {
            foreach (var subscriber in _subscribers.ToList())
            {
                try
                {
                    subscriber(metrics);
                }
                catch (Exception ex)
                {
                    _loggerService.LogErrorAsync($"Error notifying subscriber: {ex.Message}").Wait();
                }
            }
        }
    }

    private void OnSystemInfoChanged(object? sender, SystemInfoChangedEventArgs e)
    {
        _ = RefreshMetricsAsync();
    }

    private void OnHardwareStatusChanged(object? sender, HardwareStatusChangedEventArgs e)
    {
        _ = RefreshMetricsAsync();
    }

    private void OnNetworkStatusChanged(object? sender, NetworkStatusChangedEventArgs e)
    {
        _ = RefreshMetricsAsync();
    }

    private void OnCompressionStatusChanged(object? sender, CompressionStatusChangedEventArgs e)
    {
        _ = RefreshMetricsAsync();
    }

    private void OnOptimizationStatusChanged(object? sender, OptimizationStatusChangedEventArgs e)
    {
        _ = RefreshMetricsAsync();
    }

    public void Dispose()
    {
        try
        {
            CleanupAsync().Wait();
            _cancellationTokenSource.Dispose();
            _metricsTimer.Dispose();
            _cleanupTimer.Dispose();
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error disposing Metrics Service: {ex.Message}").Wait();
        }
    }
}

// Supporting classes
public class MetricsModel
{
    public DateTime Timestamp { get; set; }
    public SystemInfoModel SystemInfo { get; set; } = new();
    public MemoryMetricsModel MemoryMetrics { get; set; } = new();
    public CpuMetricsModel CpuMetrics { get; set; } = new();
    public NetworkMetricsModel NetworkMetrics { get; set; } = new();
    public HardwareMetricsModel HardwareMetrics { get; set; } = new();
    public CompressionMetricsModel CompressionMetrics { get; set; } = new();
    public OptimizationMetricsModel OptimizationMetrics { get; set; } = new();
    public PerformanceMetricsModel PerformanceMetrics { get; set; } = new();
}

public class MetricsHistoryModel
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<MetricsModel> Entries { get; set; } = new();
    public int TotalEntries { get; set; }
    public TimeSpan TimeRange { get; set; }
}

public class MetricsSummaryModel
{
    public int TotalEntries { get; set; }
    public TimeSpan TimeRange { get; set; }
    public MetricSummaryModel MemorySummary { get; set; } = new();
    public MetricSummaryModel CpuSummary { get; set; } = new();
    public MetricSummaryModel NetworkSummary { get; set; } = new();
    public DateTime LastUpdateTime { get; set; }
}

public class MetricSummaryModel
{
    public double Average { get; set; }
    public double Minimum { get; set; }
    public double Maximum { get; set; }
    public double Current { get; set; }
}

public class MetricsCollectionSettings
{
    public bool AutoCollect { get; set; } = true;
    public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxHistoryEntries { get; set; } = 1000;
    public bool EnableMemoryMetrics { get; set; } = true;
    public bool EnableCpuMetrics { get; set; } = true;
    public bool EnableNetworkMetrics { get; set; } = true;
    public bool EnableHardwareMetrics { get; set; } = true;
    public bool EnableCompressionMetrics { get; set; } = true;
    public bool EnableOptimizationMetrics { get; set; } = true;
    public bool EnablePerformanceMetrics { get; set; } = true;
}

// Event arguments
public class MetricsUpdatedEventArgs : EventArgs
{
    public MetricsModel Metrics { get; }

    public MetricsUpdatedEventArgs(MetricsModel metrics)
    {
        Metrics = metrics;
    }
}

public class MetricsErrorEventArgs : EventArgs
{
    public string ErrorMessage { get; }

    public MetricsErrorEventArgs(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }
}

public class MetricsClearedEventArgs : EventArgs
{
    public int ClearedCount { get; }

    public MetricsClearedEventArgs(int clearedCount)
    {
        ClearedCount = clearedCount;
    }
}

// Metric model classes
public class SystemInfoModel
{
    public string OperatingSystem { get; set; } = "";
    public string Architecture { get; set; } = "";
    public int ProcessorCount { get; set; }
    public string SystemUpTime { get; set; } = "";
    public string SystemResponseTime { get; set; } = "";
}

public class MemoryMetricsModel
{
    public long TotalMemoryMB { get; set; }
    public long UsedMemoryMB { get; set; }
    public long AvailableMemoryMB { get; set; }
    public double MemoryUsage { get; set; }
    public long MemoryUsageMB { get; set; }
    public long PageFileUsage { get; set; }
    public long VirtualMemoryUsage { get; set; }
}

public class CpuMetricsModel
{
    public double CPUUsage { get; set; }
    public int CoreCount { get; set; }
    public int ThreadCount { get; set; }
    public double MaxClockSpeed { get; set; }
    public double CurrentClockSpeed { get; set; }
    public double Temperature { get; set; }
    public double LoadAverage { get; set; }
}

public class NetworkMetricsModel
{
    public double NetworkUsageMbps { get; set; }
    public double DownloadSpeedMbps { get; set; }
    public double UploadSpeedMbps { get; set; }
    public double NetworkLoadPercentage { get; set; }
    public int ActiveConnections { get; set; }
    public List<NetworkInterfaceModel> NetworkInterfaces { get; set; } = new();
}

public class NetworkInterfaceModel
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public long Speed { get; set; }
    public long BytesReceived { get; set; }
    public long BytesSent { get; set; }
}

public class HardwareMetricsModel
{
    public string HardwareStatus { get; set; } = "";
    public double Temperature { get; set; }
    public double FanSpeed { get; set; }
    public double PowerUsage { get; set; }
    public List<HardwareComponentModel> HardwareComponents { get; set; } = new();
}

public class HardwareComponentModel
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public double Temperature { get; set; }
    public double Usage { get; set; }
}

public class CompressionMetricsModel
{
    public int ActiveCompressions { get; set; }
    public int CompletedCompressions { get; set; }
    public long TotalCompressedSize { get; set; }
    public long TotalOriginalSize { get; set; }
    public double CompressionRatio { get; set; }
    public TimeSpan AverageCompressionTime { get; set; }
    public double CompressionThroughput { get; set; }
}

public class OptimizationMetricsModel
{
    public int ActiveOptimizations { get; set; }
    public int CompletedOptimizations { get; set; }
    public long TotalOptimizedMemory { get; set; }
    public TimeSpan AverageOptimizationTime { get; set; }
    public double OptimizationEfficiency { get; set; }
    public double OptimizationProgress { get; set; }
}

public class PerformanceMetricsModel
{
    public int ProcessCount { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public int GdiObjects { get; set; }
    public int UserObjects { get; set; }
    public string SystemUpTime { get; set; } = "";
    public string SystemResponseTime { get; set; } = "";
}

public class DisposableSubscription : IDisposable
{
    private readonly Action _disposeAction;

    public DisposableSubscription(Action disposeAction)
    {
        _disposeAction = disposeAction;
    }

    public void Dispose()
    {
        _disposeAction();
    }
}