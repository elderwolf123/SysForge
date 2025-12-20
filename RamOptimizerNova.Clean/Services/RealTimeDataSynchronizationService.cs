using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RamOptimizerNova.Models;
using RamOptimizerNova.Services.Interfaces;

namespace RamOptimizerNova.Services;

/// <summary>
/// Service that handles real-time data synchronization between the Nova UI and backend
/// Manages data caching, updates, and ensures UI responsiveness
/// </summary>
public class RealTimeDataSynchronizationService : IRealTimeDataSynchronizationService, IDisposable
{
    private readonly ILogger<RealTimeDataSynchronizationService> _logger;
    private readonly IRamOptimizerBackendService _backendService;
    private readonly IMetricsService _metricsService;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly INetworkService _networkService;

    private readonly ConcurrentDictionary<string, object> _dataCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes = new();
    private readonly SemaphoreSlim _dataLock = new(1, 1);

    private bool _isInitialized = false;
    private bool _isSynchronizing = false;
    private bool _autoRefreshEnabled = true;
    private int _autoRefreshInterval = 2000; // 2 seconds
    private CancellationTokenSource? _synchronizationCancellationTokenSource;
    private DateTime _lastSynchronizationTime = DateTime.MinValue;

    // Data update events
    public event EventHandler<DataUpdatedEventArgs>? DataUpdated;
    public event EventHandler<CacheClearedEventArgs>? CacheCleared;
    public event EventHandler<SynchronizationErrorEventArgs>? SynchronizationError;

    // Cached data
    private SystemMetrics _cachedSystemMetrics = new();
    private HardwareState _cachedHardwareState = new();
    private OptimizationResults _cachedOptimizationResults = new();
    private SystemInfo _cachedSystemInfo = new();
    private HardwareInfo _cachedHardwareInfo = new();
    private List<OptimizationHistoryItem> _cachedOptimizationHistory = new();
    private List<HardwareSnapshotInfo> _cachedHardwareSnapshots = new();
    private ServiceStatus _cachedServiceStatus = new();

    public RealTimeDataSynchronizationService(
        ILogger<RealTimeDataSynchronizationService> logger,
        IRamOptimizerBackendService backendService,
        IMetricsService metricsService,
        IPerformanceMonitoringService performanceMonitoringService,
        INetworkService networkService)
    {
        _logger = logger;
        _backendService = backendService;
        _metricsService = metricsService;
        _performanceMonitoringService = performanceMonitoringService;
        _networkService = networkService;

        // Subscribe to backend events
        _backendService.SystemMetricsUpdated += OnSystemMetricsUpdated;
        _backendService.HardwareStateUpdated += OnHardwareStateUpdated;
        _backendService.OptimizationCompleted += OnOptimizationCompleted;
        _backendService.BackendError += OnBackendError;
    }

    /// <summary>
    /// Initialize the synchronization service
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Real-Time Data Synchronization Service...");

            // Initialize data cache
            await InitializeDataCacheAsync();

            // Load initial data
            await LoadInitialDataAsync();

            // Start synchronization
            await StartSynchronizationAsync();

            _isInitialized = true;
            _logger.LogInformation("Real-Time Data Synchronization Service initialized successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Real-Time Data Synchronization Service");
            await NotifySynchronizationError("Initialization Error", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Cleanup the synchronization service
    /// </summary>
    public async Task CleanupAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up Real-Time Data Synchronization Service...");

            // Stop synchronization
            StopSynchronization();

            // Clear cache
            await ClearCacheAsync();

            // Unsubscribe from events
            UnsubscribeFromEvents();

            _isInitialized = false;
            _logger.LogInformation("Real-Time Data Synchronization Service cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up Real-Time Data Synchronization Service");
        }
    }

    /// <summary>
    /// Get cached system metrics
    /// </summary>
    public async Task<SystemMetrics> GetSystemMetricsAsync()
    {
        try
        {
            if (!IsDataValid("SystemMetrics", _autoRefreshInterval))
            {
                await RefreshSystemMetricsAsync();
            }

            return _cachedSystemMetrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached system metrics");
            await NotifySynchronizationError("System Metrics Error", ex.Message);
            return new SystemMetrics();
        }
    }

    /// <summary>
    /// Get cached hardware state
    /// </summary>
    public async Task<HardwareState> GetHardwareStateAsync()
    {
        try
        {
            if (!IsDataValid("HardwareState", _autoRefreshInterval))
            {
                await RefreshHardwareStateAsync();
            }

            return _cachedHardwareState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached hardware state");
            await NotifySynchronizationError("Hardware State Error", ex.Message);
            return new HardwareState();
        }
    }

    /// <summary>
    /// Get cached optimization results
    /// </summary>
    public async Task<OptimizationResults> GetOptimizationResultsAsync()
    {
        try
        {
            if (!IsDataValid("OptimizationResults", _autoRefreshInterval))
            {
                await RefreshOptimizationResultsAsync();
            }

            return _cachedOptimizationResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached optimization results");
            await NotifySynchronizationError("Optimization Results Error", ex.Message);
            return new OptimizationResults();
        }
    }

    /// <summary>
    /// Get cached system info
    /// </summary>
    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        try
        {
            if (!IsDataValid("SystemInfo", 30000)) // 30 seconds
            {
                await RefreshSystemInfoAsync();
            }

            return _cachedSystemInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached system info");
            await NotifySynchronizationError("System Info Error", ex.Message);
            return new SystemInfo();
        }
    }

    /// <summary>
    /// Get cached hardware info
    /// </summary>
    public async Task<HardwareInfo> GetHardwareInfoAsync()
    {
        try
            {
            if (!IsDataValid("HardwareInfo", 30000)) // 30 seconds
            {
                await RefreshHardwareInfoAsync();
            }

            return _cachedHardwareInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached hardware info");
            await NotifySynchronizationError("Hardware Info Error", ex.Message);
            return new HardwareInfo();
        }
    }

    /// <summary>
    /// Get cached optimization history
    /// </summary>
    public async Task<List<OptimizationHistoryItem>> GetOptimizationHistoryAsync(int count = 50)
    {
        try
        {
            if (!IsDataValid("OptimizationHistory", 10000)) // 10 seconds
            {
                await RefreshOptimizationHistoryAsync(count);
            }

            return _cachedOptimizationHistory.Take(count).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached optimization history");
            await NotifySynchronizationError("Optimization History Error", ex.Message);
            return new List<OptimizationHistoryItem>();
        }
    }

    /// <summary>
    /// Get cached hardware snapshots
    /// </summary>
    public async Task<List<HardwareSnapshotInfo>> GetHardwareSnapshotsAsync()
    {
        try
        {
            if (!IsDataValid("HardwareSnapshots", 30000)) // 30 seconds
            {
                await RefreshHardwareSnapshotsAsync();
            }

            return _cachedHardwareSnapshots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached hardware snapshots");
            await NotifySynchronizationError("Hardware Snapshots Error", ex.Message);
            return new List<HardwareSnapshotInfo>();
        }
    }

    /// <summary>
    /// Get cached service status
    /// </summary>
    public async Task<ServiceStatus> GetServiceStatusAsync()
    {
        try
        {
            if (!IsDataValid("ServiceStatus", 5000)) // 5 seconds
            {
                await RefreshServiceStatusAsync();
            }

            return _cachedServiceStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached service status");
            await NotifySynchronizationError("Service Status Error", ex.Message);
            return new ServiceStatus();
        }
    }

    /// <summary>
    /// Refresh all cached data
    /// </summary>
    public async Task RefreshAllDataAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing all cached data...");

            var refreshTasks = new List<Task>
            {
                RefreshSystemMetricsAsync(),
                RefreshHardwareStateAsync(),
                RefreshOptimizationResultsAsync(),
                RefreshSystemInfoAsync(),
                RefreshHardwareInfoAsync(),
                RefreshOptimizationHistoryAsync(50),
                RefreshHardwareSnapshotsAsync(),
                RefreshServiceStatusAsync()
            };

            await Task.WhenAll(refreshTasks);

            _logger.LogInformation("All cached data refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing all cached data");
            await NotifySynchronizationError("Refresh Error", ex.Message);
        }
    }

    /// <summary>
    /// Refresh specific cached data
    /// </summary>
    public async Task RefreshDataAsync(string dataType)
    {
        try
        {
            _logger.LogInformation("Refreshing cached data: {DataType}", dataType);

            switch (dataType.ToLower())
            {
                case "systemmetrics":
                    await RefreshSystemMetricsAsync();
                    break;
                case "hardwarestate":
                    await RefreshHardwareStateAsync();
                    break;
                case "optimizationresults":
                    await RefreshOptimizationResultsAsync();
                    break;
                case "systeminfo":
                    await RefreshSystemInfoAsync();
                    break;
                case "hardwareinfo":
                    await RefreshHardwareInfoAsync();
                    break;
                case "optimizationhistory":
                    await RefreshOptimizationHistoryAsync(50);
                    break;
                case "hardwaresnapshots":
                    await RefreshHardwareSnapshotsAsync();
                    break;
                case "servicestatus":
                    await RefreshServiceStatusAsync();
                    break;
                default:
                    _logger.LogWarning("Unknown data type: {DataType}", dataType);
                    break;
            }

            _logger.LogInformation("Cached data refreshed successfully: {DataType}", dataType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cached data: {DataType}", dataType);
            await NotifySynchronizationError("Refresh Error", ex.Message);
        }
    }

    /// <summary>
    /// Clear all cached data
    /// </summary>
    public async Task ClearCacheAsync()
    {
        try
        {
            await _dataLock.WaitAsync();

            _logger.LogInformation("Clearing all cached data...");

            _dataCache.Clear();
            _lastUpdateTimes.Clear();

            // Reset cached data
            _cachedSystemMetrics = new SystemMetrics();
            _cachedHardwareState = new HardwareState();
            _cachedOptimizationResults = new OptimizationResults();
            _cachedSystemInfo = new SystemInfo();
            _cachedHardwareInfo = new HardwareInfo();
            _cachedOptimizationHistory = new List<OptimizationHistoryItem>();
            _cachedHardwareSnapshots = new List<HardwareSnapshotInfo>();
            _cachedServiceStatus = new ServiceStatus();

            _lastSynchronizationTime = DateTime.MinValue;

            await NotifyCacheCleared();

            _logger.LogInformation("All cached data cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cached data");
            await NotifySynchronizationError("Cache Clear Error", ex.Message);
        }
        finally
        {
            _dataLock.Release();
        }
    }

    /// <summary>
    /// Clear specific cached data
    /// </summary>
    public async Task ClearCacheAsync(string dataType)
    {
        try
        {
            await _dataLock.WaitAsync();

            _logger.LogInformation("Clearing cached data: {DataType}", dataType);

            _dataCache.TryRemove(dataType, out _);
            _lastUpdateTimes.TryRemove(dataType, out _);

            // Reset specific cached data
            switch (dataType.ToLower())
            {
                case "systemmetrics":
                    _cachedSystemMetrics = new SystemMetrics();
                    break;
                case "hardwarestate":
                    _cachedHardwareState = new HardwareState();
                    break;
                case "optimizationresults":
                    _cachedOptimizationResults = new OptimizationResults();
                    break;
                case "systeminfo":
                    _cachedSystemInfo = new SystemInfo();
                    break;
                case "hardwareinfo":
                    _cachedHardwareInfo = new HardwareInfo();
                    break;
                case "optimizationhistory":
                    _cachedOptimizationHistory = new List<OptimizationHistoryItem>();
                    break;
                case "hardwaresnapshots":
                    _cachedHardwareSnapshots = new List<HardwareSnapshotInfo>();
                    break;
                case "servicestatus":
                    _cachedServiceStatus = new ServiceStatus();
                    break;
                default:
                    _logger.LogWarning("Unknown data type: {DataType}", dataType);
                    break;
            }

            _logger.LogInformation("Cached data cleared successfully: {DataType}", dataType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cached data: {DataType}", dataType);
            await NotifySynchronizationError("Cache Clear Error", ex.Message);
        }
        finally
        {
            _dataLock.Release();
        }
    }

    /// <summary>
    /// Check if cached data is valid
    /// </summary>
    public bool IsDataValid(string dataType, int maxAgeMs)
    {
        if (_lastUpdateTimes.TryGetValue(dataType, out var lastUpdate))
        {
            return (DateTime.Now - lastUpdate).TotalMilliseconds <= maxAgeMs;
        }
        return false;
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public async Task<CacheStatistics> GetCacheStatisticsAsync()
    {
        try
        {
            await _dataLock.WaitAsync();

            var stats = new CacheStatistics
            {
                TotalCachedItems = _dataCache.Count,
                LastSynchronizationTime = _lastSynchronizationTime,
                CacheSizeBytes = EstimateCacheSize(),
                DataTypes = _dataCache.Keys.ToList(),
                DataAges = _lastUpdateTimes.ToDictionary(kvp => kvp.Key, kvp => DateTime.Now - kvp.Value)
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            await NotifySynchronizationError("Cache Statistics Error", ex.Message);
            return new CacheStatistics();
        }
        finally
        {
            _dataLock.Release();
        }
    }

    /// <summary>
    /// Enable or disable auto refresh
    /// </summary>
    public void SetAutoRefresh(bool enabled, int intervalMs = 2000)
    {
        _autoRefreshEnabled = enabled;
        _autoRefreshInterval = intervalMs;
        
        _logger.LogInformation("Auto refresh set to: {Enabled}, Interval: {Interval}ms", enabled, intervalMs);
    }

    /// <summary>
    /// Get synchronization status
    /// </summary>
    public SynchronizationStatus GetSynchronizationStatus()
    {
        return new SynchronizationStatus
        {
            IsInitialized = _isInitialized,
            IsSynchronizing = _isSynchronizing,
            AutoRefreshEnabled = _autoRefreshEnabled,
            AutoRefreshInterval = _autoRefreshInterval,
            LastSynchronizationTime = _lastSynchronizationTime,
            CacheStatistics = new CacheStatistics
            {
                TotalCachedItems = _dataCache.Count,
                LastSynchronizationTime = _lastSynchronizationTime,
                CacheSizeBytes = EstimateCacheSize(),
                DataTypes = _dataCache.Keys.ToList(),
                DataAges = _lastUpdateTimes.ToDictionary(kvp => kvp.Key, kvp => DateTime.Now - kvp.Value)
            }
        };
    }

    private async Task InitializeDataCacheAsync()
    {
        try
        {
            _logger.LogInformation("Initializing data cache...");

            // Initialize cache entries
            _dataCache["SystemMetrics"] = new SystemMetrics();
            _dataCache["HardwareState"] = new HardwareState();
            _dataCache["OptimizationResults"] = new OptimizationResults();
            _dataCache["SystemInfo"] = new SystemInfo();
            _dataCache["HardwareInfo"] = new HardwareInfo();
            _dataCache["OptimizationHistory"] = new List<OptimizationHistoryItem>();
            _dataCache["HardwareSnapshots"] = new List<HardwareSnapshotInfo>();
            _dataCache["ServiceStatus"] = new ServiceStatus();

            // Initialize update times
            var now = DateTime.Now;
            _lastUpdateTimes["SystemMetrics"] = now;
            _lastUpdateTimes["HardwareState"] = now;
            _lastUpdateTimes["OptimizationResults"] = now;
            _lastUpdateTimes["SystemInfo"] = now;
            _lastUpdateTimes["HardwareInfo"] = now;
            _lastUpdateTimes["OptimizationHistory"] = now;
            _lastUpdateTimes["HardwareSnapshots"] = now;
            _lastUpdateTimes["ServiceStatus"] = now;

            _logger.LogInformation("Data cache initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing data cache");
            throw;
        }
    }

    private async Task LoadInitialDataAsync()
    {
        try
        {
            _logger.LogInformation("Loading initial data...");

            // Load initial data
            await RefreshSystemMetricsAsync();
            await RefreshHardwareStateAsync();
            await RefreshOptimizationResultsAsync();
            await RefreshSystemInfoAsync();
            await RefreshHardwareInfoAsync();
            await RefreshOptimizationHistoryAsync(50);
            await RefreshHardwareSnapshotsAsync();
            await RefreshServiceStatusAsync();

            _logger.LogInformation("Initial data loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading initial data");
            throw;
        }
    }

    private async Task StartSynchronizationAsync()
    {
        try
        {
            if (_isSynchronizing)
            {
                _logger.LogWarning("Synchronization is already running");
                return;
            }

            _isSynchronizing = true;
            _synchronizationCancellationTokenSource = new CancellationTokenSource();

            _logger.LogInformation("Starting data synchronization...");

            // Start synchronization task
            var synchronizationTask = Task.Run(async () =>
            {
                while (_isSynchronizing && !_synchronizationCancellationTokenSource?.IsCancellationRequested ?? false)
                {
                    try
                    {
                        if (_autoRefreshEnabled)
                        {
                            await SynchronizeDataAsync();
                        }

                        await Task.Delay(_autoRefreshInterval, _synchronizationCancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in synchronization loop");
                        await NotifySynchronizationError("Synchronization Loop Error", ex.Message);
                    }
                }
            });

            _logger.LogInformation("Data synchronization started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting synchronization");
            await NotifySynchronizationError("Synchronization Start Error", ex.Message);
        }
    }

    private void StopSynchronization()
    {
        try
        {
            if (!_isSynchronizing)
            {
                return;
            }

            _logger.LogInformation("Stopping data synchronization...");

            _isSynchronizing = false;
            _synchronizationCancellationTokenSource?.Cancel();
            _synchronizationCancellationTokenSource?.Dispose();
            _synchronizationCancellationTokenSource = null;

            _logger.LogInformation("Data synchronization stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping synchronization");
        }
    }

    private async Task SynchronizeDataAsync()
    {
        try
        {
            var now = DateTime.Now;
            
            // Synchronize frequently updated data
            if (!IsDataValid("SystemMetrics", _autoRefreshInterval))
            {
                await RefreshSystemMetricsAsync();
            }

            if (!IsDataValid("HardwareState", _autoRefreshInterval))
            {
                await RefreshHardwareStateAsync();
            }

            if (!IsDataValid("OptimizationResults", _autoRefreshInterval))
            {
                await RefreshOptimizationResultsAsync();
            }

            if (!IsDataValid("ServiceStatus", _autoRefreshInterval))
            {
                await RefreshServiceStatusAsync();
            }

            // Synchronize less frequently updated data
            if (!IsDataValid("SystemInfo", 30000))
            {
                await RefreshSystemInfoAsync();
            }

            if (!IsDataValid("HardwareInfo", 30000))
            {
                await RefreshHardwareInfoAsync();
            }

            if (!IsDataValid("OptimizationHistory", 10000))
            {
                await RefreshOptimizationHistoryAsync(50);
            }

            if (!IsDataValid("HardwareSnapshots", 30000))
            {
                await RefreshHardwareSnapshotsAsync();
            }

            _lastSynchronizationTime = now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing data");
            await NotifySynchronizationError("Data Synchronization Error", ex.Message);
        }
    }

    private async Task RefreshSystemMetricsAsync()
    {
        try
        {
            var metrics = await _backendService.GetCurrentSystemMetrics();
            _cachedSystemMetrics = metrics;
            _lastUpdateTimes["SystemMetrics"] = DateTime.Now;
            
            await NotifyDataUpdated("SystemMetrics", metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing system metrics");
            await NotifySynchronizationError("System Metrics Refresh Error", ex.Message);
        }
    }

    private async Task RefreshHardwareStateAsync()
    {
        try
        {
            var state = await _backendService.GetCurrentHardwareState();
            _cachedHardwareState = state;
            _lastUpdateTimes["HardwareState"] = DateTime.Now;
            
            await NotifyDataUpdated("HardwareState", state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing hardware state");
            await NotifySynchronizationError("Hardware State Refresh Error", ex.Message);
        }
    }

    private async Task RefreshOptimizationResultsAsync()
    {
        try
        {
            var results = await _backendService.GetLastOptimizationResults();
            _cachedOptimizationResults = results;
            _lastUpdateTimes["OptimizationResults"] = DateTime.Now;
            
            await NotifyDataUpdated("OptimizationResults", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing optimization results");
            await NotifySynchronizationError("Optimization Results Refresh Error", ex.Message);
        }
    }

    private async Task RefreshSystemInfoAsync()
    {
        try
        {
            var info = await _backendService.GetSystemInfoAsync();
            _cachedSystemInfo = info;
            _lastUpdateTimes["SystemInfo"] = DateTime.Now;
            
            await NotifyDataUpdated("SystemInfo", info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing system info");
            await NotifySynchronizationError("System Info Refresh Error", ex.Message);
        }
    }

    private async Task RefreshHardwareInfoAsync()
    {
        try
        {
            var info = await _backendService.GetHardwareInfoAsync();
            _cachedHardwareInfo = info;
            _lastUpdateTimes["HardwareInfo"] = DateTime.Now;
            
            await NotifyDataUpdated("HardwareInfo", info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing hardware info");
            await NotifySynchronizationError("Hardware Info Refresh Error", ex.Message);
        }
    }

    private async Task RefreshOptimizationHistoryAsync(int count)
    {
        try
        {
            var history = await _backendService.GetOptimizationHistoryAsync(count);
            _cachedOptimizationHistory = history;
            _lastUpdateTimes["OptimizationHistory"] = DateTime.Now;
            
            await NotifyDataUpdated("OptimizationHistory", history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing optimization history");
            await NotifySynchronizationError("Optimization History Refresh Error", ex.Message);
        }
    }

    private async Task RefreshHardwareSnapshotsAsync()
    {
        try
        {
            var snapshots = await _backendService.GetHardwareSnapshotsAsync();
            _cachedHardwareSnapshots = snapshots;
            _lastUpdateTimes["HardwareSnapshots"] = DateTime.Now;
            
            await NotifyDataUpdated("HardwareSnapshots", snapshots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing hardware snapshots");
            await NotifySynchronizationError("Hardware Snapshots Refresh Error", ex.Message);
        }
    }

    private async Task RefreshServiceStatusAsync()
    {
        try
        {
            var status = await _backendService.GetServiceStatusAsync();
            _cachedServiceStatus = status;
            _lastUpdateTimes["ServiceStatus"] = DateTime.Now;
            
            await NotifyDataUpdated("ServiceStatus", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing service status");
            await NotifySynchronizationError("Service Status Refresh Error", ex.Message);
        }
    }

    private long EstimateCacheSize()
    {
        try
        {
            // Simple estimation of cache size
            long size = 0;
            
            size += EstimateObjectSize(_cachedSystemMetrics);
            size += EstimateObjectSize(_cachedHardwareState);
            size += EstimateObjectSize(_cachedOptimizationResults);
            size += EstimateObjectSize(_cachedSystemInfo);
            size += EstimateObjectSize(_cachedHardwareInfo);
            size += EstimateObjectSize(_cachedOptimizationHistory);
            size += EstimateObjectSize(_cachedHardwareSnapshots);
            size += EstimateObjectSize(_cachedServiceStatus);
            
            return size;
        }
        catch
        {
            return 0;
        }
    }

    private long EstimateObjectSize(object obj)
    {
        try
        {
            if (obj == null) return 0;
            
            // Simple size estimation based on object type
            switch (obj)
            {
                case SystemMetrics _:
                    return 1024; // ~1KB
                case HardwareState _:
                    return 2048; // ~2KB
                case OptimizationResults _:
                    return 1024; // ~1KB
                case SystemInfo _:
                    return 2048; // ~2KB
                case HardwareInfo _:
                    return 2048; // ~2KB
                case List<OptimizationHistoryItem> list:
                    return list.Count * 512; // ~0.5KB per item
                case List<HardwareSnapshotInfo> list:
                    return list.Count * 1024; // ~1KB per item
                case ServiceStatus _:
                    return 1024; // ~1KB
                default:
                    return 512; // Default ~0.5KB
            }
        }
        catch
        {
            return 0;
        }
    }

    private void UnsubscribeFromEvents()
    {
        _backendService.SystemMetricsUpdated -= OnSystemMetricsUpdated;
        _backendService.HardwareStateUpdated -= OnHardwareStateUpdated;
        _backendService.OptimizationCompleted -= OnOptimizationCompleted;
        _backendService.BackendError -= OnBackendError;
    }

    private async Task NotifyDataUpdated(string dataType, object data)
    {
        try
        {
            DataUpdated?.Invoke(this, new DataUpdatedEventArgs(dataType, data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying data updated");
        }
    }

    private async Task NotifyCacheCleared()
    {
        try
        {
            CacheCleared?.Invoke(this, new CacheClearedEventArgs());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying cache cleared");
        }
    }

    private async Task NotifySynchronizationError(string errorType, string errorMessage)
    {
        try
        {
            SynchronizationError?.Invoke(this, new SynchronizationErrorEventArgs(errorType, errorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying synchronization error");
        }
    }

    // Event Handlers
    private void OnSystemMetricsUpdated(object? sender, SystemMetricsUpdatedEventArgs e)
    {
        _ = UpdateSystemMetricsAsync(e.Metrics);
    }

    private void OnHardwareStateUpdated(object? sender, HardwareStateUpdatedEventArgs e)
    {
        _ = UpdateHardwareStateAsync(e.State);
    }

    private void OnOptimizationCompleted(object? sender, OptimizationCompletedEventArgs e)
    {
        _ = UpdateOptimizationResultsAsync(e.Results);
    }

    private void OnBackendError(object? sender, BackendErrorEventArgs e)
    {
        _ = NotifySynchronizationError(e.ErrorType, e.ErrorMessage);
    }

    private async Task UpdateSystemMetricsAsync(SystemMetrics metrics)
    {
        try
        {
            _cachedSystemMetrics = metrics;
            _lastUpdateTimes["SystemMetrics"] = DateTime.Now;
            await NotifyDataUpdated("SystemMetrics", metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system metrics");
        }
    }

    private async Task UpdateHardwareStateAsync(HardwareState state)
    {
        try
        {
            _cachedHardwareState = state;
            _lastUpdateTimes["HardwareState"] = DateTime.Now;
            await NotifyDataUpdated("HardwareState", state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hardware state");
        }
    }

    private async Task UpdateOptimizationResultsAsync(OptimizationResults results)
    {
        try
        {
            _cachedOptimizationResults = results;
            _lastUpdateTimes["OptimizationResults"] = DateTime.Now;
            await NotifyDataUpdated("OptimizationResults", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating optimization results");
        }
    }

    public void Dispose()
    {
        try
        {
            CleanupAsync().Wait();
        }
        catch
        {
            // Ignore disposal errors
        }
    }
}

// Supporting classes
public class CacheStatistics
{
    public int TotalCachedItems { get; set; }
    public DateTime LastSynchronizationTime { get; set; }
    public long CacheSizeBytes { get; set; }
    public List<string> DataTypes { get; set; } = new();
    public Dictionary<string, TimeSpan> DataAges { get; set; } = new();
}

public class SynchronizationStatus
{
    public bool IsInitialized { get; set; }
    public bool IsSynchronizing { get; set; }
    public bool AutoRefreshEnabled { get; set; }
    public int AutoRefreshInterval { get; set; }
    public DateTime LastSynchronizationTime { get; set; }
    public CacheStatistics CacheStatistics { get; set; } = new();
}

// Event argument classes
public class DataUpdatedEventArgs : EventArgs
{
    public string DataType { get; }
    public object Data { get; }

    public DataUpdatedEventArgs(string dataType, object data)
    {
        DataType = dataType;
        Data = data;
    }
}

public class CacheClearedEventArgs : EventArgs
{
    public CacheClearedEventArgs()
    {
    }
}

public class SynchronizationErrorEventArgs : EventArgs
{
    public string ErrorType { get; }
    public string ErrorMessage { get; }

    public SynchronizationErrorEventArgs(string errorType, string errorMessage)
    {
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }
}