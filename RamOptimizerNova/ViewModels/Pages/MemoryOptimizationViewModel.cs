using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using RamOptimizerNova.Models;
using RamOptimizerNova.Services;
using RamOptimizerNova.ViewModels.Base;
using ReactiveUI;

namespace RamOptimizerNova.ViewModels.Pages;

/// <summary>
/// ViewModel for the Memory Optimization page
/// </summary>
public class MemoryOptimizationViewModel : PageBaseViewModel, IThemeAwareViewModel, ISearchablePage
{
    private readonly IMetricsService _metricsService;
    private readonly ISystemService _systemService;
    private readonly IHardwareService _hardwareService;
    private readonly IOptimizationService _optimizationService;
    private readonly IDialogService _dialogService;
    private readonly ILoggerService _loggerService;
    private readonly INavigationService _navigationService;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;

    private ObservableCollection<MemoryProcessItem> _processItems = new();
    private ObservableCollection<PerformanceSnapshot> _memoryUsageHistory = new();
    private ObservableCollection<MemoryDistributionItem> _memoryDistribution = new();

    private MemoryProcessItem? _selectedProcess;
    private string _statusMessage = "Ready";
    private DateTime _lastUpdateTime = DateTime.Now;
    private CancellationTokenSource? _refreshCancellationTokenSource;

    // Optimization settings
    private double _memoryCleanerLevel = 50;
    private double _cacheCleanerLevel = 50;
    private double _standbyListLevel = 50;
    private bool _enableMemoryCompression = true;
    private bool _enableStandbyListTrimming = true;
    private bool _enableWorkingSetOptimization = true;
    private bool _enableSuperFetch = true;
    private bool _enablePrefetch = true;
    private bool _enableMemoryManagementOptimization = true;

    // Commands
    public RelayCommand RefreshCommand { get; }
    public RelayCommand QuickCleanCommand { get; }
    public RelayCommand DeepCleanCommand { get; }
    public RelayCommand AutoOptimizeCommand { get; }
    public RelayCommand ApplySettingsCommand { get; }
    public RelayCommand AddProcessCommand { get; }
    public RelayCommand RemoveSelectedCommand { get; }
    public RelayCommand<MemoryProcessItem> FreezeProcessCommand { get; }
    public RelayCommand<MemoryProcessItem> TerminateProcessCommand { get; }
    public RelayCommand<string> ApplyPresetCommand { get; }
    public RelayCommand ApplyAdvancedSettingsCommand { get; }
    public RelayCommand<string> NavigateToPageCommand { get; }

    // Properties
    public ObservableCollection<MemoryProcessItem> ProcessItems
    {
        get => _processItems;
        set => this.RaiseAndSetIfChanged(ref _processItems, value);
    }

    public ObservableCollection<PerformanceSnapshot> MemoryUsageHistory
    {
        get => _memoryUsageHistory;
        set => this.RaiseAndSetIfChanged(ref _memoryUsageHistory, value);
    }

    public ObservableCollection<MemoryDistributionItem> MemoryDistribution
    {
        get => _memoryDistribution;
        set => this.RaiseAndSetIfChanged(ref _memoryDistribution, value);
    }

    public MemoryProcessItem? SelectedProcess
    {
        get => _selectedProcess;
        set => this.RaiseAndSetIfChanged(ref _selectedProcess, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public DateTime LastUpdateTime
    {
        get => _lastUpdateTime;
        set => this.RaiseAndSetIfChanged(ref _lastUpdateTime, value);
    }

    // Optimization settings
    public double MemoryCleanerLevel
    {
        get => _memoryCleanerLevel;
        set => this.RaiseAndSetIfChanged(ref _memoryCleanerLevel, value);
    }

    public double CacheCleanerLevel
    {
        get => _cacheCleanerLevel;
        set => this.RaiseAndSetIfChanged(ref _cacheCleanerLevel, value);
    }

    public double StandbyListLevel
    {
        get => _standbyListLevel;
        set => this.RaiseAndSetIfChanged(ref _standbyListLevel, value);
    }

    public bool EnableMemoryCompression
    {
        get => _enableMemoryCompression;
        set => this.RaiseAndSetIfChanged(ref _enableMemoryCompression, value);
    }

    public bool EnableStandbyListTrimming
    {
        get => _enableStandbyListTrimming;
        set => this.RaiseAndSetIfChanged(ref _enableStandbyListTrimming, value);
    }

    public bool EnableWorkingSetOptimization
    {
        get => _enableWorkingSetOptimization;
        set => this.RaiseAndSetIfChanged(ref _enableWorkingSetOptimization, value);
    }

    public bool EnableSuperFetch
    {
        get => _enableSuperFetch;
        set => this.RaiseAndSetIfChanged(ref _enableSuperFetch, value);
    }

    public bool EnablePrefetch
    {
        get => _enablePrefetch;
        set => this.RaiseAndSetIfChanged(ref _enablePrefetch, value);
    }

    public bool EnableMemoryManagementOptimization
    {
        get => _enableMemoryManagementOptimization;
        set => this.RaiseAndSetIfChanged(ref _enableMemoryManagementOptimization, value);
    }

    // Memory Metrics
    public double CurrentMemoryUsage => CurrentMetrics?.MemoryMetrics?.MemoryUsage ?? 0;
    public double AvailableMemory => CurrentMetrics?.MemoryMetrics?.AvailableMemory ?? 0;
    public double PageFileUsage => CurrentMetrics?.MemoryMetrics?.PageFileUsage ?? 0;
    public string MemoryInfo => CurrentMetrics?.MemoryMetrics?.MemoryInfo ?? "Unknown";
    public double MemorySpeed => CurrentMetrics?.MemoryMetrics?.MemorySpeed ?? 0;

    public MemoryOptimizationViewModel(
        IMetricsService metricsService,
        ISystemService systemService,
        IHardwareService hardwareService,
        IOptimizationService optimizationService,
        IDialogService dialogService,
        ILoggerService loggerService,
        INavigationService navigationService,
        IPerformanceMonitoringService performanceMonitoringService) : base("Memory Optimization", navigationService)
    {
        _metricsService = metricsService;
        _systemService = systemService;
        _hardwareService = hardwareService;
        _optimizationService = optimizationService;
        _dialogService = dialogService;
        _loggerService = loggerService;
        _navigationService = navigationService;
        _performanceMonitoringService = performanceMonitoringService;

        // Initialize commands
        RefreshCommand = new RelayCommand(RefreshMemoryData);
        QuickCleanCommand = new RelayCommand(QuickCleanMemory);
        DeepCleanCommand = new RelayCommand(DeepCleanMemory);
        AutoOptimizeCommand = new RelayCommand(AutoOptimizeMemory);
        ApplySettingsCommand = new RelayCommand(ApplySettings);
        AddProcessCommand = new RelayCommand(AddProcess);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected);
        FreezeProcessCommand = new RelayCommand<MemoryProcessItem>(FreezeProcess);
        TerminateProcessCommand = new RelayCommand<MemoryProcessItem>(TerminateProcess);
        ApplyPresetCommand = new RelayCommand<string>(ApplyPreset);
        ApplyAdvancedSettingsCommand = new RelayCommand(ApplyAdvancedSettings);
        NavigateToPageCommand = new RelayCommand<string>(NavigateToPage);

        // Subscribe to events
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

        // Initialize memory optimization
        InitializeMemoryOptimization();
    }

    public override async Task InitializeAsync()
    {
        try
        {
            await ShowLoadingAsync("Initializing Memory Optimization...", async () =>
            {
                // Initialize performance monitoring
                await _performanceMonitoringService.InitializeAsync();
                await _performanceMonitoringService.StartMonitoringAsync();

                // Get initial metrics
                CurrentMetrics = await _metricsService.GetCurrentMetricsAsync();

                // Initialize memory optimization components
                InitializeProcessItems();
                InitializeMemoryUsageHistory();
                InitializeMemoryDistribution();

                // Start real-time updates
                StartRealTimeUpdates();
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error initializing memory optimization: {ex.Message}");
            await _dialogService.ShowErrorAsync("Memory Optimization Error", $"Failed to initialize memory optimization: {ex.Message}");
        }
    }

    public override async Task CleanupAsync()
    {
        try
        {
            // Stop real-time updates
            StopRealTimeUpdates();

            // Unsubscribe from events
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
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error cleaning up memory optimization: {ex.Message}");
        }
    }

    public override async Task RefreshAsync()
    {
        try
        {
            await RefreshMemoryData();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error refreshing memory optimization: {ex.Message}");
            await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh memory optimization: {ex.Message}");
        }
    }

    public async Task FocusSearchAsync()
    {
        try
        {
            // Focus search control in memory optimization
            await _dialogService.ShowMessageAsync("Search", "Search functionality will be implemented in the next phase.");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error focusing search: {ex.Message}");
        }
    }

    private void InitializeMemoryOptimization()
    {
        try
        {
            // Initialize process items
            InitializeProcessItems();

            // Initialize memory usage history
            InitializeMemoryUsageHistory();

            // Initialize memory distribution
            InitializeMemoryDistribution();
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing memory optimization: {ex.Message}").Wait();
        }
    }

    private void InitializeProcessItems()
    {
        try
        {
            _processItems.Clear();

            // Get top processes by memory usage
            var processes = _systemService.GetTopProcessesByMemory(20).Result;
            
            foreach (var process in processes)
            {
                _processItems.Add(new MemoryProcessItem
                {
                    Name = process.Name,
                    Id = process.Id,
                    MemoryUsage = process.MemoryUsage,
                    MemoryUsageMB = process.MemoryUsageMB,
                    Handles = process.Handles,
                    Status = process.Status,
                    IsSystemProcess = process.IsSystemProcess,
                    Path = process.Path,
                    Priority = process.Priority,
                    Threads = process.Threads
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing process items: {ex.Message}").Wait();
        }
    }

    private void InitializeMemoryUsageHistory()
    {
        try
        {
            _memoryUsageHistory.Clear();

            // Get historical data
            var history = _metricsService.GetMetricsHistoryAsync(TimeSpan.FromMinutes(30)).Result;
            
            foreach (var metric in history.Entries)
            {
                _memoryUsageHistory.Add(new PerformanceSnapshot
                {
                    Timestamp = metric.Timestamp,
                    Value = metric.MemoryMetrics.MemoryUsage,
                    Label = metric.Timestamp.ToString("HH:mm")
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing memory usage history: {ex.Message}").Wait();
        }
    }

    private void InitializeMemoryDistribution()
    {
        try
        {
            _memoryDistribution.Clear();

            // Get memory distribution data
            var distribution = _systemService.GetMemoryDistribution().Result;
            
            foreach (var item in distribution)
            {
                _memoryDistribution.Add(new MemoryDistributionItem
                {
                    Type = item.Type,
                    Size = item.Size,
                    Percentage = item.Percentage,
                    Color = item.Color
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing memory distribution: {ex.Message}").Wait();
        }
    }

    private void StartRealTimeUpdates()
    {
        _refreshCancellationTokenSource = new CancellationTokenSource();
        
        Task.Run(async () =>
        {
            while (!_refreshCancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await UpdateMemoryDataAsync();
                    await Task.Delay(3000, _refreshCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _loggerService.LogErrorAsync($"Error in memory optimization real-time updates: {ex.Message}");
                    await Task.Delay(5000, _refreshCancellationTokenSource.Token); // Wait longer on error
                }
            }
        }, _refreshCancellationTokenSource.Token);
    }

    private void StopRealTimeUpdates()
    {
        _refreshCancellationTokenSource?.Cancel();
        _refreshCancellationTokenSource?.Dispose();
        _refreshCancellationTokenSource = null;
    }

    private async Task UpdateMemoryDataAsync()
    {
        try
        {
            // Update metrics
            CurrentMetrics = await _metricsService.GetCurrentMetricsAsync();

            // Update UI on main thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Update process items
                UpdateProcessItems();

                // Update memory usage history
                UpdateMemoryUsageHistory();

                // Update memory distribution
                UpdateMemoryDistribution();

                // Update last update time
                LastUpdateTime = DateTime.Now;
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error updating memory data: {ex.Message}");
        }
    }

    private void UpdateProcessItems()
    {
        try
        {
            // Update process items with current data
            var processes = _systemService.GetTopProcessesByMemory(20).Result;
            
            for (int i = 0; i < Math.Min(processes.Count, _processItems.Count); i++)
            {
                var process = processes[i];
                var processItem = _processItems[i];
                
                processItem.MemoryUsage = process.MemoryUsage;
                processItem.MemoryUsageMB = process.MemoryUsageMB;
                processItem.Handles = process.Handles;
                processItem.Status = process.Status;
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating process items: {ex.Message}").Wait();
        }
    }

    private void UpdateMemoryUsageHistory()
    {
        try
        {
            // Update memory usage history with new metrics
            var history = _metricsService.GetMetricsHistoryAsync(TimeSpan.FromMinutes(30)).Result;
            
            _memoryUsageHistory.Clear();
            
            foreach (var metric in history.Entries)
            {
                _memoryUsageHistory.Add(new PerformanceSnapshot
                {
                    Timestamp = metric.Timestamp,
                    Value = metric.MemoryMetrics.MemoryUsage,
                    Label = metric.Timestamp.ToString("HH:mm")
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating memory usage history: {ex.Message}").Wait();
        }
    }

    private void UpdateMemoryDistribution()
    {
        try
        {
            // Update memory distribution with new data
            var distribution = _systemService.GetMemoryDistribution().Result;
            
            _memoryDistribution.Clear();
            
            foreach (var item in distribution)
            {
                _memoryDistribution.Add(new MemoryDistributionItem
                {
                    Type = item.Type,
                    Size = item.Size,
                    Percentage = item.Percentage,
                    Color = item.Color
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating memory distribution: {ex.Message}").Wait();
        }
    }

    private void RefreshMemoryData()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                StatusMessage = "Refreshing memory data...";
                
                await UpdateMemoryDataAsync();
                
                await _loggerService.LogAsync("Memory data refreshed successfully");
                StatusMessage = "Memory data refreshed successfully";
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error refreshing memory data: {ex.Message}");
                await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh memory data: {ex.Message}");
                StatusMessage = "Error refreshing memory data";
            }
        });
    }

    private async void QuickCleanMemory()
    {
        try
        {
            StatusMessage = "Starting quick memory clean...";
            
            await _dialogService.ShowMessageAsync("Quick Clean", "Starting quick memory clean...");
            
            // Perform quick memory clean
            await _optimizationService.QuickCleanMemoryAsync();
            
            await _dialogService.ShowMessageAsync("Quick Clean", "Quick memory clean completed successfully!");
            StatusMessage = "Quick memory clean completed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error in quick memory clean: {ex.Message}");
            await _dialogService.ShowErrorAsync("Quick Clean Error", $"Failed to perform quick memory clean: {ex.Message}");
            StatusMessage = "Error in quick memory clean";
        }
    }

    private async void DeepCleanMemory()
    {
        try
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Deep Clean", 
                "Deep clean may affect system performance. Continue?");
            
            if (result)
            {
                StatusMessage = "Starting deep memory clean...";
                
                await _dialogService.ShowMessageAsync("Deep Clean", "Starting deep memory clean...");
                
                // Perform deep memory clean
                await _optimizationService.DeepCleanMemoryAsync();
                
                await _dialogService.ShowMessageAsync("Deep Clean", "Deep memory clean completed successfully!");
                StatusMessage = "Deep memory clean completed successfully";
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error in deep memory clean: {ex.Message}");
            await _dialogService.ShowErrorAsync("Deep Clean Error", $"Failed to perform deep memory clean: {ex.Message}");
            StatusMessage = "Error in deep memory clean";
        }
    }

    private async void AutoOptimizeMemory()
    {
        try
        {
            StatusMessage = "Starting memory auto-optimization...";
            
            await _dialogService.ShowMessageAsync("Auto-Optimization", "Starting memory auto-optimization...");
            
            // Start memory optimization
            await _optimizationService.StartOptimizationAsync("Memory");
            
            await _dialogService.ShowMessageAsync("Auto-Optimization", "Memory auto-optimization completed successfully!");
            StatusMessage = "Memory auto-optimization completed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error auto-optimizing memory: {ex.Message}");
            await _dialogService.ShowErrorAsync("Auto-Optimization Error", $"Failed to auto-optimize memory: {ex.Message}");
            StatusMessage = "Error auto-optimizing memory";
        }
    }

    private async void ApplySettings()
    {
        try
        {
            StatusMessage = "Applying memory settings...";
            
            // Apply memory cleaner settings
            await _systemService.SetMemoryCleanerLevelAsync((int)MemoryCleanerLevel);
            
            // Apply cache cleaner settings
            await _systemService.SetCacheCleanerLevelAsync((int)CacheCleanerLevel);
            
            // Apply standby list settings
            await _systemService.SetStandbyListLevelAsync((int)StandbyListLevel);
            
            await _loggerService.LogAsync("Memory settings applied successfully");
            StatusMessage = "Memory settings applied successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error applying settings: {ex.Message}");
            await _dialogService.ShowErrorAsync("Settings Error", $"Failed to apply settings: {ex.Message}");
            StatusMessage = "Error applying settings";
        }
    }

    private async void AddProcess()
    {
        try
        {
            var result = await _dialogService.ShowInputAsync(
                "Add Process", 
                "Enter process name or path:");
            
            if (!string.IsNullOrEmpty(result))
            {
                StatusMessage = "Adding process...";
                
                // Add process to optimization list
                await _optimizationService.AddProcessAsync(result);
                
                // Refresh process list
                InitializeProcessItems();
                
                await _loggerService.LogAsync($"Process '{result}' added to optimization list");
                StatusMessage = "Process added successfully";
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error adding process: {ex.Message}");
            await _dialogService.ShowErrorAsync("Add Process Error", $"Failed to add process: {ex.Message}");
            StatusMessage = "Error adding process";
        }
    }

    private async void RemoveSelected()
    {
        try
        {
            if (SelectedProcess != null)
            {
                StatusMessage = "Removing process...";
                
                // Remove process from optimization list
                await _optimizationService.RemoveProcessAsync(SelectedProcess.Name);
                
                // Refresh process list
                InitializeProcessItems();
                
                await _loggerService.LogAsync($"Process '{SelectedProcess.Name}' removed from optimization list");
                StatusMessage = "Process removed successfully";
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error removing process: {ex.Message}");
            await _dialogService.ShowErrorAsync("Remove Process Error", $"Failed to remove process: {ex.Message}");
            StatusMessage = "Error removing process";
        }
    }

    private async void FreezeProcess(MemoryProcessItem process)
    {
        try
        {
            StatusMessage = $"Freezing process '{process.Name}'...";
            
            // Freeze process
            await _systemService.FreezeProcessAsync(process.Id);
            
            await _loggerService.LogAsync($"Process '{process.Name}' frozen successfully");
            StatusMessage = "Process frozen successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error freezing process: {ex.Message}");
            await _dialogService.ShowErrorAsync("Freeze Process Error", $"Failed to freeze process: {ex.Message}");
            StatusMessage = "Error freezing process";
        }
    }

    private async void TerminateProcess(MemoryProcessItem process)
    {
        try
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Terminate Process", 
                $"Are you sure you want to terminate '{process.Name}'?");
            
            if (result)
            {
                StatusMessage = $"Terminating process '{process.Name}'...";
                
                // Terminate process
                await _systemService.TerminateProcessAsync(process.Id);
                
                // Refresh process list
                InitializeProcessItems();
                
                await _loggerService.LogAsync($"Process '{process.Name}' terminated successfully");
                StatusMessage = "Process terminated successfully";
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error terminating process: {ex.Message}");
            await _dialogService.ShowErrorAsync("Terminate Process Error", $"Failed to terminate process: {ex.Message}");
            StatusMessage = "Error terminating process";
        }
    }

    private async void ApplyPreset(string preset)
    {
        try
        {
            StatusMessage = $"Applying {preset} preset...";
            
            switch (preset.ToLower())
            {
                case "aggressive":
                    await ApplyAggressivePresetAsync();
                    break;
                case "balanced":
                    await ApplyBalancedPresetAsync();
                    break;
                case "conservative":
                    await ApplyConservativePresetAsync();
                    break;
                case "custom":
                    await ApplyCustomPresetAsync();
                    break;
                default:
                    await _dialogService.ShowMessageAsync("Preset Error", $"Unknown preset: {preset}");
                    break;
            }
            
            await _loggerService.LogAsync($"{preset} preset applied successfully");
            StatusMessage = $"{preset} preset applied successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error applying preset: {ex.Message}");
            await _dialogService.ShowErrorAsync("Preset Error", $"Failed to apply preset: {ex.Message}");
            StatusMessage = "Error applying preset";
        }
    }

    private async Task ApplyAggressivePresetAsync()
    {
        // Set aggressive optimization levels
        MemoryCleanerLevel = 90;
        CacheCleanerLevel = 90;
        StandbyListLevel = 90;
        
        // Enable all optimizations
        EnableMemoryCompression = true;
        EnableStandbyListTrimming = true;
        EnableWorkingSetOptimization = true;
        EnableSuperFetch = false;
        EnablePrefetch = false;
        EnableMemoryManagementOptimization = true;
        
        // Apply settings
        await ApplySettings();
    }

    private async Task ApplyBalancedPresetAsync()
    {
        // Set balanced optimization levels
        MemoryCleanerLevel = 50;
        CacheCleanerLevel = 50;
        StandbyListLevel = 50;
        
        // Enable balanced optimizations
        EnableMemoryCompression = true;
        EnableStandbyListTrimming = true;
        EnableWorkingSetOptimization = true;
        EnableSuperFetch = true;
        EnablePrefetch = true;
        EnableMemoryManagementOptimization = true;
        
        // Apply settings
        await ApplySettings();
    }

    private async Task ApplyConservativePresetAsync()
    {
        // Set conservative optimization levels
        MemoryCleanerLevel = 20;
        CacheCleanerLevel = 20;
        StandbyListLevel = 20;
        
        // Enable conservative optimizations
        EnableMemoryCompression = true;
        EnableStandbyListTrimming = false;
        EnableWorkingSetOptimization = false;
        EnableSuperFetch = true;
        EnablePrefetch = true;
        EnableMemoryManagementOptimization = false;
        
        // Apply settings
        await ApplySettings();
    }

    private async Task ApplyCustomPresetAsync()
    {
        // Apply current settings
        await ApplySettings();
    }

    private async void ApplyAdvancedSettings()
    {
        try
        {
            StatusMessage = "Applying advanced settings...";
            
            // Apply advanced memory settings
            await _systemService.SetMemoryCompressionAsync(EnableMemoryCompression);
            await _systemService.SetStandbyListTrimmingAsync(EnableStandbyListTrimming);
            await _systemService.SetWorkingSetOptimizationAsync(EnableWorkingSetOptimization);
            await _systemService.SetSuperFetchAsync(EnableSuperFetch);
            await _systemService.SetPrefetchAsync(EnablePrefetch);
            await _systemService.SetMemoryManagementOptimizationAsync(EnableMemoryManagementOptimization);
            
            await _loggerService.LogAsync("Advanced settings applied successfully");
            StatusMessage = "Advanced settings applied successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error applying advanced settings: {ex.Message}");
            await _dialogService.ShowErrorAsync("Advanced Settings Error", $"Failed to apply advanced settings: {ex.Message}");
            StatusMessage = "Error applying advanced settings";
        }
    }

    private async void NavigateToPage(string pageName)
    {
        try
        {
            await NavigateToPageAsync(pageName);
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error navigating to page: {ex.Message}");
        }
    }

    // Event Handlers
    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        _ = UpdateMemoryDataAsync();
    }

    private void OnMetricsError(object? sender, MetricsErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Metrics Error", e.ErrorMessage);
        });
    }

    private void OnSystemInfoChanged(object? sender, SystemInfoChangedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Update system info related properties
            OnPropertyChanged(nameof(MemoryInfo));
            OnPropertyChanged(nameof(MemorySpeed));
        });
    }

    private void OnSystemError(object? sender, SystemErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("System Error", e.ErrorMessage);
        });
    }

    private void OnHardwareStatusChanged(object? sender, HardwareStatusChangedEventArgs e)
    {
        _ = UpdateMemoryDataAsync();
    }

    private void OnHardwareError(object? sender, HardwareErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Hardware Error", e.ErrorMessage);
        });
    }

    private void OnOptimizationStatusChanged(object? sender, OptimizationStatusChangedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusMessage = e.Status;
        });
    }

    private void OnOptimizationError(object? sender, OptimizationErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Optimization Error", e.ErrorMessage);
        });
    }

    private void OnPerformanceMetricsUpdated(object? sender, PerformanceMetricsUpdatedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Update current metrics with performance data
            CurrentMetrics = e.Metrics;
            await UpdateMemoryDataAsync();
        });
    }

    private void OnPerformanceAlert(object? sender, PerformanceAlertEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _loggerService.LogWarningAsync($"Performance alert: {e.Alert.Message}");
            StatusMessage = $"Performance alert: {e.Alert.Message}";
        });
    }

    private void OnPerformanceError(object? sender, PerformanceErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Performance Error", e.Message);
            await _loggerService.LogErrorAsync($"Performance error: {e.Message}");
        });
    }

    public async Task OnThemeChangedAsync(ThemeVariant newTheme)
    {
        try
        {
            await _loggerService.LogAsync($"Memory optimization theme changed to {newTheme}");
            await UpdateMemoryDataAsync();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error handling theme change: {ex.Message}");
        }
    }
}

// Supporting classes
public class MemoryProcessItem
{
    public string Name { get; set; } = "";
    public int Id { get; set; }
    public double MemoryUsage { get; set; }
    public double MemoryUsageMB { get; set; }
    public int Handles { get; set; }
    public string Status { get; set; } = "";
    public bool IsSystemProcess { get; set; }
    public string Path { get; set; } = "";
    public string Priority { get; set; } = "";
    public int Threads { get; set; }
}

public class MemoryDistributionItem
{
    public string Type { get; set; } = "";
    public double Size { get; set; }
    public double Percentage { get; set; }
    public string Color { get; set; } = "";
}