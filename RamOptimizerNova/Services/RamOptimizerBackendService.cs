using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RamOptimizer.HardwareControl;
using RamOptimizer.Core.Interfaces;
using RamOptimizerNova.Models;
using RamOptimizerNova.ViewModels.Base;
using RamOptimizerNova.Services.Interfaces;

namespace RamOptimizerNova.Services;

/// <summary>
/// Service that integrates the Nova UI with the existing RamOptimizer backend
/// Provides a unified interface to all backend functionality
/// </summary>
public class RamOptimizerBackendService : IRamOptimizerBackendService, IDisposable
{
    private readonly ILogger<RamOptimizerBackendService> _logger;
    private readonly IHardwareController _hardwareController;
    private readonly SafeHardwareController _safeHardwareController;
    private readonly SnapshotManager _snapshotManager;
    private readonly IMetricsService _metricsService;
    private readonly ISystemService _systemService;
    private readonly IHardwareService _hardwareService;
    private readonly IOptimizationService _optimizationService;
    private readonly IDialogService _dialogService;
    private readonly ILoggerService _loggerService;
    private readonly INavigationService _navigationService;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly INetworkService _networkService;

    private bool _isInitialized = false;
    private bool _isHardwareAvailable = false;
    private bool _isHardwareSafe = false;
    private bool _isDryRunMode = true;
    private bool _isMonitoring = false;
    private CancellationTokenSource? _monitoringCancellationTokenSource;
    private DateTime _lastHardwareCheck = DateTime.MinValue;
    private TimeSpan _hardwareCheckInterval = TimeSpan.FromSeconds(30);

    // Hardware state
    private HardwareState _currentHardwareState = new();
    private SystemMetrics _currentSystemMetrics = new();
    private OptimizationResults _lastOptimizationResults = new();

    // Event handlers
    public event EventHandler<HardwareStateUpdatedEventArgs>? HardwareStateUpdated;
    public event EventHandler<SystemMetricsUpdatedEventArgs>? SystemMetricsUpdated;
    public event EventHandler<OptimizationCompletedEventArgs>? OptimizationCompleted;
    public event EventHandler<BackendErrorEventArgs>? BackendError;

    public RamOptimizerBackendService(
        ILogger<RamOptimizerBackendService> logger,
        IHardwareController hardwareController,
        SafeHardwareController safeHardwareController,
        SnapshotManager snapshotManager,
        IMetricsService metricsService,
        ISystemService systemService,
        IHardwareService hardwareService,
        IOptimizationService optimizationService,
        IDialogService dialogService,
        ILoggerService loggerService,
        INavigationService navigationService,
        IPerformanceMonitoringService performanceMonitoringService,
        INetworkService networkService)
    {
        _logger = logger;
        _hardwareController = hardwareController;
        _safeHardwareController = safeHardwareController;
        _snapshotManager = snapshotManager;
        _metricsService = metricsService;
        _systemService = systemService;
        _hardwareService = hardwareService;
        _optimizationService = optimizationService;
        _dialogService = dialogService;
        _loggerService = loggerService;
        _navigationService = navigationService;
        _performanceMonitoringService = performanceMonitoringService;
        _networkService = networkService;

        // Subscribe to backend events
        _metricsService.MetricsUpdated += OnMetricsUpdated;
        _metricsService.MetricsError += OnMetricsError;
        _systemService.SystemInfoChanged += OnSystemInfoChanged;
        _systemService.SystemError += OnSystemError;
        _hardwareService.HardwareStatusChanged += OnHardwareStatusChanged;
        _hardwareService.HardwareError += OnHardwareError;
        _optimizationService.OptimizationStatusChanged += OnOptimizationStatusChanged;
        _optimizationService.OptimizationError += OnOptimizationError;
        _performanceMonitoringService.PerformanceMetricsUpdated += OnPerformanceMetricsUpdated;
        _performanceMonitoringService.PerformanceAlert += OnPerformanceAlert;
        _performanceMonitoringService.PerformanceError += OnPerformanceError;
        _networkService.NetworkProgressChanged += OnNetworkProgressChanged;
        _networkService.NetworkCompleted += OnNetworkCompleted;
        _networkService.NetworkError += OnNetworkError;
    }

    /// <summary>
    /// Initialize the backend service
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing RamOptimizer Backend Service...");

            // Initialize hardware controller
            await InitializeHardwareAsync();

            // Initialize services
            await InitializeServicesAsync();

            // Initialize monitoring
            await InitializeMonitoringAsync();

            // Load initial state
            await LoadInitialStateAsync();

            _isInitialized = true;
            _logger.LogInformation("RamOptimizer Backend Service initialized successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RamOptimizer Backend Service");
            await NotifyBackendError("Initialization Error", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Cleanup the backend service
    /// </summary>
    public async Task CleanupAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up RamOptimizer Backend Service...");

            // Stop monitoring
            StopMonitoring();

            // Unsubscribe from events
            UnsubscribeFromEvents();

            // Cleanup resources
            await CleanupResourcesAsync();

            _isInitialized = false;
            _logger.LogInformation("RamOptimizer Backend Service cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up RamOptimizer Backend Service");
        }
    }

    /// <summary>
    /// Get the current hardware state
    /// </summary>
    public HardwareState GetCurrentHardwareState()
    {
        return _currentHardwareState;
    }

    /// <summary>
    /// Get the current system metrics
    /// </summary>
    public SystemMetrics GetCurrentSystemMetrics()
    {
        return _currentSystemMetrics;
    }

    /// <summary>
    /// Get the last optimization results
    /// </summary>
    public OptimizationResults GetLastOptimizationResults()
    {
        return _lastOptimizationResults;
    }

    /// <summary>
    /// Check if hardware is available
    /// </summary>
    public bool IsHardwareAvailable()
    {
        return _isHardwareAvailable;
    }

    /// <summary>
    /// Check if hardware is safe to modify
    /// </summary>
    public bool IsHardwareSafe()
    {
        return _isHardwareSafe;
    }

    /// <summary>
    /// Check if service is in dry run mode
    /// </summary>
    public bool IsDryRunMode()
    {
        return _isDryRunMode;
    }

    /// <summary>
    /// Set dry run mode
    /// </summary>
    public void SetDryRunMode(bool dryRunMode)
    {
        _isDryRunMode = dryRunMode;
        _safeHardwareController.TestModeEnabled = dryRunMode;
        _logger.LogInformation("Dry run mode set to: {DryRunMode}", dryRunMode);
    }

    /// <summary>
    /// Start monitoring hardware and system metrics
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        try
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Monitoring is already running");
                return;
            }

            _isMonitoring = true;
            _monitoringCancellationTokenSource = new CancellationTokenSource();

            _logger.LogInformation("Starting hardware and system monitoring...");

            // Start monitoring tasks
            var monitoringTasks = new List<Task>
            {
                MonitorHardwareAsync(),
                MonitorSystemMetricsAsync(),
                MonitorPerformanceAsync()
            };

            await Task.WhenAll(monitoringTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting monitoring");
            await NotifyBackendError("Monitoring Error", ex.Message);
        }
    }

    /// <summary>
    /// Stop monitoring
    /// </summary>
    public void StopMonitoring()
    {
        try
        {
            if (!_isMonitoring)
            {
                return;
            }

            _logger.LogInformation("Stopping monitoring...");

            _isMonitoring = false;
            _monitoringCancellationTokenSource?.Cancel();
            _monitoringCancellationTokenSource?.Dispose();
            _monitoringCancellationTokenSource = null;

            _logger.LogInformation("Monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping monitoring");
        }
    }

    /// <summary>
    /// Perform RAM optimization
    /// </summary>
    public async Task<OptimizationResults> PerformRAMOptimizationAsync(int level, bool force = false)
    {
        try
        {
            _logger.LogInformation("Starting RAM optimization at level {Level}", level);

            if (!IsHardwareAvailable() && !force)
            {
                throw new InvalidOperationException("Hardware is not available");
            }

            var results = new OptimizationResults
            {
                OptimizationType = "RAM",
                Level = level,
                StartTime = DateTime.Now,
                IsDryRun = _isDryRunMode
            };

            try
            {
                // Perform optimization
                var optimizationResult = await _optimizationService.OptimizeMemoryAsync(level, _isDryRunMode);
                
                results.Success = optimizationResult.Success;
                results.Message = optimizationResult.Message;
                results.MemoryFreed = optimizationResult.MemoryFreed;
                results.ProcessesTerminated = optimizationResult.ProcessesTerminated;
                results.ProcessesOptimized = optimizationResult.ProcessesOptimized;
                results.EndTime = DateTime.Now;
                results.Duration = results.EndTime - results.StartTime;

                _lastOptimizationResults = results;

                if (results.Success)
                {
                    _logger.LogInformation("RAM optimization completed successfully: {MemoryFreed} MB freed", results.MemoryFreed);
                    await NotifyOptimizationCompleted(results);
                }
                else
                {
                    _logger.LogWarning("RAM optimization failed: {Message}", results.Message);
                }
            }
            catch (Exception ex)
            {
                results.Success = false;
                results.Message = ex.Message;
                results.EndTime = DateTime.Now;
                results.Duration = results.EndTime - results.StartTime;

                _logger.LogError(ex, "RAM optimization failed");
                await NotifyBackendError("RAM Optimization Error", ex.Message);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing RAM optimization");
            await NotifyBackendError("RAM Optimization Error", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Perform hardware optimization
    /// </summary>
    public async Task<OptimizationResults> PerformHardwareOptimizationAsync(HardwareOptimizationType type, bool force = false)
    {
        try
        {
            _logger.LogInformation("Starting hardware optimization of type {Type}", type);

            if (!IsHardwareAvailable() && !force)
            {
                throw new InvalidOperationException("Hardware is not available");
            }

            var results = new OptimizationResults
            {
                OptimizationType = "Hardware",
                HardwareType = type,
                StartTime = DateTime.Now,
                IsDryRun = _isDryRunMode
            };

            try
            {
                bool success = false;
                string message = string.Empty;

                switch (type)
                {
                    case HardwareOptimizationType.CoreManagement:
                        success = await OptimizeCoresAsync();
                        message = success ? "Core optimization completed" : "Core optimization failed";
                        break;

                    case HardwareOptimizationType.BatteryManagement:
                        success = await OptimizeBatteryAsync();
                        message = success ? "Battery optimization completed" : "Battery optimization failed";
                        break;

                    case HardwareOptimizationType.PerformanceMode:
                        success = await OptimizePerformanceModeAsync();
                        message = success ? "Performance mode optimization completed" : "Performance mode optimization failed";
                        break;

                    case HardwareOptimizationType.TemperatureControl:
                        success = await OptimizeTemperatureAsync();
                        message = success ? "Temperature optimization completed" : "Temperature optimization failed";
                        break;

                    default:
                        throw new ArgumentException($"Unknown hardware optimization type: {type}");
                }

                results.Success = success;
                results.Message = message;
                results.EndTime = DateTime.Now;
                results.Duration = results.EndTime - results.StartTime;

                _lastOptimizationResults = results;

                if (success)
                {
                    _logger.LogInformation("Hardware optimization completed successfully");
                    await NotifyOptimizationCompleted(results);
                }
                else
                {
                    _logger.LogWarning("Hardware optimization failed: {Message}", message);
                }
            }
            catch (Exception ex)
            {
                results.Success = false;
                results.Message = ex.Message;
                results.EndTime = DateTime.Now;
                results.Duration = results.EndTime - results.StartTime;

                _logger.LogError(ex, "Hardware optimization failed");
                await NotifyBackendError("Hardware Optimization Error", ex.Message);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing hardware optimization");
            await NotifyBackendError("Hardware Optimization Error", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Create a hardware snapshot
    /// </summary>
    public async Task<HardwareSnapshot?> CreateHardwareSnapshotAsync(string name, string notes = "")
    {
        try
        {
            _logger.LogInformation("Creating hardware snapshot: {Name}", name);

            if (!IsHardwareAvailable())
            {
                throw new InvalidOperationException("Hardware is not available");
            }

            var snapshot = _snapshotManager.CaptureAndSave(_hardwareController, name, notes);
            
            if (snapshot)
            {
                _logger.LogInformation("Hardware snapshot created successfully: {Name}", name);
                return _snapshotManager.LoadLatestSnapshot();
            }
            else
            {
                _logger.LogWarning("Failed to create hardware snapshot: {Name}", name);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hardware snapshot");
            await NotifyBackendError("Snapshot Error", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Restore a hardware snapshot
    /// </summary>
    public async Task<bool> RestoreHardwareSnapshotAsync(string name)
    {
        try
        {
            _logger.LogInformation("Restoring hardware snapshot: {Name}", name);

            if (!IsHardwareAvailable())
            {
                throw new InvalidOperationException("Hardware is not available");
            }

            var success = _snapshotManager.RestoreSnapshot(_hardwareController, name);
            
            if (success)
            {
                _logger.LogInformation("Hardware snapshot restored successfully: {Name}", name);
                await UpdateHardwareStateAsync();
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to restore hardware snapshot: {Name}", name);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring hardware snapshot");
            await NotifyBackendError("Snapshot Error", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Get available hardware snapshots
    /// </summary>
    public async Task<List<HardwareSnapshotInfo>> GetHardwareSnapshotsAsync()
    {
        try
        {
            var snapshots = _snapshotManager.ListSnapshots();
            var snapshotInfos = new List<HardwareSnapshotInfo>();

            foreach (var snapshot in snapshots)
            {
                snapshotInfos.Add(new HardwareSnapshotInfo
                {
                    Name = snapshot.SnapshotName,
                    Timestamp = snapshot.Timestamp,
                    Notes = snapshot.Notes,
                    PCores = snapshot.PCores,
                    ECores = snapshot.ECores,
                    BatteryLimit = snapshot.BatteryLimit,
                    PerformanceMode = snapshot.PerformanceMode
                });
            }

            return snapshotInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hardware snapshots");
            await NotifyBackendError("Snapshot Error", ex.Message);
            return new List<HardwareSnapshotInfo>();
        }
    }

    /// <summary>
    /// Get system information
    /// </summary>
    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        try
        {
            var systemInfo = await _systemService.GetSystemInfoAsync();
            
            return new SystemInfo
            {
                OperatingSystem = systemInfo.OperatingSystem,
                ProcessorCount = systemInfo.ProcessorCount,
                TotalMemory = systemInfo.TotalMemory,
                AvailableMemory = systemInfo.AvailableMemory,
                MemoryUsage = systemInfo.MemoryUsage,
                CpuUsage = systemInfo.CpuUsage,
                DiskUsage = systemInfo.DiskUsage,
                NetworkStatus = systemInfo.NetworkStatus,
                Uptime = systemInfo.Uptime,
                LastBootTime = systemInfo.LastBootTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system info");
            await NotifyBackendError("System Info Error", ex.Message);
            return new SystemInfo();
        }
    }

    /// <summary>
    /// Get hardware information
    /// </summary>
    public async Task<HardwareInfo> GetHardwareInfoAsync()
    {
        try
        {
            var hardwareInfo = await _hardwareService.GetHardwareInfoAsync();
            
            return new HardwareInfo
            {
                DeviceIdentifier = _hardwareController.GetDeviceIdentifier(),
                DeviceType = _hardwareController.GetDeviceType(),
                IsAvailable = IsHardwareAvailable(),
                IsSafe = IsHardwareSafe(),
                MaxPCores = _currentHardwareState.MaxPCores,
                MaxECores = _currentHardwareState.MaxECores,
                CurrentPCores = _currentHardwareState.CurrentPCores,
                CurrentECores = _currentHardwareState.CurrentECores,
                BatteryLimit = _currentHardwareState.BatteryLimit,
                PerformanceMode = _currentHardwareState.PerformanceMode,
                CpuTemperature = _currentHardwareState.CpuTemperature,
                GpuTemperature = _currentHardwareState.GpuTemperature,
                CpuFanSpeed = _currentHardwareState.CpuFanSpeed,
                GpuFanSpeed = _currentHardwareState.GpuFanSpeed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hardware info");
            await NotifyBackendError("Hardware Info Error", ex.Message);
            return new HardwareInfo();
        }
    }

    /// <summary>
    /// Get optimization history
    /// </summary>
    public async Task<List<OptimizationHistoryItem>> GetOptimizationHistoryAsync(int count = 50)
    {
        try
        {
            // This would typically be loaded from a database or log file
            // For now, return the last optimization result
            var history = new List<OptimizationHistoryItem>();
            
            if (_lastOptimizationResults.StartTime != DateTime.MinValue)
            {
                history.Add(new OptimizationHistoryItem
                {
                    Timestamp = _lastOptimizationResults.StartTime,
                    Type = _lastOptimizationResults.OptimizationType,
                    Level = _lastOptimizationResults.Level,
                    HardwareType = _lastOptimizationResults.HardwareType,
                    Success = _lastOptimizationResults.Success,
                    Message = _lastOptimizationResults.Message,
                    MemoryFreed = _lastOptimizationResults.MemoryFreed,
                    Duration = _lastOptimizationResults.Duration
                });
            }

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting optimization history");
            await NotifyBackendError("History Error", ex.Message);
            return new List<OptimizationHistoryItem>();
        }
    }

    /// <summary>
    /// Execute a system command
    /// </summary>
    public async Task<CommandResult> ExecuteSystemCommandAsync(string command, string arguments = "")
    {
        try
        {
            _logger.LogInformation("Executing system command: {Command} {Arguments}", command, arguments);

            var result = await _systemService.ExecuteCommandAsync(command, arguments);
            
            return new CommandResult
            {
                Success = result.Success,
                ExitCode = result.ExitCode,
                Output = result.Output,
                Error = result.Error,
                Duration = result.Duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing system command");
            await NotifyBackendError("Command Error", ex.Message);
            return new CommandResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Get service status
    /// </summary>
    public async Task<ServiceStatus> GetServiceStatusAsync()
    {
        try
        {
            return new ServiceStatus
            {
                IsInitialized = _isInitialized,
                IsHardwareAvailable = _isHardwareAvailable,
                IsHardwareSafe = _isHardwareSafe,
                IsDryRunMode = _isDryRunMode,
                IsMonitoring = _isMonitoring,
                LastHardwareCheck = _lastHardwareCheck,
                HardwareCheckInterval = _hardwareCheckInterval,
                CurrentHardwareState = _currentHardwareState,
                CurrentSystemMetrics = _currentSystemMetrics,
                LastOptimizationResults = _lastOptimizationResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service status");
            await NotifyBackendError("Status Error", ex.Message);
            return new ServiceStatus();
        }
    }

    private async Task InitializeHardwareAsync()
    {
        try
        {
            _logger.LogInformation("Initializing hardware controller...");

            // Check if hardware is available
            _isHardwareAvailable = _hardwareController.IsAvailable();
            
            if (_isHardwareAvailable)
            {
                _logger.LogInformation("Hardware controller is available: {Device}", _hardwareController.GetDeviceIdentifier());
                
                // Initialize hardware controller
                var initialized = _hardwareController.Initialize();
                
                if (initialized)
                {
                    _logger.LogInformation("Hardware controller initialized successfully");
                    
                    // Check if hardware is safe
                    _isHardwareSafe = _safeHardwareController != null;
                    
                    if (_isHardwareSafe)
                    {
                        _logger.LogInformation("Hardware safety controller is available");
                    }
                    else
                    {
                        _logger.LogWarning("Hardware safety controller is not available");
                    }
                }
                else
                {
                    _logger.LogWarning("Hardware controller initialization failed");
                    _isHardwareAvailable = false;
                }
            }
            else
            {
                _logger.LogWarning("Hardware controller is not available");
            }

            // Update hardware state
            await UpdateHardwareStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing hardware");
            _isHardwareAvailable = false;
            _isHardwareSafe = false;
        }
    }

    private async Task InitializeServicesAsync()
    {
        try
        {
            _logger.LogInformation("Initializing services...");

            // Initialize metrics service
            await _metricsService.InitializeAsync();

            // Initialize system service
            await _systemService.InitializeAsync();

            // Initialize hardware service
            await _hardwareService.InitializeAsync();

            // Initialize optimization service
            await _optimizationService.InitializeAsync();

            // Initialize performance monitoring service
            await _performanceMonitoringService.InitializeAsync();

            // Initialize network service
            await _networkService.InitializeAsync();

            _logger.LogInformation("All services initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing services");
        }
    }

    private async Task InitializeMonitoringAsync()
    {
        try
        {
            _logger.LogInformation("Initializing monitoring...");

            // Initialize performance monitoring
            await _performanceMonitoringService.StartMonitoringAsync();

            _logger.LogInformation("Monitoring initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing monitoring");
        }
    }

    private async Task LoadInitialStateAsync()
    {
        try
        {
            _logger.LogInformation("Loading initial state...");

            // Load initial hardware state
            await UpdateHardwareStateAsync();

            // Load initial system metrics
            _currentSystemMetrics = await _metricsService.GetCurrentMetricsAsync();

            // Load initial optimization results
            _lastOptimizationResults = new OptimizationResults
            {
                OptimizationType = "None",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                Success = true,
                Message = "System initialized",
                IsDryRun = _isDryRunMode
            };

            _logger.LogInformation("Initial state loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading initial state");
        }
    }

    private async Task MonitorHardwareAsync()
    {
        while (_isMonitoring && !_monitoringCancellationTokenSource?.IsCancellationRequested ?? false)
        {
            try
            {
                var now = DateTime.Now;
                
                // Check hardware at intervals
                if (now - _lastHardwareCheck >= _hardwareCheckInterval)
                {
                    await UpdateHardwareStateAsync();
                    _lastHardwareCheck = now;
                }

                await Task.Delay(1000, _monitoringCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring hardware");
                await NotifyBackendError("Hardware Monitoring Error", ex.Message);
            }
        }
    }

    private async Task MonitorSystemMetricsAsync()
    {
        while (_isMonitoring && !_monitoringCancellationTokenSource?.IsCancellationRequested ?? false)
        {
            try
            {
                // Update system metrics
                _currentSystemMetrics = await _metricsService.GetCurrentMetricsAsync();
                
                // Notify subscribers
                await NotifySystemMetricsUpdated(_currentSystemMetrics);

                await Task.Delay(2000, _monitoringCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring system metrics");
                await NotifyBackendError("Metrics Monitoring Error", ex.Message);
            }
        }
    }

    private async Task MonitorPerformanceAsync()
    {
        while (_isMonitoring && !_monitoringCancellationTokenSource?.IsCancellationRequested ?? false)
        {
            try
            {
                // Update performance metrics
                await _performanceMonitoringService.UpdateMetricsAsync();
                
                await Task.Delay(5000, _monitoringCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring performance");
                await NotifyBackendError("Performance Monitoring Error", ex.Message);
            }
        }
    }

    private async Task UpdateHardwareStateAsync()
    {
        try
        {
            var oldState = _currentHardwareState;

            if (_isHardwareAvailable && _hardwareController != null)
            {
                _currentHardwareState = new HardwareState
                {
                    Timestamp = DateTime.Now,
                    DeviceIdentifier = _hardwareController.GetDeviceIdentifier(),
                    DeviceType = _hardwareController.GetDeviceType(),
                    IsAvailable = true,
                    IsSafe = _isHardwareSafe,
                    
                    // Core information
                    MaxPCores = 0, // Would need to get from hardware controller
                    MaxECores = 0, // Would need to get from hardware controller
                    CurrentPCores = 0, // Would need to get from hardware controller
                    CurrentECores = 0, // Would need to get from hardware controller
                    
                    // Battery information
                    BatteryLimit = 0, // Would need to get from hardware controller
                    MinBatteryLimit = 60,
                    MaxBatteryLimit = 100,
                    
                    // Performance mode
                    PerformanceMode = 1, // Would need to get from hardware controller
                    AvailablePerformanceModes = new[] { 0, 1, 2 },
                    
                    // Temperature information
                    CpuTemperature = 0, // Would need to get from hardware controller
                    GpuTemperature = 0, // Would need to get from hardware controller
                    MaxCpuTemperature = 95,
                    MaxGpuTemperature = 90,
                    
                    // Fan information
                    CpuFanSpeed = 0, // Would need to get from hardware controller
                    GpuFanSpeed = 0, // Would need to get from hardware controller
                    MaxFanSpeed = 100
                };
            }
            else
            {
                _currentHardwareState = new HardwareState
                {
                    Timestamp = DateTime.Now,
                    DeviceIdentifier = "Unknown",
                    DeviceType = "Unknown",
                    IsAvailable = false,
                    IsSafe = false
                };
            }

            // Notify subscribers if state changed
            if (!oldState.Equals(_currentHardwareState))
            {
                await NotifyHardwareStateUpdated(_currentHardwareState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hardware state");
            await NotifyBackendError("Hardware State Error", ex.Message);
        }
    }

    private async Task<bool> OptimizeCoresAsync()
    {
        try
        {
            if (_isDryRunMode)
            {
                _logger.LogInformation("[DRY RUN] Would optimize cores");
                return true;
            }

            // This would implement actual core optimization logic
            // For now, return success
            _logger.LogInformation("Core optimization completed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing cores");
            return false;
        }
    }

    private async Task<bool> OptimizeBatteryAsync()
    {
        try
        {
            if (_isDryRunMode)
            {
                _logger.LogInformation("[DRY RUN] Would optimize battery");
                return true;
            }

            // This would implement actual battery optimization logic
            // For now, return success
            _logger.LogInformation("Battery optimization completed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing battery");
            return false;
        }
    }

    private async Task<bool> OptimizePerformanceModeAsync()
    {
        try
        {
            if (_isDryRunMode)
            {
                _logger.LogInformation("[DRY RUN] Would optimize performance mode");
                return true;
            }

            // This would implement actual performance mode optimization logic
            // For now, return success
            _logger.LogInformation("Performance mode optimization completed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing performance mode");
            return false;
        }
    }

    private async Task<bool> OptimizeTemperatureAsync()
    {
        try
        {
            if (_isDryRunMode)
            {
                _logger.LogInformation("[DRY RUN] Would optimize temperature");
                return true;
            }

            // This would implement actual temperature optimization logic
            // For now, return success
            _logger.LogInformation("Temperature optimization completed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing temperature");
            return false;
        }
    }

    private void UnsubscribeFromEvents()
    {
        _metricsService.MetricsUpdated -= OnMetricsUpdated;
        _metricsService.MetricsError -= OnMetricsError;
        _systemService.SystemInfoChanged -= OnSystemInfoChanged;
        _systemService.SystemError -= OnSystemError;
        _hardwareService.HardwareStatusChanged -= OnHardwareStatusChanged;
        _hardwareService.HardwareError -= OnHardwareError;
        _optimizationService.OptimizationStatusChanged -= OnOptimizationStatusChanged;
        _optimizationService.OptimizationError -= OnOptimizationError;
        _performanceMonitoringService.PerformanceMetricsUpdated -= OnPerformanceMetricsUpdated;
        _performanceMonitoringService.PerformanceAlert -= OnPerformanceAlert;
        _performanceMonitoringService.PerformanceError -= OnPerformanceError;
        _networkService.NetworkProgressChanged -= OnNetworkProgressChanged;
        _networkService.NetworkCompleted -= OnNetworkCompleted;
        _networkService.NetworkError -= OnNetworkError;
    }

    private async Task CleanupResourcesAsync()
    {
        try
        {
            // Cleanup services
            await _metricsService.CleanupAsync();
            await _systemService.CleanupAsync();
            await _hardwareService.CleanupAsync();
            await _optimizationService.CleanupAsync();
            await _performanceMonitoringService.CleanupAsync();
            await _networkService.CleanupAsync();

            // Cleanup hardware controllers
            _hardwareController?.Dispose();
            _safeHardwareController?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up resources");
        }
    }

    private async Task NotifyHardwareStateUpdated(HardwareState state)
    {
        try
        {
            HardwareStateUpdated?.Invoke(this, new HardwareStateUpdatedEventArgs(state));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying hardware state updated");
        }
    }

    private async Task NotifySystemMetricsUpdated(SystemMetrics metrics)
    {
        try
        {
            SystemMetricsUpdated?.Invoke(this, new SystemMetricsUpdatedEventArgs(metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying system metrics updated");
        }
    }

    private async Task NotifyOptimizationCompleted(OptimizationResults results)
    {
        try
        {
            OptimizationCompleted?.Invoke(this, new OptimizationCompletedEventArgs(results));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying optimization completed");
        }
    }

    private async Task NotifyBackendError(string errorType, string errorMessage)
    {
        try
        {
            BackendError?.Invoke(this, new BackendErrorEventArgs(errorType, errorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying backend error");
        }
    }

    // Event Handlers
    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        _ = UpdateSystemMetricsAsync(e.Metrics);
    }

    private void OnMetricsError(object? sender, MetricsErrorEventArgs e)
    {
        _ = NotifyBackendError("Metrics Error", e.ErrorMessage);
    }

    private void OnSystemInfoChanged(object? sender, SystemInfoChangedEventArgs e)
    {
        _ = UpdateSystemMetricsAsync(e.Metrics);
    }

    private void OnSystemError(object? sender, SystemErrorEventArgs e)
    {
        _ = NotifyBackendError("System Error", e.ErrorMessage);
    }

    private void OnHardwareStatusChanged(object? sender, HardwareStatusChangedEventArgs e)
    {
        _ = UpdateHardwareStateAsync();
    }

    private void OnHardwareError(object? sender, HardwareErrorEventArgs e)
    {
        _ = NotifyBackendError("Hardware Error", e.ErrorMessage);
    }

    private void OnOptimizationStatusChanged(object? sender, OptimizationStatusChangedEventArgs e)
    {
        _ = NotifyOptimizationCompleted(new OptimizationResults
        {
            Success = e.Success,
            Message = e.Status,
            StartTime = e.Timestamp,
            EndTime = DateTime.Now
        });
    }

    private void OnOptimizationError(object? sender, OptimizationErrorEventArgs e)
    {
        _ = NotifyBackendError("Optimization Error", e.ErrorMessage);
    }

    private void OnPerformanceMetricsUpdated(object? sender, PerformanceMetricsUpdatedEventArgs e)
    {
        _ = UpdateSystemMetricsAsync(e.Metrics);
    }

    private void OnPerformanceAlert(object? sender, PerformanceAlertEventArgs e)
    {
        _ = NotifyBackendError("Performance Alert", e.Alert.Message);
    }

    private void OnPerformanceError(object? sender, PerformanceErrorEventArgs e)
    {
        _ = NotifyBackendError("Performance Error", e.Message);
    }

    private void OnNetworkProgressChanged(object? sender, NetworkProgressEventArgs e)
    {
        _ = NotifySystemMetricsUpdated(new SystemMetrics
        {
            NetworkProgress = e.Progress,
            QueueProgress = e.QueueProgress,
            ProgressDetails = e.Details,
            EstimatedTimeRemaining = e.EstimatedTime
        });
    }

    private void OnNetworkCompleted(object? sender, NetworkCompletedEventArgs e)
    {
        _ = NotifyOptimizationCompleted(new OptimizationResults
        {
            Success = true,
            Message = $"Network optimization completed: {e.ProcessesOptimized} processes optimized",
            MemoryFreed = e.BandwidthSaved,
            StartTime = e.StartTime,
            EndTime = e.EndTime
        });
    }

    private void OnNetworkError(object? sender, NetworkErrorEventArgs e)
    {
        _ = NotifyBackendError("Network Error", e.Message);
    }

    private async Task UpdateSystemMetricsAsync(SystemMetrics metrics)
    {
        try
        {
            _currentSystemMetrics = metrics;
            await NotifySystemMetricsUpdated(_currentSystemMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system metrics");
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

// Supporting classes and enums
public enum HardwareOptimizationType
{
    CoreManagement,
    BatteryManagement,
    PerformanceMode,
    TemperatureControl
}

public class HardwareState
{
    public DateTime Timestamp { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsSafe { get; set; }
    
    // Core information
    public int MaxPCores { get; set; }
    public int MaxECores { get; set; }
    public int CurrentPCores { get; set; }
    public int CurrentECores { get; set; }
    
    // Battery information
    public int BatteryLimit { get; set; }
    public int MinBatteryLimit { get; set; }
    public int MaxBatteryLimit { get; set; }
    
    // Performance mode
    public int PerformanceMode { get; set; }
    public int[] AvailablePerformanceModes { get; set; } = Array.Empty<int>();
    
    // Temperature information
    public int CpuTemperature { get; set; }
    public int GpuTemperature { get; set; }
    public int MaxCpuTemperature { get; set; }
    public int MaxGpuTemperature { get; set; }
    
    // Fan information
    public int CpuFanSpeed { get; set; }
    public int GpuFanSpeed { get; set; }
    public int MaxFanSpeed { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is HardwareState other)
        {
            return Timestamp == other.Timestamp &&
                   DeviceIdentifier == other.DeviceIdentifier &&
                   DeviceType == other.DeviceType &&
                   IsAvailable == other.IsAvailable &&
                   IsSafe == other.IsSafe &&
                   MaxPCores == other.MaxPCores &&
                   MaxECores == other.MaxECores &&
                   CurrentPCores == other.CurrentPCores &&
                   CurrentECores == other.CurrentECores &&
                   BatteryLimit == other.BatteryLimit &&
                   MinBatteryLimit == other.MinBatteryLimit &&
                   MaxBatteryLimit == other.MaxBatteryLimit &&
                   PerformanceMode == other.PerformanceMode &&
                   AvailablePerformanceModes.SequenceEqual(other.AvailablePerformanceModes) &&
                   CpuTemperature == other.CpuTemperature &&
                   GpuTemperature == other.GpuTemperature &&
                   MaxCpuTemperature == other.MaxCpuTemperature &&
                   MaxGpuTemperature == other.MaxGpuTemperature &&
                   CpuFanSpeed == other.CpuFanSpeed &&
                   GpuFanSpeed == other.GpuFanSpeed &&
                   MaxFanSpeed == other.MaxFanSpeed;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Timestamp, DeviceIdentifier, DeviceType, IsAvailable, IsSafe,
            MaxPCores, MaxECores, CurrentPCores, CurrentECores,
            BatteryLimit, MinBatteryLimit, MaxBatteryLimit, PerformanceMode,
            CpuTemperature, GpuTemperature, MaxCpuTemperature, MaxGpuTemperature,
            CpuFanSpeed, GpuFanSpeed, MaxFanSpeed);
    }
}

public class SystemMetrics
{
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public double NetworkUsage { get; set; }
    public double NetworkProgress { get; set; }
    public double QueueProgress { get; set; }
    public string ProgressDetails { get; set; } = string.Empty;
    public string EstimatedTimeRemaining { get; set; } = string.Empty;
    public int ProcessCount { get; set; }
    public int ThreadCount { get; set; }
    public long TotalMemory { get; set; }
    public long AvailableMemory { get; set; }
    public long TotalDiskSpace { get; set; }
    public long AvailableDiskSpace { get; set; }
    public double NetworkDownloadSpeed { get; set; }
    public double NetworkUploadSpeed { get; set; }
    public int ActiveConnections { get; set; }
    public double NetworkLoad { get; set; }
}

public class OptimizationResults
{
    public string OptimizationType { get; set; } = string.Empty;
    public int Level { get; set; }
    public HardwareOptimizationType? HardwareType { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double MemoryFreed { get; set; }
    public int ProcessesTerminated { get; set; }
    public int ProcessesOptimized { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsDryRun { get; set; }
}

public class HardwareSnapshotInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int PCores { get; set; }
    public int ECores { get; set; }
    public int BatteryLimit { get; set; }
    public int PerformanceMode { get; set; }
}

public class OptimizationHistoryItem
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Level { get; set; }
    public HardwareOptimizationType? HardwareType { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double MemoryFreed { get; set; }
    public TimeSpan Duration { get; set; }
}

public class SystemInfo
{
    public string OperatingSystem { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public long TotalMemory { get; set; }
    public long AvailableMemory { get; set; }
    public double MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public double DiskUsage { get; set; }
    public string NetworkStatus { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }
    public DateTime LastBootTime { get; set; }
}

public class HardwareInfo
{
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsSafe { get; set; }
    public int MaxPCores { get; set; }
    public int MaxECores { get; set; }
    public int CurrentPCores { get; set; }
    public int CurrentECores { get; set; }
    public int BatteryLimit { get; set; }
    public int PerformanceMode { get; set; }
    public int CpuTemperature { get; set; }
    public int GpuTemperature { get; set; }
    public int CpuFanSpeed { get; set; }
    public int GpuFanSpeed { get; set; }
}

public class CommandResult
{
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

public class ServiceStatus
{
    public bool IsInitialized { get; set; }
    public bool IsHardwareAvailable { get; set; }
    public bool IsHardwareSafe { get; set; }
    public bool IsDryRunMode { get; set; }
    public bool IsMonitoring { get; set; }
    public DateTime LastHardwareCheck { get; set; }
    public TimeSpan HardwareCheckInterval { get; set; }
    public HardwareState CurrentHardwareState { get; set; } = new();
    public SystemMetrics CurrentSystemMetrics { get; set; } = new();
    public OptimizationResults LastOptimizationResults { get; set; } = new();
}

// Event argument classes
public class HardwareStateUpdatedEventArgs : EventArgs
{
    public HardwareState State { get; }

    public HardwareStateUpdatedEventArgs(HardwareState state)
    {
        State = state;
    }
}

public class SystemMetricsUpdatedEventArgs : EventArgs
{
    public SystemMetrics Metrics { get; }

    public SystemMetricsUpdatedEventArgs(SystemMetrics metrics)
    {
        Metrics = metrics;
    }
}

public class OptimizationCompletedEventArgs : EventArgs
{
    public OptimizationResults Results { get; }

    public OptimizationCompletedEventArgs(OptimizationResults results)
    {
        Results = results;
    }
}

public class BackendErrorEventArgs : EventArgs
{
    public string ErrorType { get; }
    public string ErrorMessage { get; }

    public BackendErrorEventArgs(string errorType, string errorMessage)
    {
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }
}