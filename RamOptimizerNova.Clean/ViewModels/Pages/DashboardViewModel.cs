using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Threading;
using RamOptimizerNova.Services;
using RamOptimizerNova.ViewModels.Pages;

namespace RamOptimizerNova.ViewModels.Pages;

public class DashboardViewModel : PageBaseViewModel, IThemeAwareViewModel, ISearchablePage
{
    private readonly IMetricsService _metricsService;
    private readonly ISystemService _systemService;
    private readonly IHardwareService _hardwareService;
    private readonly INetworkService _networkService;
    private readonly ICompressionService _compressionService;
    private readonly IOptimizationService _optimizationService;
    private readonly IDialogService _dialogService;
    private readonly ILoggerService _loggerService;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;

    private readonly ObservableCollection<DashboardMetricCard> _metricCards = new();
    private readonly ObservableCollection<DashboardProcessItem> _processItems = new();
    private readonly ObservableCollection<DashboardChartItem> _chartData = new();
    private readonly ObservableCollection<DashboardAlertItem> _alertItems = new();
    private readonly ObservableCollection<DashboardQuickAction> _quickActions = new();

    private MetricsModel _currentMetrics = new();
    private SystemInfoModel _systemInfo = new();
    private DashboardChartType _selectedChartType = DashboardChartType.Memory;
    private DashboardViewMode _viewMode = DashboardViewMode.Comprehensive;
    private bool _isRefreshing = false;
    private bool _showProcesses = true;
    private bool _showAlerts = true;
    private bool _showQuickActions = true;
    private string _searchText = "";
    private DateTime _lastUpdateTime = DateTime.Now;
    private CancellationTokenSource? _refreshCancellationTokenSource;

    // Commands
    public RelayCommand RefreshCommand { get; }
    public RelayCommand<string> ChangeChartTypeCommand { get; }
    public RelayCommand ChangeViewModeCommand { get; }
    public RelayCommand ToggleProcessesCommand { get; }
    public RelayCommand ToggleAlertsCommand { get; }
    public RelayCommand ToggleQuickActionsCommand { get; }
    public RelayCommand<string> QuickActionCommand { get; }
    public RelayCommand ViewProcessDetailsCommand { get; }
    public RelayCommand ViewAlertDetailsCommand { get; }
    public RelayCommand OptimizeNowCommand { get; }
    public RelayCommand<string> NavigateToPageCommand { get; }

    public DashboardViewModel(
        IMetricsService metricsService,
        ISystemService systemService,
        IHardwareService hardwareService,
        INetworkService networkService,
        ICompressionService compressionService,
        IOptimizationService optimizationService,
        IDialogService dialogService,
        ILoggerService loggerService,
        INavigationService navigationService,
        IPerformanceMonitoringService performanceMonitoringService) : base("Dashboard", navigationService)
    {
        _metricsService = metricsService;
        _systemService = systemService;
        _hardwareService = hardwareService;
        _networkService = networkService;
        _compressionService = compressionService;
        _optimizationService = optimizationService;
        _dialogService = dialogService;
        _loggerService = loggerService;
        _performanceMonitoringService = performanceMonitoringService;

        // Initialize commands
        RefreshCommand = new RelayCommand(RefreshDashboard);
        ChangeChartTypeCommand = new RelayCommand<string>(ChangeChartType);
        ChangeViewModeCommand = new RelayCommand(ChangeViewMode);
        ToggleProcessesCommand = new RelayCommand(ToggleProcesses);
        ToggleAlertsCommand = new RelayCommand(ToggleAlerts);
        ToggleQuickActionsCommand = new RelayCommand(ToggleQuickActions);
        QuickActionCommand = new RelayCommand<string>(ExecuteQuickAction);
        ViewProcessDetailsCommand = new RelayCommand<ViewProcessDetailsEventArgs>(ViewProcessDetails);
        ViewAlertDetailsCommand = new RelayCommand<ViewAlertDetailsEventArgs>(ViewAlertDetails);
        OptimizeNowCommand = new RelayCommand(OptimizeNow);
        NavigateToPageCommand = new RelayCommand<string>(NavigateToPage);

        // Subscribe to events
        _metricsService.MetricsUpdated += OnMetricsUpdated;
        _metricsService.MetricsError += OnMetricsError;
        _systemService.SystemInfoChanged += OnSystemInfoChanged;
        _systemService.SystemError += OnSystemError;
        _hardwareService.HardwareStatusChanged += OnHardwareStatusChanged;
        _hardwareService.HardwareError += OnHardwareError;
        _networkService.NetworkStatusChanged += OnNetworkStatusChanged;
        _networkService.NetworkError += OnNetworkError;
        _compressionService.CompressionStatusChanged += OnCompressionStatusChanged;
        _compressionService.CompressionError += OnCompressionError;
        _optimizationService.OptimizationStatusChanged += OnOptimizationStatusChanged;
        _optimizationService.OptimizationError += OnOptimizationError;
        _performanceMonitoringService.PerformanceMetricsUpdated += OnPerformanceMetricsUpdated;
        _performanceMonitoringService.PerformanceAlert += OnPerformanceAlert;
        _performanceMonitoringService.PerformanceError += OnPerformanceError;

        // Initialize dashboard
        InitializeDashboard();
    }

    public override async Task InitializeAsync()
    {
        try
        {
            await ShowLoadingAsync("Initializing Dashboard...", async () =>
            {
                // Initialize performance monitoring
                await _performanceMonitoringService.InitializeAsync();
                await _performanceMonitoringService.StartMonitoringAsync();

                // Get initial metrics
                _currentMetrics = await _metricsService.GetCurrentMetricsAsync();
                _systemInfo = await _systemService.GetSystemInfoAsync();

                // Initialize dashboard components
                InitializeMetricCards();
                InitializeProcessItems();
                InitializeChartData();
                InitializeAlertItems();
                InitializeQuickActions();

                // Start real-time updates
                StartRealTimeUpdates();
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error initializing dashboard: {ex.Message}");
            await _dialogService.ShowErrorAsync("Dashboard Error", $"Failed to initialize dashboard: {ex.Message}");
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
            _networkService.NetworkStatusChanged -= OnNetworkStatusChanged;
            _networkService.NetworkError -= OnNetworkError;
            _compressionService.CompressionStatusChanged -= OnCompressionStatusChanged;
            _compressionService.CompressionError -= OnCompressionError;
            _optimizationService.OptimizationStatusChanged -= OnOptimizationStatusChanged;
            _optimizationService.OptimizationError -= OnOptimizationError;
            _performanceMonitoringService.PerformanceMetricsUpdated -= OnPerformanceMetricsUpdated;
            _performanceMonitoringService.PerformanceAlert -= OnPerformanceAlert;
            _performanceMonitoringService.PerformanceError -= OnPerformanceError;
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error cleaning up dashboard: {ex.Message}");
        }
    }

    public override async Task RefreshAsync()
    {
        try
        {
            await RefreshDashboard();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error refreshing dashboard: {ex.Message}");
            await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh dashboard: {ex.Message}");
        }
    }

    public async Task FocusSearchAsync()
    {
        try
        {
            // Focus search control in dashboard
            await _dialogService.ShowMessageAsync("Search", "Search functionality will be implemented in the next phase.");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error focusing search: {ex.Message}");
        }
    }

    public ObservableCollection<DashboardMetricCard> MetricCards => _metricCards;

    public ObservableCollection<DashboardProcessItem> ProcessItems => _processItems;

    public ObservableCollection<DashboardChartItem> ChartData => _chartData;

    public ObservableCollection<DashboardAlertItem> AlertItems => _alertItems;

    public ObservableCollection<DashboardQuickAction> QuickActions => _quickActions;

    public MetricsModel CurrentMetrics
    {
        get => _currentMetrics;
        set => Set(ref _currentMetrics, value);
    }

    public SystemInfoModel SystemInfo
    {
        get => _systemInfo;
        set => Set(ref _systemInfo, value);
    }

    public DashboardChartType SelectedChartType
    {
        get => _selectedChartType;
        set => Set(ref _selectedChartType, value);
    }

    public DashboardViewMode ViewMode
    {
        get => _viewMode;
        set => Set(ref _viewMode, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => Set(ref _isRefreshing, value);
    }

    public bool ShowProcesses
    {
        get => _showProcesses;
        set => Set(ref _showProcesses, value);
    }

    public bool ShowAlerts
    {
        get => _showAlerts;
        set => Set(ref _showAlerts, value);
    }

    public bool ShowQuickActions
    {
        get => _showQuickActions;
        set => Set(ref _showQuickActions, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => Set(ref _searchText, value);
    }

    public DateTime LastUpdateTime
    {
        get => _lastUpdateTime;
        set => Set(ref _lastUpdateTime, value);
    }

    public double MemoryUsagePercentage => CurrentMetrics.MemoryMetrics.MemoryUsage;
    public double CpuUsagePercentage => CurrentMetrics.CpuMetrics.CPUUsage;
    public double NetworkUsagePercentage => CurrentMetrics.NetworkMetrics.NetworkLoadPercentage;
    public double Temperature => CurrentMetrics.HardwareMetrics.Temperature;
    public long TotalMemoryMB => CurrentMetrics.MemoryMetrics.TotalMemoryMB;
    public long UsedMemoryMB => CurrentMetrics.MemoryMetrics.UsedMemoryMB;
    public long AvailableMemoryMB => CurrentMetrics.MemoryMetrics.AvailableMemoryMB;
    public int ActiveProcesses => CurrentMetrics.PerformanceMetrics.ProcessCount;
    public int ActiveConnections => CurrentMetrics.NetworkMetrics.ActiveConnections;
    public int ActiveOptimizations => CurrentMetrics.OptimizationMetrics.ActiveOptimizations;
    public int ActiveCompressions => CurrentMetrics.CompressionMetrics.ActiveCompressions;

    private void InitializeDashboard()
    {
        try
        {
            // Initialize metric cards
            InitializeMetricCards();

            // Initialize process items
            InitializeProcessItems();

            // Initialize chart data
            InitializeChartData();

            // Initialize alert items
            InitializeAlertItems();

            // Initialize quick actions
            InitializeQuickActions();
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing dashboard: {ex.Message}").Wait();
        }
    }

    private void InitializeMetricCards()
    {
        try
        {
            _metricCards.Clear();

            // Memory metric card
            _metricCards.Add(new DashboardMetricCard
            {
                Title = "Memory Usage",
                Value = $"{CurrentMetrics.MemoryMetrics.MemoryUsage:F1}%",
                Icon = "💾",
                Color = GetMetricColor(CurrentMetrics.MemoryMetrics.MemoryUsage, 80, 90),
                Type = DashboardMetricType.Memory,
                Details = $"Used: {CurrentMetrics.MemoryMetrics.UsedMemoryMB:F0} MB\nAvailable: {CurrentMetrics.MemoryMetrics.AvailableMemoryMB:F0} MB",
                Trend = GetTrendValue(CurrentMetrics.MemoryMetrics.MemoryUsage)
            });

            // CPU metric card
            _metricCards.Add(new DashboardMetricCard
            {
                Title = "CPU Usage",
                Value = $"{CurrentMetrics.CpuMetrics.CPUUsage:F1}%",
                Icon = "🖥️",
                Color = GetMetricColor(CurrentMetrics.CpuMetrics.CPUUsage, 70, 85),
                Type = DashboardMetricType.CPU,
                Details = $"Cores: {CurrentMetrics.CpuMetrics.CoreCount}\nTemperature: {CurrentMetrics.CpuMetrics.Temperature:F1}°C",
                Trend = GetTrendValue(CurrentMetrics.CpuMetrics.CPUUsage)
            });

            // Network metric card
            _metricCards.Add(new DashboardMetricCard
            {
                Title = "Network",
                Value = $"{CurrentMetrics.NetworkMetrics.NetworkLoadPercentage:F1}%",
                Icon = "🌐",
                Color = GetMetricColor(CurrentMetrics.NetworkMetrics.NetworkLoadPercentage, 60, 80),
                Type = DashboardMetricType.Network,
                Details = $"Download: {CurrentMetrics.NetworkMetrics.DownloadSpeedMbps:F1} Mbps\nUpload: {CurrentMetrics.NetworkMetrics.UploadSpeedMbps:F1} Mbps",
                Trend = GetTrendValue(CurrentMetrics.NetworkMetrics.NetworkLoadPercentage)
            });

            // Hardware metric card
            _metricCards.Add(new DashboardMetricCard
            {
                Title = "Hardware",
                Value = $"{CurrentMetrics.HardwareMetrics.Temperature:F1}°C",
                Icon = "🔧",
                Color = GetMetricColor(CurrentMetrics.HardwareMetrics.Temperature, 70, 85),
                Type = DashboardMetricType.Hardware,
                Details = $"Status: {CurrentMetrics.HardwareMetrics.HardwareStatus}\nFan Speed: {CurrentMetrics.HardwareMetrics.FanSpeed:F1}%",
                Trend = GetTrendValue(CurrentMetrics.HardwareMetrics.Temperature)
            });

            // Optimization metric card
            _metricCards.Add(new DashboardMetricCard
            {
                Title = "Optimization",
                Value = $"{CurrentMetrics.OptimizationMetrics.OptimizationProgress:F1}%",
                Icon = "⚡",
                Color = GetMetricColor(CurrentMetrics.OptimizationMetrics.OptimizationProgress, 90, 95),
                Type = DashboardMetricType.Optimization,
                Details = $"Active: {CurrentMetrics.OptimizationMetrics.ActiveOptimizations}\nCompleted: {CurrentMetrics.OptimizationMetrics.CompletedOptimizations}",
                Trend = GetTrendValue(CurrentMetrics.OptimizationMetrics.OptimizationProgress)
            });

            // Compression metric card
            _metricCards.Add(new DashboardMetricCard
            {
                Title = "Compression",
                Value = $"{CurrentMetrics.CompressionMetrics.CompressionRatio:P1}",
                Icon = "🗜️",
                Color = GetMetricColor(CurrentMetrics.CompressionMetrics.CompressionRatio * 100, 70, 90),
                Type = DashboardMetricType.Compression,
                Details = $"Active: {CurrentMetrics.CompressionMetrics.ActiveCompressions}\nThroughput: {CurrentMetrics.CompressionMetrics.CompressionThroughput:F1} MB/s",
                Trend = GetTrendValue(CurrentMetrics.CompressionMetrics.CompressionRatio * 100)
            });
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing metric cards: {ex.Message}").Wait();
        }
    }

    private void InitializeProcessItems()
    {
        try
        {
            _processItems.Clear();

            // Get top processes by memory usage
            var processes = _systemService.GetTopProcessesByMemory(10).Result;
            
            foreach (var process in processes)
            {
                _processItems.Add(new DashboardProcessItem
                {
                    Name = process.Name,
                    Id = process.Id,
                    MemoryUsage = process.MemoryUsageMB,
                    CpuUsage = process.CPUUsage,
                    Priority = process.Priority,
                    Status = process.Status,
                    IsSystemProcess = process.IsSystemProcess,
                    Path = process.Path
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing process items: {ex.Message}").Wait();
        }
    }

    private void InitializeChartData()
    {
        try
        {
            _chartData.Clear();

            // Get historical data for the selected chart type
            var history = _metricsService.GetMetricsHistoryAsync(TimeSpan.FromMinutes(30)).Result;
            
            foreach (var metric in history.Entries)
            {
                double value = 0;
                switch (SelectedChartType)
                {
                    case DashboardChartType.Memory:
                        value = metric.MemoryMetrics.MemoryUsage;
                        break;
                    case DashboardChartType.CPU:
                        value = metric.CpuMetrics.CPUUsage;
                        break;
                    case DashboardChartType.Network:
                        value = metric.NetworkMetrics.NetworkLoadPercentage;
                        break;
                    case DashboardChartType.Temperature:
                        value = metric.HardwareMetrics.Temperature;
                        break;
                }

                _chartData.Add(new DashboardChartItem
                {
                    Timestamp = metric.Timestamp,
                    Value = value,
                    Label = metric.Timestamp.ToString("HH:mm")
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing chart data: {ex.Message}").Wait();
        }
    }

    private void InitializeAlertItems()
    {
        try
        {
            _alertItems.Clear();

            // Check for high memory usage
            if (CurrentMetrics.MemoryMetrics.MemoryUsage > 85)
            {
                _alertItems.Add(new DashboardAlertItem
                {
                    Title = "High Memory Usage",
                    Message = "Memory usage is above 85%. Consider optimizing memory usage.",
                    Type = DashboardAlertType.Warning,
                    Severity = DashboardAlertSeverity.High,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    Action = "Optimize Memory"
                });
            }

            // Check for high CPU usage
            if (CurrentMetrics.CpuMetrics.CPUUsage > 80)
            {
                _alertItems.Add(new DashboardAlertItem
                {
                    Title = "High CPU Usage",
                    Message = "CPU usage is above 80%. Consider closing unnecessary processes.",
                    Type = DashboardAlertType.Warning,
                    Severity = DashboardAlertSeverity.Medium,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    Action = "Optimize CPU"
                });
            }

            // Check for high temperature
            if (CurrentMetrics.HardwareMetrics.Temperature > 80)
            {
                _alertItems.Add(new DashboardAlertItem
                {
                    Title = "High Temperature",
                    Message = "System temperature is above 80°C. Consider cooling solutions.",
                    Type = DashboardAlertType.Error,
                    Severity = DashboardAlertSeverity.High,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    Action = "Cool System"
                });
            }

            // Check for network issues
            if (CurrentMetrics.NetworkMetrics.NetworkLoadPercentage > 90)
            {
                _alertItems.Add(new DashboardAlertItem
                {
                    Title = "High Network Load",
                    Message = "Network load is above 90%. Consider optimizing network usage.",
                    Type = DashboardAlertType.Info,
                    Severity = DashboardAlertSeverity.Low,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    Action = "Optimize Network"
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing alert items: {ex.Message}").Wait();
        }
    }

    private void InitializeQuickActions()
    {
        try
        {
            _quickActions.Clear();

            // Memory optimization
            _quickActions.Add(new DashboardQuickAction
            {
                Title = "Optimize Memory",
                Description = "Free up memory by terminating unnecessary processes",
                Icon = "💾",
                Color = "#4CAF50",
                Action = "Memory",
                IsEnabled = true
            });

            // CPU optimization
            _quickActions.Add(new DashboardQuickAction
            {
                Title = "Optimize CPU",
                Description = "Reduce CPU usage by adjusting process priorities",
                Icon = "🖥️",
                Color = "#2196F3",
                Action = "CPU",
                IsEnabled = true
            });

            // Network optimization
            _quickActions.Add(new DashboardQuickAction
            {
                Title = "Optimize Network",
                Description = "Improve network performance and bandwidth allocation",
                Icon = "🌐",
                Color = "#FF9800",
                Action = "Network",
                IsEnabled = true
            });

            // Compression
            _quickActions.Add(new DashboardQuickAction
            {
                Title = "Compress Files",
                Description = "Compress large files to save disk space",
                Icon = "🗜️",
                Color = "#9C27B0",
                Action = "Compression",
                IsEnabled = true
            });

            // System cleanup
            _quickActions.Add(new DashboardQuickAction
            {
                Title = "System Cleanup",
                Description = "Clean up temporary files and system junk",
                Icon = "🧹",
                Color = "#F44336",
                Action = "Cleanup",
                IsEnabled = true
            });

            // Hardware check
            _quickActions.Add(new DashboardQuickAction
            {
                Title = "Hardware Check",
                Description = "Check hardware status and performance",
                Icon = "🔧",
                Color = "#607D8B",
                Action = "Hardware",
                IsEnabled = true
            });
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error initializing quick actions: {ex.Message}").Wait();
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
                    await UpdateDashboardAsync();
                    await Task.Delay(5000, _refreshCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _loggerService.LogErrorAsync($"Error in dashboard real-time updates: {ex.Message}");
                    await Task.Delay(10000, _refreshCancellationTokenSource.Token); // Wait longer on error
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

    private async Task UpdateDashboardAsync()
    {
        try
        {
            // Update metrics
            CurrentMetrics = await _metricsService.GetCurrentMetricsAsync();
            SystemInfo = await _systemService.GetSystemInfoAsync();

            // Update UI on main thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Update metric cards
                UpdateMetricCards();

                // Update process items
                UpdateProcessItems();

                // Update chart data
                UpdateChartData();

                // Update alert items
                UpdateAlertItems();

                // Update quick actions
                UpdateQuickActions();

                // Update last update time
                LastUpdateTime = DateTime.Now;
            });
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error updating dashboard: {ex.Message}");
        }
    }

    private void UpdateMetricCards()
    {
        try
        {
            if (_metricCards.Count == 0)
                return;

            // Update memory card
            var memoryCard = _metricCards.FirstOrDefault(c => c.Type == DashboardMetricType.Memory);
            if (memoryCard != null)
            {
                memoryCard.Value = $"{CurrentMetrics.MemoryMetrics.MemoryUsage:F1}%";
                memoryCard.Details = $"Used: {CurrentMetrics.MemoryMetrics.UsedMemoryMB:F0} MB\nAvailable: {CurrentMetrics.MemoryMetrics.AvailableMemoryMB:F0} MB";
                memoryCard.Color = GetMetricColor(CurrentMetrics.MemoryMetrics.MemoryUsage, 80, 90);
                memoryCard.Trend = GetTrendValue(CurrentMetrics.MemoryMetrics.MemoryUsage);
            }

            // Update CPU card
            var cpuCard = _metricCards.FirstOrDefault(c => c.Type == DashboardMetricType.CPU);
            if (cpuCard != null)
            {
                cpuCard.Value = $"{CurrentMetrics.CpuMetrics.CPUUsage:F1}%";
                cpuCard.Details = $"Cores: {CurrentMetrics.CpuMetrics.CoreCount}\nTemperature: {CurrentMetrics.CpuMetrics.Temperature:F1}°C";
                cpuCard.Color = GetMetricColor(CurrentMetrics.CpuMetrics.CPUUsage, 70, 85);
                cpuCard.Trend = GetTrendValue(CurrentMetrics.CpuMetrics.CPUUsage);
            }

            // Update network card
            var networkCard = _metricCards.FirstOrDefault(c => c.Type == DashboardMetricType.Network);
            if (networkCard != null)
            {
                networkCard.Value = $"{CurrentMetrics.NetworkMetrics.NetworkLoadPercentage:F1}%";
                networkCard.Details = $"Download: {CurrentMetrics.NetworkMetrics.DownloadSpeedMbps:F1} Mbps\nUpload: {CurrentMetrics.NetworkMetrics.UploadSpeedMbps:F1} Mbps";
                networkCard.Color = GetMetricColor(CurrentMetrics.NetworkMetrics.NetworkLoadPercentage, 60, 80);
                networkCard.Trend = GetTrendValue(CurrentMetrics.NetworkMetrics.NetworkLoadPercentage);
            }

            // Update hardware card
            var hardwareCard = _metricCards.FirstOrDefault(c => c.Type == DashboardMetricType.Hardware);
            if (hardwareCard != null)
            {
                hardwareCard.Value = $"{CurrentMetrics.HardwareMetrics.Temperature:F1}°C";
                hardwareCard.Details = $"Status: {CurrentMetrics.HardwareMetrics.HardwareStatus}\nFan Speed: {CurrentMetrics.HardwareMetrics.FanSpeed:F1}%";
                hardwareCard.Color = GetMetricColor(CurrentMetrics.HardwareMetrics.Temperature, 70, 85);
                hardwareCard.Trend = GetTrendValue(CurrentMetrics.HardwareMetrics.Temperature);
            }

            // Update optimization card
            var optimizationCard = _metricCards.FirstOrDefault(c => c.Type == DashboardMetricType.Optimization);
            if (optimizationCard != null)
            {
                optimizationCard.Value = $"{CurrentMetrics.OptimizationMetrics.OptimizationProgress:F1}%";
                optimizationCard.Details = $"Active: {CurrentMetrics.OptimizationMetrics.ActiveOptimizations}\nCompleted: {CurrentMetrics.OptimizationMetrics.CompletedOptimizations}";
                optimizationCard.Color = GetMetricColor(CurrentMetrics.OptimizationMetrics.OptimizationProgress, 90, 95);
                optimizationCard.Trend = GetTrendValue(CurrentMetrics.OptimizationMetrics.OptimizationProgress);
            }

            // Update compression card
            var compressionCard = _metricCards.FirstOrDefault(c => c.Type == DashboardMetricType.Compression);
            if (compressionCard != null)
            {
                compressionCard.Value = $"{CurrentMetrics.CompressionMetrics.CompressionRatio:P1}";
                compressionCard.Details = $"Active: {CurrentMetrics.CompressionMetrics.ActiveCompressions}\nThroughput: {CurrentMetrics.CompressionMetrics.CompressionThroughput:F1} MB/s";
                compressionCard.Color = GetMetricColor(CurrentMetrics.CompressionMetrics.CompressionRatio * 100, 70, 90);
                compressionCard.Trend = GetTrendValue(CurrentMetrics.CompressionMetrics.CompressionRatio * 100);
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating metric cards: {ex.Message}").Wait();
        }
    }

    private void UpdateProcessItems()
    {
        try
        {
            // Update process items with current data
            var processes = _systemService.GetTopProcessesByMemory(10).Result;
            
            for (int i = 0; i < Math.Min(processes.Count, _processItems.Count); i++)
            {
                var process = processes[i];
                var processItem = _processItems[i];
                
                processItem.MemoryUsage = process.MemoryUsageMB;
                processItem.CpuUsage = process.CPUUsage;
                processItem.Priority = process.Priority;
                processItem.Status = process.Status;
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating process items: {ex.Message}").Wait();
        }
    }

    private void UpdateChartData()
    {
        try
        {
            // Update chart data with new metrics
            var history = _metricsService.GetMetricsHistoryAsync(TimeSpan.FromMinutes(30)).Result;
            
            _chartData.Clear();
            
            foreach (var metric in history.Entries)
            {
                double value = 0;
                switch (SelectedChartType)
                {
                    case DashboardChartType.Memory:
                        value = metric.MemoryMetrics.MemoryUsage;
                        break;
                    case DashboardChartType.CPU:
                        value = metric.CpuMetrics.CPUUsage;
                        break;
                    case DashboardChartType.Network:
                        value = metric.NetworkMetrics.NetworkLoadPercentage;
                        break;
                    case DashboardChartType.Temperature:
                        value = metric.HardwareMetrics.Temperature;
                        break;
                }

                _chartData.Add(new DashboardChartItem
                {
                    Timestamp = metric.Timestamp,
                    Value = value,
                    Label = metric.Timestamp.ToString("HH:mm")
                });
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating chart data: {ex.Message}").Wait();
        }
    }

    private void UpdateAlertItems()
    {
        try
        {
            // Check for new alerts
            var newAlerts = new List<DashboardAlertItem>();

            // Check for high memory usage
            if (CurrentMetrics.MemoryMetrics.MemoryUsage > 85)
            {
                newAlerts.Add(new DashboardAlertItem
                {
                    Title = "High Memory Usage",
                    Message = "Memory usage is above 85%. Consider optimizing memory usage.",
                    Type = DashboardAlertType.Warning,
                    Severity = DashboardAlertSeverity.High,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    Action = "Optimize Memory"
                });
            }

            // Check for high CPU usage
            if (CurrentMetrics.CpuMetrics.CPUUsage > 80)
            {
                newAlerts.Add(new DashboardAlertItem
                {
                    Title = "High CPU Usage",
                    Message = "CPU usage is above 80%. Consider closing unnecessary processes.",
                    Type = DashboardAlertType.Warning,
                    Severity = DashboardAlertSeverity.Medium,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    Action = "Optimize CPU"
                });
            }

            // Check for high temperature
            if (CurrentMetrics.HardwareMetrics.Temperature > 80)
            {
                newAlerts.Add(new DashboardAlertItem
                {
                    Title = "High Temperature",
                    Message = "System temperature is above 80°C. Consider cooling solutions.",
                    Type = DashboardAlertType.Error,
                    Severity = DashboardAlertSeverity.High,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    Action = "Cool System"
                });
            }

            // Check for network issues
            if (CurrentMetrics.NetworkMetrics.NetworkLoadPercentage > 90)
            {
                newAlerts.Add(new DashboardAlertItem
                {
                    Title = "High Network Load",
                    Message = "Network load is above 90%. Consider optimizing network usage.",
                    Type = DashboardAlertType.Info,
                    Severity = DashboardAlertSeverity.Low,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    Action = "Optimize Network"
                });
            }

            // Add new alerts
            foreach (var alert in newAlerts)
            {
                if (!_alertItems.Any(a => a.Title == alert.Title && a.Timestamp == alert.Timestamp))
                {
                    _alertItems.Insert(0, alert);
                }
            }

            // Remove old alerts (older than 1 hour)
            var cutoffTime = DateTime.Now - TimeSpan.FromHours(1);
            _alertItems.RemoveAll(a => a.Timestamp < cutoffTime);
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating alert items: {ex.Message}").Wait();
        }
    }

    private void UpdateQuickActions()
    {
        try
        {
            // Update quick action states based on current system status
            foreach (var action in _quickActions)
            {
                switch (action.Action)
                {
                    case "Memory":
                        action.IsEnabled = CurrentMetrics.MemoryMetrics.MemoryUsage > 70;
                        break;
                    case "CPU":
                        action.IsEnabled = CurrentMetrics.CpuMetrics.CPUUsage > 70;
                        break;
                    case "Network":
                        action.IsEnabled = CurrentMetrics.NetworkMetrics.NetworkLoadPercentage > 70;
                        break;
                    case "Compression":
                        action.IsEnabled = CurrentMetrics.CompressionMetrics.ActiveCompressions < 5;
                        break;
                    case "Cleanup":
                        action.IsEnabled = true;
                        break;
                    case "Hardware":
                        action.IsEnabled = CurrentMetrics.HardwareMetrics.Temperature > 60;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error updating quick actions: {ex.Message}").Wait();
        }
    }

    private void RefreshDashboard()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                IsRefreshing = true;
                
                await UpdateDashboardAsync();
                
                await _loggerService.LogAsync("Dashboard refreshed successfully");
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync($"Error refreshing dashboard: {ex.Message}");
                await _dialogService.ShowErrorAsync("Refresh Error", $"Failed to refresh dashboard: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
            }
        });
    }

    private void ChangeChartType(string chartType)
    {
        try
        {
            if (Enum.TryParse(chartType, out DashboardChartType type))
            {
                SelectedChartType = type;
                InitializeChartData();
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error changing chart type: {ex.Message}").Wait();
        }
    }

    private void ChangeViewMode()
    {
        try
        {
            ViewMode = ViewMode == DashboardViewMode.Comprehensive ? 
                DashboardViewMode.Minimal : DashboardViewMode.Comprehensive;
        }
        catch (Exception ex)
        {
            _loggerService.LogErrorAsync($"Error changing view mode: {ex.Message}").Wait();
        }
    }

    private void ToggleProcesses()
    {
        ShowProcesses = !ShowProcesses;
    }

    private void ToggleAlerts()
    {
        ShowAlerts = !ShowAlerts;
    }

    private void ToggleQuickActions()
    {
        ShowQuickActions = !ShowQuickActions;
    }

    private async void ExecuteQuickAction(string action)
    {
        try
        {
            switch (action)
            {
                case "Memory":
                    await NavigateToPageAsync("Memory");
                    break;
                case "CPU":
                    await NavigateToPageAsync("CPU");
                    break;
                case "Network":
                    await NavigateToPageAsync("Network");
                    break;
                case "Compression":
                    await NavigateToPageAsync("Compression");
                    break;
                case "Cleanup":
                    await NavigateToPageAsync("Storage");
                    break;
                case "Hardware":
                    await NavigateToPageAsync("Hardware");
                    break;
                default:
                    await _dialogService.ShowMessageAsync("Quick Action", $"Action '{action}' will be implemented in the next phase.");
                    break;
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error executing quick action: {ex.Message}");
            await _dialogService.ShowErrorAsync("Quick Action Error", $"Failed to execute quick action: {ex.Message}");
        }
    }

    private async void ViewProcessDetails(ViewProcessDetailsEventArgs args)
    {
        try
        {
            var process = _processItems.FirstOrDefault(p => p.Id == args.ProcessId);
            if (process != null)
            {
                await _dialogService.ShowDialogAsync<ProcessDetailsViewModel>(
                    $"Process Details - {process.Name}", 
                    new { ProcessId = process.Id });
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error viewing process details: {ex.Message}");
            await _dialogService.ShowErrorAsync("Process Details Error", $"Failed to view process details: {ex.Message}");
        }
    }

    private async void ViewAlertDetails(ViewAlertDetailsEventArgs args)
    {
        try
        {
            var alert = _alertItems.FirstOrDefault(a => a.Title == args.AlertTitle);
            if (alert != null)
            {
                alert.IsRead = true;
                await _dialogService.ShowMessageAsync(alert.Title, alert.Message);
            }
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error viewing alert details: {ex.Message}");
            await _dialogService.ShowErrorAsync("Alert Details Error", $"Failed to view alert details: {ex.Message}");
        }
    }

    private async void OptimizeNow()
    {
        try
        {
            await _dialogService.ShowMessageAsync("Optimization", "Starting system optimization...");
            
            // Start optimization
            await _optimizationService.StartOptimizationAsync("Memory");
            
            await _dialogService.ShowMessageAsync("Optimization", "System optimization completed successfully!");
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error optimizing system: {ex.Message}");
            await _dialogService.ShowErrorAsync("Optimization Error", $"Failed to optimize system: {ex.Message}");
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

    private string GetMetricColor(double value, double warningThreshold, double criticalThreshold)
    {
        if (value >= criticalThreshold)
            return "#F44336"; // Red
        if (value >= warningThreshold)
            return "#FF9800"; // Orange
        return "#4CAF50"; // Green
    }

    private DashboardTrend GetTrendValue(double currentValue)
    {
        // This is a simplified trend calculation
        // In a real implementation, you would compare with previous values
        if (currentValue > 75)
            return DashboardTrend.Up;
        if (currentValue < 25)
            return DashboardTrend.Down;
        return DashboardTrend.Stable;
    }

    // Event Handlers
    private void OnMetricsUpdated(object? sender, MetricsUpdatedEventArgs e)
    {
        _ = UpdateDashboardAsync();
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
            SystemInfo = e.SystemInfo;
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
        _ = UpdateDashboardAsync();
    }

    private void OnHardwareError(object? sender, HardwareErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Hardware Error", e.ErrorMessage);
        });
    }

    private void OnNetworkStatusChanged(object? sender, NetworkStatusChangedEventArgs e)
    {
        _ = UpdateDashboardAsync();
    }

    private void OnNetworkError(object? sender, NetworkErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Network Error", e.ErrorMessage);
        });
    }

    private void OnCompressionStatusChanged(object? sender, CompressionStatusChangedEventArgs e)
    {
        _ = UpdateDashboardAsync();
    }

    private void OnCompressionError(object? sender, CompressionErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Compression Error", e.ErrorMessage);
        });
    }

    private void OnOptimizationStatusChanged(object? sender, OptimizationStatusChangedEventArgs e)
    {
        _ = UpdateDashboardAsync();
    }

    private void OnOptimizationError(object? sender, OptimizationErrorEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.ShowErrorAsync("Optimization Error", e.ErrorMessage);
        });
    }

    public async Task OnThemeChangedAsync(ThemeVariant newTheme)
    {
        try
        {
            // Update dashboard theme
            await _loggerService.LogAsync($"Dashboard theme changed to {newTheme}");
            
            // Update UI colors and styles
            await UpdateDashboardAsync();
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync($"Error handling theme change: {ex.Message}");
        }
    }

    // Performance monitoring event handlers
    private void OnPerformanceMetricsUpdated(object? sender, PerformanceMetricsUpdatedEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Update current metrics with performance data
            CurrentMetrics = e.Metrics;
            await UpdateDashboardAsync();
        });
    }

    private void OnPerformanceAlert(object? sender, PerformanceAlertEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Add performance alert to alert items
            _alertItems.Insert(0, new DashboardAlertItem
            {
                Title = e.Alert.Type.ToString(),
                Message = e.Alert.Message,
                Type = DashboardAlertType.Warning,
                Severity = DashboardAlertSeverity.Medium,
                Timestamp = DateTime.Now,
                IsRead = false,
                Action = "View Details"
            });
            
            await _loggerService.LogWarningAsync($"Performance alert: {e.Alert.Message}");
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
}

// Supporting classes
public class DashboardMetricCard
{
    public string Title { get; set; } = "";
    public string Value { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
    public DashboardMetricType Type { get; set; }
    public string Details { get; set; } = "";
    public DashboardTrend Trend { get; set; }
}

public class DashboardProcessItem
{
    public string Name { get; set; } = "";
    public int Id { get; set; }
    public double MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public string Priority { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsSystemProcess { get; set; }
    public string Path { get; set; } = "";
}

public class DashboardChartItem
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string Label { get; set; } = "";
}

public class DashboardAlertItem
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DashboardAlertType Type { get; set; }
    public DashboardAlertSeverity Severity { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
    public string Action { get; set; } = "";
}

public class DashboardQuickAction
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
    public string Action { get; set; } = "";
    public bool IsEnabled { get; set; }
}

public enum DashboardChartType
{
    Memory,
    CPU,
    Network,
    Temperature
}

public enum DashboardViewMode
{
    Comprehensive,
    Minimal
}

public enum DashboardMetricType
{
    Memory,
    CPU,
    Network,
    Hardware,
    Optimization,
    Compression
}

public enum DashboardTrend
{
    Up,
    Down,
    Stable
}

public enum DashboardAlertType
{
    Info,
    Warning,
    Error,
    Success
}

public enum DashboardAlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

// Event argument classes
public class ViewProcessDetailsEventArgs : EventArgs
{
    public int ProcessId { get; }

    public ViewProcessDetailsEventArgs(int processId)
    {
        ProcessId = processId;
    }
}

public class ViewAlertDetailsEventArgs : EventArgs
{
    public string AlertTitle { get; }

    public ViewAlertDetailsEventArgs(string alertTitle)
    {
        AlertTitle = alertTitle;
    }
}