using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RamOptimizerNova.ViewModels;
using RamOptimizerNova.Views;
using RamOptimizerNova.Services;
using RamOptimizerNova.Services.Interfaces;
using RamOptimizer.HardwareControl;
using RamOptimizer.Core.Interfaces;
using RamOptimizer.Logging;

namespace RamOptimizerNova;

/// <summary>
/// Main application class for RAM Optimizer Nova
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<App> _logger;
    private readonly IRamOptimizerBackendService _backendService;
    private readonly IRealTimeDataSynchronizationService _synchronizationService;
    private readonly IErrorHandlingAndLoggingService _errorHandlingService;
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly INetworkService _networkService;
    private readonly IMetricsService _metricsService;
    private readonly ISystemService _systemService;
    private readonly IHardwareService _hardwareService;
    private readonly IOptimizationService _optimizationService;
    private readonly IDialogService _dialogService;
    private readonly ILoggerService _loggerService;

    private bool _isInitialized = false;
    private bool _isShuttingDown = false;

    public App()
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        // Register services
        services.AddSingleton<IRamOptimizerBackendService, RamOptimizerBackendService>();
        services.AddSingleton<IRealTimeDataSynchronizationService, RealTimeDataSynchronizationService>();
        services.AddSingleton<IErrorHandlingAndLoggingService, ErrorHandlingAndLoggingService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
        services.AddSingleton<INetworkService, NetworkService>();
        services.AddSingleton<IMetricsService, MetricsService>();
        services.AddSingleton<ISystemService, SystemService>();
        services.AddSingleton<IHardwareService, HardwareService>();
        services.AddSingleton<IOptimizationService, OptimizationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ILoggerService, LoggerService>();

        // Register view models
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CpuOptimizationViewModel>();
        services.AddTransient<MemoryOptimizationViewModel>();
        services.AddTransient<CompressionViewModel>();
        services.AddTransient<StorageOptimizationViewModel>();
        services.AddTransient<NetworkOptimizationViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();

        // Register views
        services.AddTransient<MainWindow>();
        services.AddTransient<DashboardView>();
        services.AddTransient<CpuOptimizationView>();
        services.AddTransient<MemoryOptimizationView>();
        services.AddTransient<CompressionView>();
        services.AddTransient<StorageOptimizationView>();
        services.AddTransient<NetworkOptimizationView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<AboutView>();

        // Register hardware controllers
        services.AddSingleton<AsusHardwareController>();
        services.AddSingleton<SafeHardwareController>();
        services.AddSingleton<SnapshotManager>();

        // Build service provider
        _serviceProvider = services.BuildServiceProvider();

        // Get logger
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();

        // Get services
        _backendService = _serviceProvider.GetRequiredService<IRamOptimizerBackendService>();
        _synchronizationService = _serviceProvider.GetRequiredService<IRealTimeDataSynchronizationService>();
        _errorHandlingService = _serviceProvider.GetRequiredService<IErrorHandlingAndLoggingService>();
        _navigationService = _serviceProvider.GetRequiredService<INavigationService>();
        _themeService = _serviceProvider.GetRequiredService<IThemeService>();
        _performanceMonitoringService = _serviceProvider.GetRequiredService<IPerformanceMonitoringService>();
        _networkService = _serviceProvider.GetRequiredService<INetworkService>();
        _metricsService = _serviceProvider.GetRequiredService<IMetricsService>();
        _systemService = _serviceProvider.GetRequiredService<ISystemService>();
        _hardwareService = _serviceProvider.GetRequiredService<IHardwareService>();
        _optimizationService = _serviceProvider.GetRequiredService<IOptimizationService>();
        _dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        _loggerService = _serviceProvider.GetRequiredService<ILoggerService>();

        // Subscribe to error handling events
        _errorHandlingService.ErrorOccurred += OnErrorOccurred;
        _errorHandlingService.WarningOccurred += OnWarningOccurred;
        _errorHandlingService.InfoLogged += OnInfoLogged;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            _logger.LogInformation("Starting RAM Optimizer Nova application...");

            // Initialize services
            await InitializeServicesAsync();

            // Set up theme
            await SetupThemeAsync();

            // Set up navigation
            await SetupNavigationAsync();

            // Show main window
            await ShowMainWindowAsync();

            _isInitialized = true;
            _logger.LogInformation("RAM Optimizer Nova application started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting RAM Optimizer Nova application");
            await HandleStartupErrorAsync(ex);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_isShuttingDown)
                return;

            _isShuttingDown = true;
            _logger.LogInformation("Shutting down RAM Optimizer Nova application...");

            // Cleanup services
            await CleanupServicesAsync();

            _logger.LogInformation("RAM Optimizer Nova application shutdown complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during application shutdown");
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private async Task InitializeServicesAsync()
    {
        try
        {
            _logger.LogInformation("Initializing services...");

            // Initialize error handling and logging service
            var errorHandlingInitialized = await _errorHandlingService.InitializeAsync();
            if (!errorHandlingInitialized)
            {
                throw new InvalidOperationException("Failed to initialize error handling service");
            }

            // Initialize backend service
            var backendInitialized = await _backendService.InitializeAsync();
            if (!backendInitialized)
            {
                throw new InvalidOperationException("Failed to initialize backend service");
            }

            // Initialize synchronization service
            var synchronizationInitialized = await _synchronizationService.InitializeAsync();
            if (!synchronizationInitialized)
            {
                throw new InvalidOperationException("Failed to initialize synchronization service");
            }

            // Initialize performance monitoring service
            await _performanceMonitoringService.InitializeAsync();

            // Initialize network service
            await _networkService.InitializeAsync();

            // Initialize metrics service
            await _metricsService.InitializeAsync();

            // Initialize system service
            await _systemService.InitializeAsync();

            // Initialize hardware service
            await _hardwareService.InitializeAsync();

            // Initialize optimization service
            await _optimizationService.InitializeAsync();

            // Initialize dialog service
            await _dialogService.InitializeAsync();

            // Initialize logger service
            await _loggerService.InitializeAsync();

            _logger.LogInformation("All services initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing services");
            throw;
        }
    }

    private async Task SetupThemeAsync()
    {
        try
        {
            _logger.LogInformation("Setting up theme...");

            // Load saved theme preference
            var theme = await _themeService.GetThemeAsync();
            await _themeService.ApplyThemeAsync(theme);

            _logger.LogInformation("Theme setup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up theme");
            // Use default theme if setup fails
            await _themeService.ApplyThemeAsync(ThemeType.Dark);
        }
    }

    private async Task SetupNavigationAsync()
    {
        try
        {
            _logger.LogInformation("Setting up navigation...");

            // Configure navigation service
            _navigationService.ConfigureViewMapping<DashboardViewModel, DashboardView>();
            _navigationService.ConfigureViewMapping<CpuOptimizationViewModel, CpuOptimizationView>();
            _navigationService.ConfigureViewMapping<MemoryOptimizationViewModel, MemoryOptimizationView>();
            _navigationService.ConfigureViewMapping<CompressionViewModel, CompressionView>();
            _navigationService.ConfigureViewMapping<StorageOptimizationViewModel, StorageOptimizationView>();
            _navigationService.ConfigureViewMapping<NetworkOptimizationViewModel, NetworkOptimizationView>();
            _navigationService.ConfigureViewMapping<SettingsViewModel, SettingsView>();
            _navigationService.ConfigureViewMapping<AboutViewModel, AboutView>();

            _logger.LogInformation("Navigation setup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up navigation");
            throw;
        }
    }

    private async Task ShowMainWindowAsync()
    {
        try
        {
            _logger.LogInformation("Showing main window...");

            // Get main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            
            // Set main window as active window
            if (Application.MainWindow != null)
            {
                Application.MainWindow = mainWindow;
            }

            // Show main window
            mainWindow.Show();

            // Navigate to dashboard
            await _navigationService.NavigateToAsync<DashboardViewModel>();

            _logger.LogInformation("Main window shown successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing main window");
            throw;
        }
    }

    private async Task CleanupServicesAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up services...");

            // Cleanup services in reverse order of initialization
            try { await _loggerService.CleanupAsync(); } catch { }
            try { await _dialogService.CleanupAsync(); } catch { }
            try { await _optimizationService.CleanupAsync(); } catch { }
            try { await _hardwareService.CleanupAsync(); } catch { }
            try { await _systemService.CleanupAsync(); } catch { }
            try { await _metricsService.CleanupAsync(); } catch { }
            try { await _networkService.CleanupAsync(); } catch { }
            try { await _performanceMonitoringService.CleanupAsync(); } catch { }
            try { await _synchronizationService.CleanupAsync(); } catch { }
            try { await _backendService.CleanupAsync(); } catch { }
            try { await _errorHandlingService.CleanupAsync(); } catch { }

            _logger.LogInformation("All services cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up services");
        }
    }

    private async Task HandleStartupErrorAsync(Exception ex)
    {
        try
        {
            _logger.LogError(ex, "Handling startup error...");

            // Show error dialog
            await _dialogService.ShowErrorAsync(
                "Startup Error",
                "Failed to start RAM Optimizer Nova. Please check the logs for more details.",
                ex.Message);

            // Exit application
            Environment.Exit(1);
        }
        catch (Exception dialogEx)
        {
            _logger.LogError(dialogEx, "Error showing startup error dialog");
            Environment.Exit(1);
        }
    }

    private async void OnErrorOccurred(object? sender, ErrorHandlingAndLoggingService.ErrorOccurredEventArgs e)
    {
        try
        {
            _logger.LogError("Error occurred: {Message}", e.Entry.Message);

            // Show error notification in UI if available
            if (_isInitialized && !_isShuttingDown)
            {
                await _dialogService.ShowErrorAsync(
                    "Error",
                    e.Entry.Message,
                    e.Entry.Exception?.Message ?? "No additional information available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling error occurred event");
        }
    }

    private async void OnWarningOccurred(object? sender, ErrorHandlingAndLoggingService.WarningOccurredEventArgs e)
    {
        try
        {
            _logger.LogWarning("Warning occurred: {Message}", e.Entry.Message);

            // Show warning notification in UI if available
            if (_isInitialized && !_isShuttingDown)
            {
                await _dialogService.ShowWarningAsync(
                    "Warning",
                    e.Entry.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling warning occurred event");
        }
    }

    private async void OnInfoLogged(object? sender, ErrorHandlingAndLoggingService.InfoLoggedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Info logged: {Message}", e.Entry.Message);

            // Show info notification in UI if available and important
            if (_isInitialized && !_isShuttingDown && 
                (e.Entry.Category.Equals("System", StringComparison.OrdinalIgnoreCase) ||
                 e.Entry.Category.Equals("Hardware", StringComparison.OrdinalIgnoreCase)))
            {
                await _dialogService.ShowInfoAsync(
                    "Information",
                    e.Entry.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling info logged event");
        }
    }
}