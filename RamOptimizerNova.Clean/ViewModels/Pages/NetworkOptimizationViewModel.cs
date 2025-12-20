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
/// ViewModel for the Network Optimization page
/// </summary>
public class NetworkOptimizationViewModel : PageBaseViewModel, IThemeAwareViewModel, ISearchablePage
{
    private readonly IMetricsService _metricsService;
    private readonly ISystemService _systemService;
    private readonly IHardwareService _hardwareService;
    private readonly IOptimizationService _optimizationService;
    private readonly IDialogService _dialogService;
    private readonly ILoggerService _loggerService;
    private readonly INavigationService _navigationService;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly INetworkService _networkService;

    private ObservableCollection<NetworkProcessItem> _processItems = new();
    private ObservableCollection<NetworkHistoryItem> _networkHistory = new();
    private ObservableCollection<string> _optimizationLevels = new();
    private ObservableCollection<string> _optimizationModes = new();

    private NetworkProcessItem? _selectedProcess;
    private string? _selectedOptimizationLevel;
    private string? _selectedOptimizationMode;
    private string _statusMessage = "Ready";
    private DateTime _lastUpdateTime = DateTime.Now;
    private CancellationTokenSource? _refreshCancellationTokenSource;

    // Network settings
    private bool _enableQoS = true;
    private bool _prioritizeGaming = false;
    private bool _prioritizeStreaming = false;
    private bool _throttleBackground = true;
    private bool _autoOptimization = false;
    private bool _networkMonitoring = true;
    private bool _bandwidthLimiting = false;
    private bool _integrateWithSystem = false;
    private bool _enableNotifications = true;
    private bool _enableLogging = true;
    private bool _enableBackup = true;

    // Commands
    public RelayCommand RefreshCommand { get; }
    public RelayCommand QuickOptimizeCommand { get; }
    public RelayCommand AdvancedOptimizeCommand { get; }
    public RelayCommand SettingsCommand { get; }
    public RelayCommand AddProcessCommand { get; }
    public RelayCommand ClearAllCommand { get; }
    public RelayCommand<NetworkProcessItem> PrioritizeProcessCommand { get; }
    public RelayCommand<NetworkProcessItem> ThrottleProcessCommand { get; }
    public RelayCommand<string> ApplyPresetCommand { get; }
    public RelayCommand ApplyAdvancedSettingsCommand { get; }
    public RelayCommand<string> NavigateToPageCommand { get; }

    // Properties
    public ObservableCollection<NetworkProcessItem> ProcessItems
    {
        get => _processItems;
        set => this.RaiseAndSetIfChanged(ref _processItems, value);
    }

    public ObservableCollection<NetworkHistoryItem> NetworkHistory
    {
        get => _networkHistory;
        set => this.RaiseAndSetIfChanged(ref _networkHistory, value);
    }

    public ObservableCollection<string> OptimizationLevels
    {
        get => _optimizationLevels;
        set => this.RaiseAndSetIfChanged(ref _optimizationLevels, value);
    }

    public ObservableCollection<string> OptimizationModes
    {
        get => _optimizationModes;
        set => this.RaiseAndSetIfChanged(ref _optimizationModes, value);
    }

    public NetworkProcessItem? SelectedProcess
    {
        get => _selectedProcess;
        set => this.RaiseAndSetIfChanged(ref _selectedProcess, value);
    }

    public string? SelectedOptimizationLevel
    {
        get => _selectedOptimizationLevel;
        set => this.RaiseAndSetIfChanged(ref _selectedOptimizationLevel, value);
    }

    public string? SelectedOptimizationMode
    {
        get => _selectedOptimizationMode;
        set => this.RaiseAndSetIfChanged(ref _selectedOptimizationMode, value);
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

    // Network metrics
    public string NetworkStatus => _networkService?.NetworkStatus ?? "Unknown";
    public string DownloadSpeed => _networkService?.DownloadSpeed ?? "0 MB/s";
    public string UploadSpeed => _networkService?.UploadSpeed ?? "0 MB/s";
    public string NetworkStats => GetNetworkStats();
    public int ActiveConnections => _networkService?.ActiveConnections ?? 0;
    public double NetworkLoad => _networkService?.NetworkLoad ?? 0;
    public double CurrentProgress => _networkService?.CurrentProgress ?? 0;
    public double QueueProgress => _networkService?.QueueProgress ?? 0;
    public string ProgressDetails => _networkService?.ProgressDetails ?? "";
    public string EstimatedTimeRemaining => _networkService?.EstimatedTimeRemaining ?? "";
    public object BandwidthUsage => GetBandwidthUsage();
    public object ConnectionQuality => GetConnectionQuality();
    public object NetworkHistory => GetNetworkHistory();
    public object RecentOptimizations => GetRecentOptimizations();
    public object NetworkStatistics => GetNetworkStatistics();

    // Network settings
    public bool EnableQoS
    {
        get => _enableQoS;
        set => this.RaiseAndSetIfChanged(ref _enableQoS, value);
    }

    public bool PrioritizeGaming
    {
        get => _prioritizeGaming;
        set => this.RaiseAndSetIfChanged(ref _prioritizeGaming, value);
    }

    public bool PrioritizeStreaming
    {
        get => _prioritizeStreaming;
        set => this.RaiseAndSetIfChanged(ref _prioritizeStreaming, value);
    }

    public bool ThrottleBackground
    {
        get => _throttleBackground;
        set => this.RaiseAndSetIfChanged(ref _throttleBackground, value);
    }

    public bool AutoOptimization
    {
        get => _autoOptimization;
        set => this.RaiseAndSetIfChanged(ref _autoOptimization, value);
    }

    public bool NetworkMonitoring
    {
        get => _networkMonitoring;
        set => this.RaiseAndSetIfChanged(ref _networkMonitoring, value);
    }

    public bool BandwidthLimiting
    {
        get => _bandwidthLimiting;
        set => this.RaiseAndSetIfChanged(ref _bandwidthLimiting, value);
    }

    public bool IntegrateWithSystem
    {
        get => _integrateWithSystem;
        set => this.RaiseAndSetIfChanged(ref _integrateWithSystem, value);
    }

    public bool EnableNotifications
    {
        get => _enableNotifications;
        set => this.RaiseAndSetIfChanged(ref _enableNotifications, value);
    }

    public bool EnableLogging
    {
        get => _enableLogging;
        set => this.RaiseAndSetIfChanged(ref _enableLogging, value);
    }

    public bool EnableBackup
    {
        get => _enableBackup;
        set => this.RaiseAndSetIfChanged(ref _enableBackup, value);
    }

    public NetworkOptimizationViewModel(
        IMetricsService metricsService,
        ISystemService systemService,
        IHardwareService hardwareService,
        IOptimizationService optimizationService,
        IDialogService dialogService,
        ILoggerService loggerService,
        INavigationService navigationService,
        IPerformanceMonitoringService performanceMonitoringService,
        INetworkService networkService) : base("Network", navigationService)
    {
        _metricsService = metricsService;
        _systemService = systemService;
        _hardwareService = hardwareService;
        _optimizationService = optimizationService;
        _dialogService = dialogService;
        _loggerService = loggerService;
        _navigationService = navigationService;
        _performanceMonitoringService = performanceMonitoringService;
        _networkService = networkService;

        // Initialize commands
        RefreshCommand = new RelayCommand(RefreshNetworkData);
        QuickOptimizeCommand = new RelayCommand(QuickOptimize);
        AdvancedOptimizeCommand = new RelayCommand(AdvancedOptimize);
        SettingsCommand = new RelayCommand(OpenSettings);
        AddProcessCommand = new RelayCommand(AddProcess);
        ClearAllCommand = new RelayCommand(ClearAll);
        PrioritizeProcessCommand = new RelayCommand<NetworkProcessItem>(PrioritizeProcess);
        ThrottleProcessCommand = new RelayCommand<NetworkProcessItem>(ThrottleProcess);
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
        _networkService.NetworkProgressChanged += OnNetworkProgressChanged;
        _networkService.NetworkCompleted += OnNetworkCompleted;
        _networkService.NetworkError += OnNetworkError;

        // Initialize network
        InitializeNetwork();
    }

    public override async Task InitializeAsync()
    {
        try
        {
            await ShowLoadingAsync("Initializing Network...", async () =>
            {
                // Initialize performance monitoring
                await _performanceMonitoringService.InitializeAsync();
                await _performanceMonitoringService.StartMonitoringAsync();

                // Initialize network service
                await _networkService.InitializeAsync();

                // Get initial metrics
                CurrentMetrics = await _metricsService.GetCurrentMetricsAsync();

                // Initialize network components
                InitializeProcessItems();
                InitializeNetworkHistory();
                InitializeOptimizationLevels();
                InitializeOptimizationModes();

                // Start real-time updates
                StartRealTimeUpdates();
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error initializing network: {ex.Message}");
            await _dialogService.ShowErrorAsync("Network Error", $"Failed to initialize network: {ex.Message}");
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
            _networkService.NetworkProgressChanged -= OnNetworkProgressChanged;
            _networkService.NetworkCompleted -= OnNetworkCompleted;
            _networkService.NetworkError -= OnNetworkError;
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error cleaning up network: {ex.Message}");
        }
    }

    public override async Task RefreshAsync()
    {
        try
        {
            await RefreshNetworkData();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error refreshing network: {ex.Message}");
            await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh network: {ex.Message}");
        }
    }

    public async Task FocusSearchAsync()
    {
        try
        {
            // Focus search control in network
            await _dialogService.ShowMessageAsync("Search", "Search functionality will be implemented in the next phase.");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error focusing search: {ex.Message}");
        }
    }

    private void InitializeNetwork()
    {
        try
        {
            // Initialize process items
            InitializeProcessItems();

            // Initialize network history
            InitializeNetworkHistory();

            // Initialize optimization levels
            InitializeOptimizationLevels();

            // Initialize optimization modes
            InitializeOptimizationModes();
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing network: {ex.Message}").Wait();
        }
    }

    private void InitializeProcessItems()
    {
        try
        {
            _processItems.Clear();

            // Get processes for optimization
            var processes = _networkService.GetNetworkProcesses(50).Result;
            
            foreach (var process in processes)
            {
                _processItems.Add(new NetworkProcessItem
                {
                    ProcessName = process.ProcessName,
                    ProcessId = process.ProcessId,
                    DownloadSpeed = process.DownloadSpeed,
                    UploadSpeed = process.UploadSpeed,
                    Priority = process.Priority,
                    Status = process.Status,
                    IsSelected = false,
                    IsGaming = process.IsGaming,
                    IsStreaming = process.IsStreaming,
                    IsBackground = process.IsBackground
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing process items: {ex.Message}").Wait();
        }
    }

    private void InitializeNetworkHistory()
    {
        try
        {
            _networkHistory.Clear();

            // Get network history
            var history = _networkService.GetNetworkHistoryAsync(30).Result;
            
            foreach (var item in history)
            {
                _networkHistory.Add(new NetworkHistoryItem
                {
                    OptimizationDate = item.OptimizationDate,
                    ProcessesOptimized = item.ProcessesOptimized,
                    BandwidthSaved = item.BandwidthSaved,
                    OptimizationLevel = item.OptimizationLevel,
                    Status = item.Status
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing network history: {ex.Message}").Wait();
        }
    }

    private void InitializeOptimizationLevels()
    {
        try
        {
            _optimizationLevels.Clear();

            // Add optimization levels
            _optimizationLevels.Add("Basic");
            _optimizationLevels.Add("Standard");
            _optimizationLevels.Add("Advanced");
            _optimizationLevels.Add("Maximum");

            // Set default
            SelectedOptimizationLevel = "Standard";
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing optimization levels: {ex.Message}").Wait();
        }
    }

    private void InitializeOptimizationModes()
    {
        try
        {
            _optimizationModes.Clear();

            // Add optimization modes
            _optimizationModes.Add("Single Process");
            _optimizationModes.Add("Batch");
            _optimizationModes.Add("Automatic");
            _optimizationModes.Add("Scheduled");

            // Set default
            SelectedOptimizationMode = "Batch";
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing optimization modes: {ex.Message}").Wait();
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
                    await UpdateNetworkDataAsync();
                    await Task.Delay(3000, _refreshCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _loggerService.LogErrorAsync($"Error in network real-time updates: {ex.Message}");
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

    private async Task UpdateNetworkDataAsync()
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

                // Update network history
                UpdateNetworkHistory();

                // Update last update time
                LastUpdateTime = DateTime.Now;
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error updating network data: {ex.Message}");
        }
    }

    private void UpdateProcessItems()
    {
        try
        {
            // Update process items with current data
            var processes = _networkService.GetNetworkProcesses(50).Result;
            
            for (int i = 0; i < Math.Min(processes.Count, _processItems.Count); i++)
            {
                var process = processes[i];
                var processItem = _processItems[i];
                
                processItem.ProcessName = process.ProcessName;
                processItem.ProcessId = process.ProcessId;
                processItem.DownloadSpeed = process.DownloadSpeed;
                processItem.UploadSpeed = process.UploadSpeed;
                processItem.Priority = process.Priority;
                processItem.Status = process.Status;
                processItem.IsGaming = process.IsGaming;
                processItem.IsStreaming = process.IsStreaming;
                processItem.IsBackground = process.IsBackground;
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating process items: {ex.Message}").Wait();
        }
    }

    private void UpdateNetworkHistory()
    {
        try
        {
            // Update network history with new data
            var history = _networkService.GetNetworkHistoryAsync(30).Result;
            
            _networkHistory.Clear();
            
            foreach (var item in history)
            {
                _networkHistory.Add(new NetworkHistoryItem
                {
                    OptimizationDate = item.OptimizationDate,
                    ProcessesOptimized = item.ProcessesOptimized,
                    BandwidthSaved = item.BandwidthSaved,
                    OptimizationLevel = item.OptimizationLevel,
                    Status = item.Status
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating network history: {ex.Message}").Wait();
        }
    }

    private void RefreshNetworkData()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                StatusMessage = "Refreshing network data...";
                
                await UpdateNetworkDataAsync();
                
                await _loggerService.LogAsync("Network data refreshed successfully");
                StatusMessage = "Network data refreshed successfully";
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error refreshing network data: {ex.Message}");
                await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh network data: {ex.Message}");
                StatusMessage = "Error refreshing network data";
            }
        });
    }

    private async void QuickOptimize()
    {
        try
        {
            StatusMessage = "Starting quick optimization...";
            
            await _dialogService.ShowMessageAsync("Quick Optimize", "Starting quick network optimization...");
            
            // Perform quick optimization
            await _networkService.QuickOptimizeAsync();
            
            await _dialogService.ShowMessageAsync("Quick Optimize", "Quick optimization completed successfully!");
            StatusMessage = "Quick optimization completed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error in quick optimization: {ex.Message}");
            await _dialogService.ShowErrorAsync("Quick Optimize Error", $"Failed to perform quick optimization: {ex.Message}");
            StatusMessage = "Error in quick optimization";
        }
    }

    private async void AdvancedOptimize()
    {
        try
        {
            StatusMessage = "Starting advanced optimization...";
            
            await _dialogService.ShowMessageAsync("Advanced Optimize", "Starting advanced network optimization...");
            
            // Start advanced optimization
            await _networkService.StartAdvancedOptimizeAsync();
            
            await _dialogService.ShowMessageAsync("Advanced Optimize", "Advanced optimization completed successfully!");
            StatusMessage = "Advanced optimization completed successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error in advanced optimization: {ex.Message}");
            await _dialogService.ShowErrorAsync("Advanced Optimize Error", $"Failed to perform advanced optimization: {ex.Message}");
            StatusMessage = "Error in advanced optimization";
        }
    }

    private async void OpenSettings()
    {
        try
        {
            StatusMessage = "Opening network settings...";
            
            // Open network settings dialog
            await _dialogService.ShowMessageAsync("Network Settings", "Network settings dialog will be implemented in the next phase.");
            
            StatusMessage = "Network settings opened successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error opening network settings: {ex.Message}");
            await _dialogService.ShowErrorAsync("Settings Error", $"Failed to open network settings: {ex.Message}");
            StatusMessage = "Error opening network settings";
        }
    }

    private async void AddProcess()
    {
        try
        {
            StatusMessage = "Adding process...";
            
            // Open process selection dialog
            var result = await _dialogService.ShowProcessPickerAsync("Select Process");
            
            if (result != null)
            {
                _processItems.Add(new NetworkProcessItem
                {
                    ProcessName = result.ProcessName,
                    ProcessId = result.ProcessId,
                    DownloadSpeed = "0 MB/s",
                    UploadSpeed = "0 MB/s",
                    Priority = "Normal",
                    Status = "Pending",
                    IsSelected = false,
                    IsGaming = false,
                    IsStreaming = false,
                    IsBackground = false
                });
                
                await _loggerService.LogAsync($"Added process '{result.ProcessName}' to optimization queue");
                StatusMessage = $"Process added successfully";
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error adding process: {ex.Message}");
            await _dialogService.ShowErrorAsync("Add Process Error", $"Failed to add process: {ex.Message}");
            StatusMessage = "Error adding process";
        }
    }

    private async void ClearAll()
    {
        try
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Clear All", 
                "Are you sure you want to clear all processes from the optimization queue?");
            
            if (result)
            {
                StatusMessage = "Clearing all processes...";
                
                _processItems.Clear();
                
                await _loggerService.LogAsync("Cleared all processes from optimization queue");
                StatusMessage = "All processes cleared successfully";
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error clearing all processes: {ex.Message}");
            await _dialogService.ShowErrorAsync("Clear All Error", $"Failed to clear all processes: {ex.Message}");
            StatusMessage = "Error clearing all processes";
        }
    }

    private async void PrioritizeProcess(NetworkProcessItem process)
    {
        try
        {
            StatusMessage = $"Prioritizing process '{process.ProcessName}'...";
            
            // Prioritize individual process
            await _networkService.PrioritizeProcessAsync(process.ProcessId, SelectedOptimizationLevel, SelectedOptimizationMode);
            
            // Update process status
            process.Priority = "High";
            process.Status = "Prioritized";
            
            await _loggerService.LogAsync($"Process '{process.ProcessName}' prioritized successfully");
            StatusMessage = "Process prioritized successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error prioritizing process: {ex.Message}");
            await _dialogService.ShowErrorAsync("Prioritize Process Error", $"Failed to prioritize process: {ex.Message}");
            StatusMessage = "Error prioritizing process";
        }
    }

    private async void ThrottleProcess(NetworkProcessItem process)
    {
        try
        {
            StatusMessage = $"Throttling process '{process.ProcessName}'...";
            
            // Throttle individual process
            await _networkService.ThrottleProcessAsync(process.ProcessId, SelectedOptimizationLevel, SelectedOptimizationMode);
            
            // Update process status
            process.Priority = "Low";
            process.Status = "Throttled";
            
            await _loggerService.LogAsync($"Process '{process.ProcessName}' throttled successfully");
            StatusMessage = "Process throttled successfully";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error throttling process: {ex.Message}");
            await _dialogService.ShowErrorAsync("Throttle Process Error", $"Failed to throttle process: {ex.Message}");
            StatusMessage = "Error throttling process";
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
                case "streaming":
                    await ApplyStreamingPresetAsync();
                    break;
                case "download":
                    await ApplyDownloadPresetAsync();
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
        // Set gaming optimization settings
        SelectedOptimizationLevel = "Advanced";
        SelectedOptimizationMode = "Automatic";
        
        // Enable gaming optimization
        EnableQoS = true;
        PrioritizeGaming = true;
        PrioritizeStreaming = false;
        ThrottleBackground = true;
        AutoOptimization = true;
        
        await _loggerService.LogAsync("Gaming preset applied");
    }

    private async Task ApplyStreamingPresetAsync()
    {
        // Set streaming optimization settings
        SelectedOptimizationLevel = "Advanced";
        SelectedOptimizationMode = "Automatic";
        
        // Enable streaming optimization
        EnableQoS = true;
        PrioritizeGaming = false;
        PrioritizeStreaming = true;
        ThrottleBackground = true;
        AutoOptimization = true;
        
        await _loggerService.LogAsync("Streaming preset applied");
    }

    private async Task ApplyDownloadPresetAsync()
    {
        // Set download optimization settings
        SelectedOptimizationLevel = "Maximum";
        SelectedOptimizationMode = "Batch";
        
        // Enable download optimization
        EnableQoS = false;
        PrioritizeGaming = false;
        PrioritizeStreaming = false;
        ThrottleBackground = false;
        AutoOptimization = false;
        
        await _loggerService.LogAsync("Download preset applied");
    }

    private async Task ApplyBalancedPresetAsync()
    {
        // Set balanced optimization settings
        SelectedOptimizationLevel = "Standard";
        SelectedOptimizationMode = "Automatic";
        
        // Enable balanced optimization
        EnableQoS = true;
        PrioritizeGaming = false;
        PrioritizeStreaming = false;
        ThrottleBackground = true;
        AutoOptimization = true;
        
        await _loggerService.LogAsync("Balanced preset applied");
    }

    private async void ApplyAdvancedSettings()
    {
        try
        {
            StatusMessage = "Applying advanced settings...";
            
            // Apply advanced network settings
            await _networkService.SetNetworkSettingsAsync(
                EnableQoS,
                PrioritizeGaming,
                PrioritizeStreaming,
                ThrottleBackground,
                AutoOptimization,
                NetworkMonitoring,
                BandwidthLimiting,
                IntegrateWithSystem,
                EnableNotifications,
                EnableLogging,
                EnableBackup);
            
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

    private string GetNetworkStats()
    {
        try
        {
            var totalProcesses = ProcessItems.Count;
            var selectedProcesses = ProcessItems.Count(p => p.IsSelected);
            var totalDownload = ProcessItems.Sum(p => ParseSpeed(p.DownloadSpeed));
            var totalUpload = ProcessItems.Sum(p => ParseSpeed(p.UploadSpeed));
            
            return $"Total: {totalProcesses} processes, Download: {totalDownload} MB/s, Upload: {totalUpload} MB/s, Selected: {selectedProcesses} processes";
        }
        catch
        {
            return "No network data available";
        }
    }

    private double ParseSpeed(string speed)
    {
        try
        {
            if (string.IsNullOrEmpty(speed)) return 0;
            
            var parts = speed.Split(' ');
            if (parts.Length != 2) return 0;
            
            var value = double.Parse(parts[0]);
            var unit = parts[1];
            
            return unit switch
            {
                "B/s" => value / 1024 / 1024,
                "KB/s" => value / 1024,
                "MB/s" => value,
                "GB/s" => value * 1024,
                _ => value
            };
        }
        catch
        {
            return 0;
        }
    }

    private object GetBandwidthUsage()
    {
        try
        {
            // Return bandwidth usage as a list
            var usage = ProcessItems.GroupBy(p => p.Priority)
                .OrderByDescending(g => g.Sum(p => ParseSpeed(p.DownloadSpeed) + ParseSpeed(p.UploadSpeed)))
                .Take(10)
                .Select(g => new { Priority = g.Key, Count = g.Count(), Bandwidth = g.Sum(p => ParseSpeed(p.DownloadSpeed) + ParseSpeed(p.UploadSpeed)) })
                .ToList();
            
            return usage;
        }
        catch
        {
            return new List<object>();
        }
    }

    private object GetConnectionQuality()
    {
        try
        {
            // Return connection quality as a list
            var quality = ProcessItems.Where(p => p.IsGaming || p.IsStreaming)
                .OrderByDescending(p => ParseSpeed(p.DownloadSpeed) + ParseSpeed(p.UploadSpeed))
                .Take(10)
                .ToList();
            
            return quality;
        }
        catch
        {
            return new List<NetworkProcessItem>();
        }
    }

    private object GetNetworkHistory()
    {
        try
        {
            // Return network history as a list
            return NetworkHistory.Take(10).ToList();
        }
        catch
        {
            return new List<NetworkHistoryItem>();
        }
    }

    private object GetRecentOptimizations()
    {
        try
        {
            // Return recent optimizations as a list
            return NetworkHistory.Take(10).ToList();
        }
        catch
        {
            return new List<NetworkHistoryItem>();
        }
    }

    private object GetNetworkStatistics()
    {
        try
        {
            // Return network statistics
            return new
            {
                TotalOptimizations = NetworkHistory.Count,
                TotalProcessesOptimized = NetworkHistory.Sum(c => c.ProcessesOptimized),
                TotalBandwidthSaved = NetworkHistory.Sum(c => c.BandwidthSaved),
                AverageBandwidthSaved = NetworkHistory.Any() ? NetworkHistory.Average(c => c.BandwidthSaved) : 0,
                MostUsedOptimizationLevel = NetworkHistory.GroupBy(c => c.OptimizationLevel)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?
                    .Key ?? "None"
            };
        }
        catch
        {
            return new { TotalOptimizations = 0, TotalProcessesOptimized = 0, TotalBandwidthSaved = 0, AverageBandwidthSaved = 0, MostUsedOptimizationLevel = "None" };
        }
    }

    // Event Handlers
    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        _ = UpdateNetworkDataAsync();
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
            OnPropertyChanged(nameof(NetworkStats));
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
        _ = UpdateNetworkDataAsync();
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
            await UpdateNetworkDataAsync();
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

    private void OnNetworkProgressChanged(object? sender, NetworkProgressEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Update network progress
            OnPropertyChanged(nameof(CurrentProgress));
            OnPropertyChanged(nameof(QueueProgress));
            OnPropertyChanged(nameof(ProgressDetails));
            OnPropertyChanged(nameof(EstimatedTimeRemaining));
        });
    }

    private void OnNetworkCompleted(object? sender, NetworkCompletedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _loggerService.LogAsync($"Network optimization completed: {e.ProcessesOptimized} processes optimized, {e.BandwidthSaved} MB/s saved");
            StatusMessage = $"Network optimization completed: {e.ProcessesOptimized} processes optimized, {e.BandwidthSaved} MB/s saved";
            
            // Update process status
            foreach (var process in ProcessItems)
            {
                if (process.IsSelected)
                {
                    process.Status = "Optimized";
                }
            }
        });
    }

    private void OnNetworkError(object? sender, NetworkErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _loggerService.LogErrorAsync($"Network error: {e.Message}");
            await _dialogService.ShowErrorAsync("Network Error", e.Message);
            StatusMessage = $"Network error: {e.Message}";
        });
    }

    public async Task OnThemeChangedAsync(ThemeVariant newTheme)
    {
        try
        {
            await _loggerService.LogAsync($"Network theme changed to {newTheme}");
            await UpdateNetworkDataAsync();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error handling theme change: {ex.Message}");
        }
    }
}

// Supporting classes
public class NetworkProcessItem
{
    public string ProcessName { get; set; } = "";
    public int ProcessId { get; set; }
    public string DownloadSpeed { get; set; } = "";
    public string UploadSpeed { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsSelected { get; set; }
    public bool IsGaming { get; set; }
    public bool IsStreaming { get; set; }
    public bool IsBackground { get; set; }
}

public class NetworkHistoryItem
{
    public DateTime OptimizationDate { get; set; }
    public int ProcessesOptimized { get; set; }
    public double BandwidthSaved { get; set; }
    public string OptimizationLevel { get; set; } = "";
    public string Status { get; set; } = "";
}