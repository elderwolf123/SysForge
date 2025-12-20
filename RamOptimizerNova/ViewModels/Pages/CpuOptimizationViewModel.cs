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
/// ViewModel for the CPU Optimization page
/// </summary>
public class CpuOptimizationViewModel : PageBaseViewModel, IThemeAwareViewModel, ISearchablePage
{
    private readonly IMetricsService _metricsService;
    private readonly ISystemService _systemService;
    private readonly IHardwareService _hardwareService;
    private readonly IOptimizationService _optimizationService;
    private readonly IDialogService _dialogService;
    private readonly ILoggerService _loggerService;
    private readonly INavigationService _navigationService;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;

    private ObservableCollection<CpuProcessItem> _processItems = new();
    private ObservableCollection<string> _powerPlans = new();
    private ObservableCollection<string> _priorityLevels = new();
    private ObservableCollection<string> _coreAffinityOptions = new();
    private ObservableCollection<PerformanceSnapshot> _cpuUsageHistory = new();
    private ObservableCollection<PerformanceSnapshot> _temperatureHistory = new();

    private CpuProcessItem? _selectedProcess;
    private string? _selectedPowerPlan;
    private string? _selectedPriority;
    private string? _selectedCoreAffinity;
    private string _statusMessage = "Ready";
    private DateTime _lastUpdateTime = DateTime.Now;
    private CancellationTokenSource? _refreshCancellationTokenSource;

    // Commands
    public RelayCommand RefreshCommand { get; }
    public RelayCommand AutoOptimizeCommand { get; }
    public RelayCommand ResetAllCommand { get; }
    public RelayCommand ApplySettingsCommand { get; }
    public RelayCommand AddProcessCommand { get; }
    public RelayCommand RemoveSelectedCommand { get; }
    public RelayCommand<CpuProcessItem> BoostProcessCommand { get; }
    public RelayCommand<CpuProcessItem> LimitProcessCommand { get; }
    public RelayCommand<string> ApplyPresetCommand { get; }
    public RelayCommand<string> NavigateToPageCommand { get; }

    // Properties
    public ObservableCollection<CpuProcessItem> ProcessItems
    {
        get => _processItems;
        set => this.RaiseAndSetIfChanged(ref _processItems, value);
    }

    public ObservableCollection<string> PowerPlans
    {
        get => _powerPlans;
        set => this.RaiseAndSetIfChanged(ref _powerPlans, value);
    }

    public ObservableCollection<string> PriorityLevels
    {
        get => _priorityLevels;
        set => this.RaiseAndSetIfChanged(ref _priorityLevels, value);
    }

    public ObservableCollection<string> CoreAffinityOptions
    {
        get => _coreAffinityOptions;
        set => this.RaiseAndSetIfChanged(ref _coreAffinityOptions, value);
    }

    public ObservableCollection<PerformanceSnapshot> CpuUsageHistory
    {
        get => _cpuUsageHistory;
        set => this.RaiseAndSetIfChanged(ref _cpuUsageHistory, value);
    }

    public ObservableCollection<PerformanceSnapshot> TemperatureHistory
    {
        get => _temperatureHistory;
        set => this.RaiseAndSetIfChanged(ref _temperatureHistory, value);
    }

    public CpuProcessItem? SelectedProcess
    {
        get => _selectedProcess;
        set => this.RaiseAndSetIfChanged(ref _selectedProcess, value);
    }

    public string? SelectedPowerPlan
    {
        get => _selectedPowerPlan;
        set => this.RaiseAndSetIfChanged(ref _selectedPowerPlan, value);
    }

    public string? SelectedPriority
    {
        get => _selectedPriority;
        set => this.RaiseAndSetIfChanged(ref _selectedPriority, value);
    }

    public string? SelectedCoreAffinity
    {
        get => _selectedCoreAffinity;
        set => this.RaiseAndSetIfChanged(ref _selectedCoreAffinity, value);
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

    // CPU Metrics
    public double CurrentCpuUsage => CurrentMetrics?.CpuMetrics?.CPUUsage ?? 0;
    public double CurrentTemperature => CurrentMetrics?.HardwareMetrics?.Temperature ?? 0;
    public int ActiveCores => CurrentMetrics?.CpuMetrics?.CoreCount ?? 0;
    public string CpuModel => CurrentMetrics?.SystemInfo?.CpuModel ?? "Unknown";
    public double CpuFrequency => CurrentMetrics?.CpuMetrics?.Frequency ?? 0;

    public CpuOptimizationViewModel(
        IMetricsService metricsService,
        ISystemService systemService,
        IHardwareService hardwareService,
        IOptimizationService optimizationService,
        IDialogService dialogService,
        ILoggerService loggerService,
        INavigationService navigationService,
        IPerformanceMonitoringService performanceMonitoringService) : base("CPU Optimization", navigationService)
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
        RefreshCommand = new RelayCommand(RefreshCpuData);
        AutoOptimizeCommand = new RelayCommand(AutoOptimizeCpu);
        ResetAllCommand = new RelayCommand(ResetAllSettings);
        ApplySettingsCommand = new RelayCommand(ApplySettings);
        AddProcessCommand = new RelayCommand(AddProcess);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected);
        BoostProcessCommand = new RelayCommand<CpuProcessItem>(BoostProcess);
        LimitProcessCommand = new RelayCommand<CpuProcessItem>(LimitProcess);
        ApplyPresetCommand = new RelayCommand<string>(ApplyPreset);
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

        // Initialize CPU optimization
        InitializeCpuOptimization();
    }

    public override async Task InitializeAsync()
    {
        try
        {
            await ShowLoadingAsync("Initializing CPU Optimization...", async () =>
            {
                // Initialize performance monitoring
                await _performanceMonitoringService.InitializeAsync();
                await _performanceMonitoringService.StartMonitoringAsync();

                // Get initial metrics
                CurrentMetrics = await _metricsService.GetCurrentMetricsAsync();

                // Initialize CPU optimization components
                InitializeProcessItems();
                InitializePowerPlans();
                InitializePriorityLevels();
                InitializeCoreAffinityOptions();
                InitializePerformanceHistory();

                // Start real-time updates
                StartRealTimeUpdates();
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error initializing CPU optimization: {ex.Message}");
            await _dialogService.ShowErrorAsync("CPU Optimization Error", $"Failed to initialize CPU optimization: {ex.Message}");
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
            await _loggerService.LogErrorAsync($"Error cleaning up CPU optimization: {ex.Message}");
        }
    }

    public override async Task RefreshAsync()
    {
        try
        {
            await RefreshCpuData();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error refreshing CPU optimization: {ex.Message}");
            await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh CPU optimization: {ex.Message}");
        }
    }

    public async Task FocusSearchAsync()
    {
        try
        {
            // Focus search control in CPU optimization
            await _dialogService.ShowMessageAsync("Search", "Search functionality will be implemented in the next phase.");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error focusing search: {ex.Message}");
        }
    }

    private void InitializeCpuOptimization()
    {
        try
        {
            // Initialize process items
            InitializeProcessItems();

            // Initialize power plans
            InitializePowerPlans();

            // Initialize priority levels
            InitializePriorityLevels();

            // Initialize core affinity options
            InitializeCoreAffinityOptions();

            // Initialize performance history
            InitializePerformanceHistory();
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing CPU optimization: {ex.Message}").Wait();
        }
    }

    private void InitializeProcessItems()
    {
        try
        {
            _processItems.Clear();

            // Get top processes by CPU usage
            var processes = _systemService.GetTopProcessesByCpu(20).Result;
            
            foreach (var process in processes)
            {
                _processItems.Add(new CpuProcessItem
                {
                    Name = process.Name,
                    Id = process.Id,
                    CpuUsage = process.CPUUsage,
                    MemoryUsage = process.MemoryUsageMB,
                    Priority = process.Priority,
                    Status = process.Status,
                    IsSystemProcess = process.IsSystemProcess,
                    Path = process.Path,
                    Affinity = process.Affinity,
                    StartTime = process.StartTime
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing process items: {ex.Message}").Wait();
        }
    }

    private void InitializePowerPlans()
    {
        try
        {
            _powerPlans.Clear();

            // Get available power plans
            var plans = _systemService.GetPowerPlans().Result;
            
            foreach (var plan in plans)
            {
                _powerPlans.Add(plan);
            }

            // Select balanced plan by default
            SelectedPowerPlan = _powerPlans.FirstOrDefault(p => p.Contains("Balanced")) ?? _powerPlans.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing power plans: {ex.Message}").Wait();
        }
    }

    private void InitializePriorityLevels()
    {
        try
        {
            _priorityLevels.Clear();

            // Add priority levels
            _priorityLevels.Add("Realtime");
            _priorityLevels.Add("High");
            _priorityLevels.Add("Above Normal");
            _priorityLevels.Add("Normal");
            _priorityLevels.Add("Below Normal");
            _priorityLevels.Add("Low");

            // Select normal priority by default
            SelectedPriority = "Normal";
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing priority levels: {ex.Message}").Wait();
        }
    }

    private void InitializeCoreAffinityOptions()
    {
        try
        {
            _coreAffinityOptions.Clear();

            // Get available cores
            var coreCount = CurrentMetrics?.CpuMetrics?.CoreCount ?? Environment.ProcessorCount;
            
            // Add core affinity options
            _coreAffinityOptions.Add("All Cores");
            for (int i = 1; i <= coreCount; i++)
            {
                _coreAffinityOptions.Add($"Core {i}");
            }

            // Select all cores by default
            SelectedCoreAffinity = "All Cores";
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing core affinity options: {ex.Message}").Wait();
        }
    }

    private void InitializePerformanceHistory()
    {
        try
        {
            _cpuUsageHistory.Clear();
            _temperatureHistory.Clear();

            // Get historical data
            var history = _metricsService.GetMetricsHistoryAsync(TimeSpan.FromMinutes(30)).Result;
            
            foreach (var metric in history.Entries)
            {
                _cpuUsageHistory.Add(new PerformanceSnapshot
                {
                    Timestamp = metric.Timestamp,
                    Value = metric.CpuMetrics.CPUUsage,
                    Label = metric.Timestamp.ToString("HH:mm")
                });

                _temperatureHistory.Add(new PerformanceSnapshot
                {
                    Timestamp = metric.Timestamp,
                    Value = metric.HardwareMetrics.Temperature,
                    Label = metric.Timestamp.ToString("HH:mm")
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing performance history: {ex.Message}").Wait();
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
                    await UpdateCpuDataAsync();
                    await Task.Delay(3000, _refreshCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _loggerService.LogErrorAsync($"Error in CPU optimization real-time updates: {ex.Message}");
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

    private async Task UpdateCpuDataAsync()
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

                // Update performance history
                UpdatePerformanceHistory();

                // Update last update time
                LastUpdateTime = DateTime.Now;
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error updating CPU data: {ex.Message}");
        }
    }

    private void UpdateProcessItems()
    {
        try
        {
            // Update process items with current data
            var processes = _systemService.GetTopProcessesByCpu(20).Result;
            
            for (int i = 0; i < Math.Min(processes.Count, _processItems.Count); i++)
            {
                var process = processes[i];
                var processItem = _processItems[i];
                
                processItem.CpuUsage = process.CPUUsage;
                processItem.MemoryUsage = process.MemoryUsageMB;
                processItem.Priority = process.Priority;
                processItem.Status = process.Status;
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating process items: {ex.Message}").Wait();
        }
    }

    private void UpdatePerformanceHistory()
    {
        try
        {
            // Update performance history with new metrics
            var history = _metricsService.GetMetricsHistoryAsync(TimeSpan.FromMinutes(30)).Result;
            
            _cpuUsageHistory.Clear();
            _temperatureHistory.Clear();
            
            foreach (var metric in history.Entries)
            {
                _cpuUsageHistory.Add(new PerformanceSnapshot
                {
                    Timestamp = metric.Timestamp,
                    Value = metric.CpuMetrics.CPUUsage,
                    Label = metric.Timestamp.ToString("HH:mm")
                });

                _temperatureHistory.Add(new PerformanceSnapshot
                {
                    Timestamp = metric.Timestamp,
                    Value = metric.HardwareMetrics.Temperature,
                    Label = metric.Timestamp.ToString("HH:mm")
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating performance history: {ex.Message}").Wait();
        }
    }

    private void RefreshCpuData()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                StatusMessage = "Refreshing CPU data...";
                
                await UpdateCpuDataAsync();
                
                await _loggerService.LogAsync("CPU data refreshed successfully");
                StatusMessage = "CPU data refreshed successfully";
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error refreshing CPU data: {ex.Message}");
                await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh CPU data: {ex.Message}");
                StatusMessage = "Error refreshing CPU data";
            }
        });
    }

    private async void AutoOptimizeCpu()
    {
        try
        {
            StatusMessage = "Starting CPU auto-optimization...";
            
            await _dialogService.ShowMessageAsync("Auto-Optimization", "Starting CPU auto-optimization...");
            
            // Start CPU optimization
            await _optimizationService.StartOptimizationAsync("CPU");
            
            await _dialogService.ShowMessageAsync("Auto-Optimization", "CPU auto-optimization completed successfully!");
            StatusMessage = "CPU auto-optimization completed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error auto-optimizing CPU: {ex.Message}");
            await _dialogService.ShowErrorAsync("Auto-Optimization Error", $"Failed to auto-optimize CPU: {ex.Message}");
            StatusMessage = "Error auto-optimizing CPU";
        }
    }

    private async void ResetAllSettings()
    {
        try
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Reset All Settings", 
                "Are you sure you want to reset all CPU optimization settings to default?");
            
            if (result)
            {
                StatusMessage = "Resetting all settings...";
                
                // Reset power plan
                SelectedPowerPlan = _powerPlans.FirstOrDefault(p => p.Contains("Balanced")) ?? _powerPlans.FirstOrDefault();
                
                // Reset priority
                SelectedPriority = "Normal";
                
                // Reset core affinity
                SelectedCoreAffinity = "All Cores";
                
                // Reset process priorities
                await ResetProcessPriorities();
                
                await _loggerService.LogAsync("All CPU optimization settings reset to default");
                StatusMessage = "All settings reset successfully";
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error resetting settings: {ex.Message}");
            await _dialogService.ShowErrorAsync("Reset Error", $"Failed to reset settings: {ex.Message}");
            StatusMessage = "Error resetting settings";
        }
    }

    private async void ApplySettings()
    {
        try
        {
            StatusMessage = "Applying CPU settings...";
            
            // Apply power plan
            if (!string.IsNullOrEmpty(SelectedPowerPlan))
            {
                await _systemService.SetPowerPlanAsync(SelectedPowerPlan);
            }
            
            // Apply default priority
            if (!string.IsNullOrEmpty(SelectedPriority))
            {
                await _systemService.SetDefaultPriorityAsync(SelectedPriority);
            }
            
            // Apply core affinity
            if (!string.IsNullOrEmpty(SelectedCoreAffinity))
            {
                await _systemService.SetCoreAffinityAsync(SelectedCoreAffinity);
            }
            
            await _loggerService.LogAsync("CPU settings applied successfully");
            StatusMessage = "CPU settings applied successfully";
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

    private async void BoostProcess(CpuProcessItem process)
    {
        try
        {
            StatusMessage = $"Boosting process '{process.Name}'...";
            
            // Set high priority for process
            await _systemService.SetProcessPriorityAsync(process.Id, "High");
            
            await _loggerService.LogAsync($"Process '{process.Name}' priority set to High");
            StatusMessage = "Process boosted successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error boosting process: {ex.Message}");
            await _dialogService.ShowErrorAsync("Boost Process Error", $"Failed to boost process: {ex.Message}");
            StatusMessage = "Error boosting process";
        }
    }

    private async void LimitProcess(CpuProcessItem process)
    {
        try
        {
            StatusMessage = $"Limiting process '{process.Name}'...";
            
            // Set low priority for process
            await _systemService.SetProcessPriorityAsync(process.Id, "Below Normal");
            
            await _loggerService.LogAsync($"Process '{process.Name}' priority set to Below Normal");
            StatusMessage = "Process limited successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error limiting process: {ex.Message}");
            await _dialogService.ShowErrorAsync("Limit Process Error", $"Failed to limit process: {ex.Message}");
            StatusMessage = "Error limiting process";
        }
    }

    private async void ApplyPreset(string preset)
    {
        try
        {
            StatusMessage = $"Applying {preset} preset...";
            
            switch (preset.ToLower())
            {
                case "gaming":
                    await ApplyGamingPresetAsync();
                    break;
                case "productivity":
                    await ApplyProductivityPresetAsync();
                    break;
                case "power saver":
                    await ApplyPowerSaverPresetAsync();
                    break;
                case "high performance":
                    await ApplyHighPerformancePresetAsync();
                    break;
                case "balanced":
                    await ApplyBalancedPresetAsync();
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

    private async Task ApplyGamingPresetAsync()
    {
        // Set high performance power plan
        SelectedPowerPlan = _powerPlans.FirstOrDefault(p => p.Contains("High Performance")) ?? _powerPlans.FirstOrDefault();
        await _systemService.SetPowerPlanAsync(SelectedPowerPlan);
        
        // Set high priority for gaming processes
        var gamingProcesses = _processItems.Where(p => p.Name.Contains("game", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var process in gamingProcesses)
        {
            await _systemService.SetProcessPriorityAsync(process.Id, "High");
        }
    }

    private async Task ApplyProductivityPresetAsync()
    {
        // Set balanced power plan
        SelectedPowerPlan = _powerPlans.FirstOrDefault(p => p.Contains("Balanced")) ?? _powerPlans.FirstOrDefault();
        await _systemService.SetPowerPlanAsync(SelectedPowerPlan);
        
        // Set normal priority for productivity processes
        var productivityProcesses = _processItems.Where(p => 
            p.Name.Contains("chrome", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("firefox", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("word", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("excel", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var process in productivityProcesses)
        {
            await _systemService.SetProcessPriorityAsync(process.Id, "Above Normal");
        }
    }

    private async Task ApplyPowerSaverPresetAsync()
    {
        // Set power saver power plan
        SelectedPowerPlan = _powerPlans.FirstOrDefault(p => p.Contains("Power Saver")) ?? _powerPlans.FirstOrDefault();
        await _systemService.SetPowerPlanAsync(SelectedPowerPlan);
        
        // Set low priority for non-essential processes
        var nonEssentialProcesses = _processItems.Where(p => !p.IsSystemProcess).ToList();
        foreach (var process in nonEssentialProcesses)
        {
            await _systemService.SetProcessPriorityAsync(process.Id, "Below Normal");
        }
    }

    private async Task ApplyHighPerformancePresetAsync()
    {
        // Set high performance power plan
        SelectedPowerPlan = _powerPlans.FirstOrDefault(p => p.Contains("High Performance")) ?? _powerPlans.FirstOrDefault();
        await _systemService.SetPowerPlanAsync(SelectedPowerPlan);
        
        // Set high priority for all processes
        var allProcesses = _processItems.ToList();
        foreach (var process in allProcesses)
        {
            await _systemService.SetProcessPriorityAsync(process.Id, "High");
        }
    }

    private async Task ApplyBalancedPresetAsync()
    {
        // Set balanced power plan
        SelectedPowerPlan = _powerPlans.FirstOrDefault(p => p.Contains("Balanced")) ?? _powerPlans.FirstOrDefault();
        await _systemService.SetPowerPlanAsync(SelectedPowerPlan);
        
        // Set normal priority for all processes
        var allProcesses = _processItems.ToList();
        foreach (var process in allProcesses)
        {
            await _systemService.SetProcessPriorityAsync(process.Id, "Normal");
        }
    }

    private async Task ResetProcessPriorities()
    {
        try
        {
            var allProcesses = _processItems.ToList();
            foreach (var process in allProcesses)
            {
                await _systemService.SetProcessPriorityAsync(process.Id, "Normal");
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error resetting process priorities: {ex.Message}");
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
        _ = UpdateCpuDataAsync();
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
            OnPropertyChanged(nameof(CpuModel));
            OnPropertyChanged(nameof(CpuFrequency));
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
        _ = UpdateCpuDataAsync();
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
            await UpdateCpuDataAsync();
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
            await _loggerService.LogAsync($"CPU optimization theme changed to {newTheme}");
            await UpdateCpuDataAsync();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error handling theme change: {ex.Message}");
        }
    }
}

// Supporting classes
public class CpuProcessItem
{
    public string Name { get; set; } = "";
    public int Id { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public string Priority { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsSystemProcess { get; set; }
    public string Path { get; set; } = "";
    public string Affinity { get; set; } = "";
    public DateTime StartTime { get; set; }
}