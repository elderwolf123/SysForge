using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Threading;
using RamOptimizerNova.Services;
using RamOptimizerNova.ViewModels.Pages;

namespace RamOptimizerNova.ViewModels;

public class MainViewModel : ViewModelBase, IThemeAwareViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IThemeManager _themeManager;
    private readonly ISettingsService _settingsService;
    private readonly IMetricsService _metricsService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ISystemService _systemService;
    private readonly IHardwareService _hardwareService;
    private readonly INetworkService _networkService;
    private readonly ICompressionService _compressionService;
    private readonly IOptimizationService _optimizationService;
    private readonly ILoggerService _loggerService;

    private string _title = "RAM Optimizer Nova";
    private string _status = "Ready";
    private string _memoryUsage = "0 MB";
    private string _cpuUsage = "0%";
    private double _progress = 0;
    private bool _isLoading = false;
    private string _loadingText = "Loading...";
    private PageBaseViewModel? _currentPage;
    private object? _currentPageContent;
    private System.Windows.Size _windowSize = new System.Windows.Size(1200, 800);
    private int _memoryOptimizationCount = 0;
    private int _networkOptimizationCount = 0;
    private int _compressionCount = 0;
    private DateTime _lastUpdateTime = DateTime.Now;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isInitialized = false;

    // Commands
    public RelayCommand<string> NavigateCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand MinimizeCommand { get; }
    public RelayCommand MaximizeCommand { get; }
    public RelayCommand RestoreCommand { get; }
    public RelayCommand CloseCommand { get; }
    public RelayCommand SettingsCommand { get; }
    public RelayCommand HelpCommand { get; }
    public RelayCommand AboutCommand { get; }

    // Events
    public event EventHandler? RequestClose;
    public event EventHandler? RequestMinimize;
    public event EventHandler? RequestMaximize;
    public event EventHandler? RequestRestore;

    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string MemoryUsage
    {
        get => _memoryUsage;
        set => Set(ref _memoryUsage, value);
    }

    public string CPUUsage
    {
        get => _cpuUsage;
        set => Set(ref _cpuUsage, value);
    }

    public double Progress
    {
        get => _progress;
        set => Set(ref _progress, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    public string LoadingText
    {
        get => _loadingText;
        set => Set(ref _loadingText, value);
    }

    public PageBaseViewModel? CurrentPage
    {
        get => _currentPage;
        set => Set(ref _currentPage, value);
    }

    public object? CurrentPageContent
    {
        get => _currentPageContent;
        set => Set(ref _currentPageContent, value);
    }

    public System.Windows.Size WindowSize
    {
        get => _windowSize;
        set => Set(ref _windowSize, value);
    }

    public int MemoryOptimizationCount
    {
        get => _memoryOptimizationCount;
        set => Set(ref _memoryOptimizationCount, value);
    }

    public int NetworkOptimizationCount
    {
        get => _networkOptimizationCount;
        set => Set(ref _networkOptimizationCount, value);
    }

    public int CompressionCount
    {
        get => _compressionCount;
        set => Set(ref _compressionCount, value);
    }

    public DateTime LastUpdateTime
    {
        get => _lastUpdateTime;
        set => Set(ref _lastUpdateTime, value);
    }

    public MainViewModel(
        IServiceProvider serviceProvider,
        IThemeManager themeManager,
        ISettingsService settingsService,
        IMetricsService metricsService,
        INavigationService navigationService,
        IDialogService dialogService,
        ISystemService systemService,
        IHardwareService hardwareService,
        INetworkService networkService,
        ICompressionService compressionService,
        IOptimizationService optimizationService,
        ILoggerService loggerService)
    {
        _serviceProvider = serviceProvider;
        _themeManager = themeManager;
        _settingsService = settingsService;
        _metricsService = metricsService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _systemService = systemService;
        _hardwareService = hardwareService;
        _networkService = networkService;
        _compressionService = compressionService;
        _optimizationService = optimizationService;
        _loggerService = loggerService;

        // Initialize commands
        NavigateCommand = new RelayCommand<string>(NavigateToPage);
        RefreshCommand = new RelayCommand(RefreshCurrentPage);
        MinimizeCommand = new RelayCommand(MinimizeWindow);
        MaximizeCommand = new RelayCommand(MaximizeWindow);
        RestoreCommand = new RelayCommand(RestoreWindow);
        CloseCommand = new RelayCommand(CloseWindow);
        SettingsCommand = new RelayCommand(OpenSettings);
        HelpCommand = new RelayCommand(OpenHelp);
        AboutCommand = new RelayCommand(OpenAbout);

        // Subscribe to navigation events
        _navigationService.NavigationChanged += OnNavigationChanged;
        _navigationService.NavigationFailed += OnNavigationFailed;

        // Subscribe to metrics events
        _metricsService.MetricsUpdated += OnMetricsUpdated;
        _metricsService.MetricsError += OnMetricsError;

        // Subscribe to system events
        _systemService.SystemInfoChanged += OnSystemInfoChanged;
        _systemService.SystemError += OnSystemError;

        // Subscribe to hardware events
        _hardwareService.HardwareStatusChanged += OnHardwareStatusChanged;
        _hardwareService.HardwareError += OnHardwareError;

        // Subscribe to network events
        _networkService.NetworkStatusChanged += OnNetworkStatusChanged;
        _networkService.NetworkError += OnNetworkError;

        // Subscribe to compression events
        _compressionService.CompressionStatusChanged += OnCompressionStatusChanged;
        _compressionService.CompressionError += OnCompressionError;

        // Subscribe to optimization events
        _optimizationService.OptimizationStatusChanged += OnOptimizationStatusChanged;
        _optimizationService.OptimizationError += OnOptimizationError;

        // Subscribe to logger events
        _loggerService.LogMessage += OnLogMessage;
        _loggerService.LogError += OnLogError;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            await ShowLoadingAsync("Initializing...", async () =>
            {
                // Initialize services
                await _settingsService.InitializeAsync();
                await _themeManager.InitializeAsync();
                await _metricsService.InitializeAsync();
                await _navigationService.InitializeAsync();
                await _dialogService.InitializeAsync();
                await _systemService.InitializeAsync();
                await _hardwareService.InitializeAsync();
                await _networkService.InitializeAsync();
                await _compressionService.InitializeAsync();
                await _optimizationService.InitializeAsync();
                await _loggerService.InitializeAsync();

                // Load initial page
                await NavigateToPageAsync("Dashboard");

                // Start real-time updates
                StartRealTimeUpdates();

                // Initialize counters
                InitializeCounters();

                _isInitialized = true;
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error initializing main view model: {ex.Message}");
            await _dialogService.ShowErrorAsync("Initialization Error", $"Failed to initialize application: {ex.Message}");
        }
    }

    public async Task CleanupAsync()
    {
        try
        {
            // Stop real-time updates
            StopRealTimeUpdates();

            // Cleanup services
            await _loggerService.CleanupAsync();
            await _optimizationService.CleanupAsync();
            await _compressionService.CleanupAsync();
            await _networkService.CleanupAsync();
            await _hardwareService.CleanupAsync();
            await _systemService.CleanupAsync();
            await _dialogService.CleanupAsync();
            await _navigationService.CleanupAsync();
            await _metricsService.CleanupAsync();
            await _settingsService.CleanupAsync();

            // Unsubscribe from events
            _navigationService.NavigationChanged -= OnNavigationChanged;
            _navigationService.NavigationFailed -= OnNavigationFailed;
            _metricsService.MetricsUpdated -= OnMetricsUpdated;
            _metricsService.MetricsError -= OnMetricsError;
            _systemService.SystemInfoChanged -= OnSystemInfoChanged;
            _systemService.SystemError -= OnSystemError;
            _hardwareService.HardwareStatusChanged -= OnHardwareStatusChanged;
            _hardwareService.HardwareError -= OnHardwareError;
            _networkService.NetworkStatusChanged -= OnNetworkStatusChanged;
            _networkService.NetworkError -= OnNetworkError;
            _compressionService.CompressionStatusChanged -= OnCompressionStatusChanged;
            _compressionService.CompressionError -= OnCompressionError;
            _optimizationService.OptimizationStatusChanged -= OnOptimizationStatusChanged;
            _optimizationService.OptimizationError -= OnOptimizationError;
            _loggerService.LogMessage -= OnLogMessage;
            _loggerService.LogError -= OnLogError;
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error cleaning up main view model: {ex.Message}");
        }
    }

    private async Task NavigateToPageAsync(string pageName)
    {
        try
        {
            await ShowLoadingAsync($"Loading {pageName}...", async () =>
            {
                var page = await _navigationService.NavigateToAsync(pageName);
                if (page != null)
                {
                    CurrentPage = page;
                    CurrentPageContent = page;
                    Title = $"RAM Optimizer Nova - {page.Title}";
                }
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error navigating to page {pageName}: {ex.Message}");
            await _dialogService.ShowErrorAsync("Navigation Error", $"Failed to navigate to {pageName}: {ex.Message}");
        }
    }

    private void NavigateToPage(string pageName)
    {
        _ = NavigateToPageAsync(pageName);
    }

    private async Task RefreshCurrentPageAsync()
    {
        try
        {
            if (CurrentPage != null)
            {
                await ShowLoadingAsync("Refreshing...", async () =>
                {
                    await CurrentPage.RefreshAsync();
                    await _metricsService.RefreshMetricsAsync();
                });
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error refreshing current page: {ex.Message}");
            await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh page: {ex.Message}");
        }
    }

    private void RefreshCurrentPage()
    {
        _ = RefreshCurrentPageAsync();
    }

    private async Task MinimizeWindowAsync()
    {
        RequestMinimize?.Invoke(this, EventArgs.Empty);
    }

    private void MinimizeWindow()
    {
        _ = MinimizeWindowAsync();
    }

    private async Task MaximizeWindowAsync()
    {
        RequestMaximize?.Invoke(this, EventArgs.Empty);
    }

    private void MaximizeWindow()
    {
        _ = MaximizeWindowAsync();
    }

    private async Task RestoreWindowAsync()
    {
        RequestRestore?.Invoke(this, EventArgs.Empty);
    }

    private void RestoreWindow()
    {
        _ = RestoreWindowAsync();
    }

    private async Task CloseWindowAsync()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void CloseWindow()
    {
        _ = CloseWindowAsync();
    }

    private async Task OpenSettingsAsync()
    {
        try
        {
            await _dialogService.ShowDialogAsync<SettingsViewModel>("Settings");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error opening settings: {ex.Message}");
            await _dialogService.ShowErrorAsync("Settings Error", $"Failed to open settings: {ex.Message}");
        }
    }

    private void OpenSettings()
    {
        _ = OpenSettingsAsync();
    }

    private async Task OpenHelpAsync()
    {
        try
        {
            await _dialogService.ShowDialogAsync<HelpViewModel>("Help");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error opening help: {ex.Message}");
            await _dialogService.ShowErrorAsync("Help Error", $"Failed to open help: {ex.Message}");
        }
    }

    private void OpenHelp()
    {
        _ = OpenHelpAsync();
    }

    private async Task OpenAboutAsync()
    {
        try
        {
            await _dialogService.ShowDialogAsync<AboutViewModel>("About");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error opening about: {ex.Message}");
            await _dialogService.ShowErrorAsync("About Error", $"Failed to open about: {ex.Message}");
        }
    }

    private void OpenAbout()
    {
        _ = OpenAboutAsync();
    }

    private async Task CloseCurrentDialogAsync()
    {
        try
        {
            await _dialogService.CloseCurrentDialogAsync();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error closing current dialog: {ex.Message}");
        }
    }

    private async Task FocusSearchAsync()
    {
        try
        {
            // Focus search control in current page
            if (CurrentPage is ISearchablePage searchablePage)
            {
                await searchablePage.FocusSearchAsync();
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error focusing search: {ex.Message}");
        }
    }

    private async Task ReloadSettingsAsync()
    {
        try
        {
            await _settingsService.InitializeAsync();
            await _themeManager.InitializeAsync();
            await _metricsService.RefreshMetricsAsync();
            
            Status = "Settings reloaded";
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error reloading settings: {ex.Message}");
            Status = "Error reloading settings";
        }
    }

    private void StartRealTimeUpdates()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        
        Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await UpdateMetricsAsync();
                    await UpdateStatusAsync();
                    
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _loggerService.LogErrorAsync($"Error in real-time updates: {ex.Message}");
                    await Task.Delay(5000, _cancellationTokenSource.Token); // Wait longer on error
                }
            }
        }, _cancellationTokenSource.Token);
    }

    private void StopRealTimeUpdates()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private async Task UpdateMetricsAsync()
    {
        try
        {
            var metrics = await _metricsService.GetCurrentMetricsAsync();
            
            MemoryUsage = $"{metrics.MemoryUsageMB:F0} MB";
            CPUUsage = $"{metrics.CPUUsage:F1}%";
            Progress = metrics.OptimizationProgress;
            
            LastUpdateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error updating metrics: {ex.Message}");
        }
    }

    private async Task UpdateStatusAsync()
    {
        try
        {
            var status = await _systemService.GetSystemStatusAsync();
            Status = status;
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error updating status: {ex.Message}");
            Status = "Unknown";
        }
    }

    private void InitializeCounters()
    {
        try
        {
            MemoryOptimizationCount = _optimizationService.GetOptimizationCount("Memory");
            NetworkOptimizationCount = _optimizationService.GetOptimizationCount("Network");
            CompressionCount = _compressionService.GetCompressionCount();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error initializing counters: {ex.Message}");
        }
    }

    private async Task ShowLoadingAsync(string text, Func<Task> action)
    {
        try
        {
            IsLoading = true;
            LoadingText = text;
            
            await action();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error in loading operation: {ex.Message}");
            throw;
        }
        finally
        {
            IsLoading = false;
            LoadingText = "Loading...";
        }
    }

    // Event Handlers
    private void OnNavigationChanged(object? sender, NavigationEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await NavigateToPageAsync(e.PageName);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error handling navigation change: {ex.Message}");
            }
        });
    }

    private void OnNavigationFailed(object? sender, NavigationErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await _dialogService.ShowErrorAsync("Navigation Error", e.ErrorMessage);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error handling navigation failure: {ex.Message}");
            }
        });
    }

    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                MemoryUsage = $"{e.Metrics.MemoryUsageMB:F0} MB";
                CPUUsage = $"{e.Metrics.CPUUsage:F1}%";
                Progress = e.Metrics.OptimizationProgress;
            }
            catch (Exception ex)
            {
                _loggerService.LogErrorAsync($"Error handling metrics update: {ex.Message}").Wait();
            }
        });
    }

    private void OnMetricsError(object? sender, MetricsErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await _dialogService.ShowErrorAsync("Metrics Error", e.ErrorMessage);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error handling metrics error: {ex.Message}");
            }
        });
    }

    private void OnSystemInfoChanged(object? sender, SystemInfoChangedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                Status = e.Status;
            }
            catch (Exception ex)
            {
                _loggerService.LogErrorAsync($"Error handling system info change: {ex.Message}").Wait();
            }
        });
    }

    private void OnSystemError(object? sender, SystemErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await _dialogService.ShowErrorAsync("System Error", e.ErrorMessage);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error handling system error: {ex.Message}");
            }
        });
    }

    private void OnHardwareStatusChanged(object? sender, HardwareStatusChangedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                Status = $"Hardware: {e.Status}";
            }
            catch (Exception ex)
            {
                _loggerService.LogErrorAsync($"Error handling hardware status change: {ex.Message}").Wait();
            }
        });
    }

    private void OnHardwareError(object? sender, HardwareErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await _dialogService.ShowErrorAsync("Hardware Error", e.ErrorMessage);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error handling hardware error: {ex.Message}");
            }
        });
    }

    private void OnNetworkStatusChanged(object? sender, NetworkStatusChangedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                Status = $"Network: {e.Status}";
            }
            catch (Exception ex)
            {
                _loggerService.LogErrorAsync($"Error handling network status change: {ex.Message}").Wait();
            }
        });
    }

    private void OnNetworkError(object? sender, NetworkErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await _dialogService.ShowErrorAsync("Network Error", e.ErrorMessage);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error handling network error: {ex.Message}");
            }
        });
    }

    private void OnCompressionStatusChanged(object? sender, CompressionStatusChangedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                if (e.IsCompleted)
                {
                    CompressionCount++;
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogErrorAsync($"Error handling compression status change: {ex.Message}").Wait();
            }
        });
    }

    private void OnCompressionError(object? sender, CompressionErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await _dialogService.ShowErrorAsync("Compression Error", e.ErrorMessage);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error handling compression error: {ex.Message}");
            }
        });
    }

    private void OnOptimizationStatusChanged(object? sender, OptimizationStatusChangedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                if (e.Type == "Memory")
                {
                    MemoryOptimizationCount++;
                }
                else if (e.Type == "Network")
                {
                    NetworkOptimizationCount++;
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogErrorAsync($"Error handling optimization status change: {ex.Message}").Wait();
            }
        });
    }

    private void OnOptimizationError(object? sender, OptimizationErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await _dialogService.ShowErrorAsync("Optimization Error", e.ErrorMessage);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error handling optimization error: {ex.Message}");
            }
        });
    }

    private void OnLogMessage(object? sender, LogMessageEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                // Update status with log message if it's important
                if (e.Level == LogLevel.Error || e.Level == LogLevel.Warning)
                {
                    Status = $"{e.Message}";
                }
            }
            catch (Exception ex)
            {
                // Ignore errors in log handling
            }
        });
    }

    private void OnLogError(object? sender, LogErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await _dialogService.ShowErrorAsync("Log Error", e.ErrorMessage);
            }
            catch (Exception ex)
            {
                // Ignore errors in error handling
            }
        });
    }

    public async Task OnThemeChangedAsync(ThemeVariant newTheme)
    {
        try
        {
            // Update UI for theme change
            await _themeManager.SetThemeAsync(newTheme);
            
            // Update current page if it supports theme changes
            if (CurrentPage is IThemeAwareViewModel themeAwarePage)
            {
                await themeAwarePage.OnThemeChangedAsync(newTheme);
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error handling theme change: {ex.Message}");
        }
    }
}