using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using RamOptimizerNova.ViewModels;
using RamOptimizerNova.Services.Interfaces;
using RamOptimizerNova.Models;

namespace RamOptimizerNova.Tests.ViewModels;

/// <summary>
/// Unit tests for DashboardViewModel
/// </summary>
public class DashboardViewModelTests
{
    private readonly Mock<IRamOptimizerBackendService> _mockBackendService;
    private readonly Mock<IRealTimeDataSynchronizationService> _mockSynchronizationService;
    private readonly Mock<IPerformanceMonitoringService> _mockPerformanceMonitoringService;
    private readonly Mock<INetworkService> _mockNetworkService;
    private readonly Mock<IMetricsService> _mockMetricsService;
    private readonly Mock<ISystemService> _mockSystemService;
    private readonly Mock<IHardwareService> _mockHardwareService;
    private readonly Mock<IOptimizationService> _mockOptimizationService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<IErrorHandlingAndLoggingService> _mockErrorHandlingService;

    private DashboardViewModel _viewModel;

    public DashboardViewModelTests()
    {
        // Mock all dependencies
        _mockBackendService = new Mock<IRamOptimizerBackendService>();
        _mockSynchronizationService = new Mock<IRealTimeDataSynchronizationService>();
        _mockPerformanceMonitoringService = new Mock<IPerformanceMonitoringService>();
        _mockNetworkService = new Mock<INetworkService>();
        _mockMetricsService = new Mock<IMetricsService>();
        _mockSystemService = new Mock<ISystemService>();
        _mockHardwareService = new Mock<IHardwareService>();
        _mockOptimizationService = new Mock<IOptimizationService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockLoggerService = new Mock<ILoggerService>();
        _mockErrorHandlingService = new Mock<IErrorHandlingAndLoggingService>();

        // Setup default behavior
        SetupDefaultBehavior();
    }

    private void SetupDefaultBehavior()
    {
        // Backend service
        _mockBackendService.Setup(x => x.InitializeAsync())
            .ReturnsAsync(true);
        _mockBackendService.Setup(x => x.IsHardwareAvailable())
            .Returns(true);
        _mockBackendService.Setup(x => x.IsHardwareSafe())
            .Returns(true);
        _mockBackendService.Setup(x => x.IsDryRunMode())
            .Returns(true);

        // Synchronization service
        _mockSynchronizationService.Setup(x => x.InitializeAsync())
            .ReturnsAsync(true);
        _mockSynchronizationService.Setup(x => x.GetSystemMetricsAsync())
            .ReturnsAsync(new SystemMetrics
            {
                TotalMemory = 16L * 1024 * 1024 * 1024, // 16GB
                AvailableMemory = 8L * 1024 * 1024 * 1024, // 8GB
                UsedMemory = 8L * 1024 * 1024 * 1024, // 8GB
                MemoryUsagePercentage = 50.0,
                CpuUsage = 25.0,
                GpuUsage = 15.0,
                DiskUsage = 45.0,
                NetworkUploadSpeed = 10.5,
                NetworkDownloadSpeed = 50.2,
                ProcessCount = 150,
                ThreadCount = 2000,
                Uptime = TimeSpan.FromDays(1)
            });
        _mockSynchronizationService.Setup(x => x.GetHardwareStateAsync())
            .ReturnsAsync(new HardwareState
            {
                IsHardwareAvailable = true,
                IsHardwareSafe = true,
                IsDryRunMode = true,
                CurrentPCores = 4,
                CurrentECores = 4,
                BatteryLevel = 75,
                ThermalThrottling = false,
                HardwareWarnings = new List<string>()
            });
        _mockSynchronizationService.Setup(x => x.GetOptimizationResultsAsync())
            .ReturnsAsync(new OptimizationResults
            {
                TotalOptimizations = 100,
                SuccessfulOptimizations = 95,
                FailedOptimizations = 5,
                AverageMemoryFreed = 512L * 1024 * 1024, // 512MB
                TotalMemoryFreed = 50L * 1024 * 1024 * 1024, // 50GB
                LastOptimizationTime = DateTime.Now.AddMinutes(-5),
                OptimizationHistory = new List<OptimizationHistoryItem>
                {
                    new OptimizationHistoryItem
                    {
                        Timestamp = DateTime.Now.AddMinutes(-5),
                        Type = "Memory",
                        Success = true,
                        MemoryFreed = 512L * 1024 * 1024,
                        Description = "Optimized memory usage"
                    }
                }
            });

        // Performance monitoring service
        _mockPerformanceMonitoringService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);
        _mockPerformanceMonitoringService.Setup(x => x.StartMonitoringAsync())
            .Returns(Task.CompletedTask);
        _mockPerformanceMonitoringService.Setup(x => x.StopMonitoringAsync())
            .Returns(Task.CompletedTask);

        // Network service
        _mockNetworkService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);

        // Metrics service
        _mockMetricsService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);

        // System service
        _mockSystemService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);

        // Hardware service
        _mockHardwareService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);

        // Optimization service
        _mockOptimizationService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);

        // Dialog service
        _mockDialogService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);
        _mockDialogService.Setup(x => x.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockDialogService.Setup(x => x.ShowWarningAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockDialogService.Setup(x => x.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Logger service
        _mockLoggerService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);

        // Error handling service
        _mockErrorHandlingService.Setup(x => x.InitializeAsync())
            .ReturnsAsync(true);
        _mockErrorHandlingService.Setup(x => x.LogErrorAsync(It.IsAny<string>(), It.IsAny<Exception>()))
            .Returns(Task.CompletedTask);
        _mockErrorHandlingService.Setup(x => x.LogWarningAsync(It.IsAny<string>(), It.IsAny<Exception>()))
            .Returns(Task.CompletedTask);
        _mockErrorHandlingService.Setup(x => x.LogInfoAsync(It.IsAny<string>(), It.IsAny<Exception>()))
            .Returns(Task.CompletedTask);
    }

    private void CreateViewModel()
    {
        _viewModel = new DashboardViewModel(
            _mockBackendService.Object,
            _mockSynchronizationService.Object,
            _mockPerformanceMonitoringService.Object,
            _mockNetworkService.Object,
            _mockMetricsService.Object,
            _mockSystemService.Object,
            _mockHardwareService.Object,
            _mockOptimizationService.Object,
            _mockDialogService.Object,
            _mockLoggerService.Object,
            _mockErrorHandlingService.Object);
    }

    [Fact]
    public void Constructor_WhenCalled_ShouldInitializeProperties()
    {
        // Arrange & Act
        CreateViewModel();

        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Title.Should().Be("Dashboard");
        _viewModel.IsInitialized.Should().BeFalse();
        _viewModel.IsLoading.Should().BeTrue();
        _viewModel.SystemMetrics.Should().NotBeNull();
        _viewModel.HardwareState.Should().NotBeNull();
        _viewModel.OptimizationResults.Should().NotBeNull();
        _viewModel.MetricsHistory.Should().NotBeNull();
        _viewModel.MetricsHistory.Should().BeEmpty();
        _viewModel.RealTimeMetrics.Should().NotBeNull();
        _viewModel.RealTimeMetrics.Should().BeEmpty();
    }

    [Fact]
    public async Task InitializeAsync_WhenSuccessful_ShouldSetIsInitializedToTrue()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.IsInitialized.Should().BeTrue();
        _viewModel.IsLoading.Should().BeFalse();
        _mockSynchronizationService.Verify(x => x.GetSystemMetricsAsync(), Times.Once);
        _mockSynchronizationService.Verify(x => x.GetHardwareStateAsync(), Times.Once);
        _mockSynchronizationService.Verify(x => x.GetOptimizationResultsAsync(), Times.Once);
        _mockPerformanceMonitoringService.Verify(x => x.StartMonitoringAsync(), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenSynchronizationFails_ShouldLogError()
    {
        // Arrange
        _mockSynchronizationService.Setup(x => x.GetSystemMetricsAsync())
            .ThrowsAsync(new Exception("Synchronization failed"));

        CreateViewModel();

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.IsInitialized.Should().BeFalse();
        _viewModel.IsLoading.Should().BeFalse();
        _mockErrorHandlingService.Verify(x => x.LogErrorAsync(
            It.IsAny<string>(), 
            It.IsAny<Exception>()), 
            Times.Once);
    }

    [Fact]
    public async Task RefreshMetricsAsync_WhenCalled_ShouldRefreshMetrics()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.RefreshMetricsAsync();

        // Assert
        _mockSynchronizationService.Verify(x => x.GetSystemMetricsAsync(), Times.Exactly(2));
        _mockSynchronizationService.Verify(x => x.GetHardwareStateAsync(), Times.Exactly(2));
        _mockSynchronizationService.Verify(x => x.GetOptimizationResultsAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task RefreshMetricsAsync_WhenCalledAndSynchronizationFails_ShouldLogError()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        _mockSynchronizationService.Setup(x => x.GetSystemMetricsAsync())
            .ThrowsAsync(new Exception("Refresh failed"));

        // Act
        await _viewModel.RefreshMetricsAsync();

        // Assert
        _mockErrorHandlingService.Verify(x => x.LogErrorAsync(
            It.IsAny<string>(), 
            It.IsAny<Exception>()), 
            Times.Once);
    }

    [Fact]
    public void GetMemoryUsageColor_WhenLowUsage_ShouldReturnGreen()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetMemoryUsageColor(25.0);

        // Assert
        color.Should().Be("Green");
    }

    [Fact]
    public void GetMemoryUsageColor_WhenMediumUsage_ShouldReturnYellow()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetMemoryUsageColor(60.0);

        // Assert
        color.Should().Be("Yellow");
    }

    [Fact]
    public void GetMemoryUsageColor_WhenHighUsage_ShouldReturnRed()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetMemoryUsageColor(85.0);

        // Assert
        color.Should().Be("Red");
    }

    [Fact]
    public void GetCpuUsageColor_WhenLowUsage_ShouldReturnGreen()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetCpuUsageColor(20.0);

        // Assert
        color.Should().Be("Green");
    }

    [Fact]
    public void GetCpuUsageColor_WhenMediumUsage_ShouldReturnYellow()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetCpuUsageColor(60.0);

        // Assert
        color.Should().Be("Yellow");
    }

    [Fact]
    public void GetCpuUsageColor_WhenHighUsage_ShouldReturnRed()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetCpuUsageColor(90.0);

        // Assert
        color.Should().Be("Red");
    }

    [Fact]
    public void GetDiskUsageColor_WhenLowUsage_ShouldReturnGreen()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetDiskUsageColor(30.0);

        // Assert
        color.Should().Be("Green");
    }

    [Fact]
    public void GetDiskUsageColor_WhenMediumUsage_ShouldReturnYellow()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetDiskUsageColor(70.0);

        // Assert
        color.Should().Be("Yellow");
    }

    [Fact]
    public void GetDiskUsageColor_WhenHighUsage_ShouldReturnRed()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetDiskUsageColor(90.0);

        // Assert
        color.Should().Be("Red");
    }

    [Fact]
    public void GetNetworkSpeedColor_WhenLowSpeed_ShouldReturnGreen()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetNetworkSpeedColor(10.0);

        // Assert
        color.Should().Be("Green");
    }

    [Fact]
    public void GetNetworkSpeedColor_WhenMediumSpeed_ShouldReturnYellow()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetNetworkSpeedColor(50.0);

        // Assert
        color.Should().Be("Yellow");
    }

    [Fact]
    public void GetNetworkSpeedColor_WhenHighSpeed_ShouldReturnRed()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetNetworkSpeedColor(100.0);

        // Assert
        color.Should().Be("Red");
    }

    [Fact]
    public void GetHardwareStatusIcon_WhenAvailableAndSafe_ShouldReturnCheckmark()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var icon = _viewModel.GetHardwareStatusIcon(true, true);

        // Assert
        icon.Should().Be("✅");
    }

    [Fact]
    public void GetHardwareStatusIcon_WhenAvailableButNotSafe_ShouldReturnWarning()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var icon = _viewModel.GetHardwareStatusIcon(true, false);

        // Assert
        icon.Should().Be("⚠️");
    }

    [Fact]
    public void GetHardwareStatusIcon_WhenNotAvailable_ShouldReturnX()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var icon = _viewModel.GetHardwareStatusIcon(false, true);

        // Assert
        icon.Should().Be("❌");
    }

    [Fact]
    public void GetOptimizationSuccessRate_WhenHighRate_ShouldReturnGreen()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetOptimizationSuccessRate(95.0);

        // Assert
        color.Should().Be("Green");
    }

    [Fact]
    public void GetOptimizationSuccessRate_WhenMediumRate_ShouldReturnYellow()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetOptimizationSuccessRate(70.0);

        // Assert
        color.Should().Be("Yellow");
    }

    [Fact]
    public void GetOptimizationSuccessRate_WhenLowRate_ShouldReturnRed()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var color = _viewModel.GetOptimizationSuccessRate(30.0);

        // Assert
        color.Should().Be("Red");
    }

    [Fact]
    public async Task StartOptimizationAsync_WhenCalled_ShouldStartOptimization()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.StartOptimizationAsync();

        // Assert
        _mockOptimizationService.Verify(x => x.StartOptimizationAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task StartOptimizationAsync_WhenNotInitialized_ShouldNotStart()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.StartOptimizationAsync();

        // Assert
        _mockOptimizationService.Verify(x => x.StartOptimizationAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StartOptimizationAsync_WhenOptimizationFails_ShouldLogError()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        _mockOptimizationService.Setup(x => x.StartOptimizationAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Optimization failed"));

        // Act
        await _viewModel.StartOptimizationAsync();

        // Assert
        _mockErrorHandlingService.Verify(x => x.LogErrorAsync(
            It.IsAny<string>(), 
            It.IsAny<Exception>()), 
            Times.Once);
    }

    [Fact]
    public async Task ShowOptimizationHistoryAsync_WhenCalled_ShouldShowHistory()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.ShowOptimizationHistoryAsync();

        // Assert
        _mockDialogService.Verify(x => x.ShowInfoAsync(
            "Optimization History", 
            It.IsAny<string>()), 
            Times.Once);
    }

    [Fact]
    public async Task ShowOptimizationHistoryAsync_WhenNotInitialized_ShouldNotShow()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.ShowOptimizationHistoryAsync();

        // Assert
        _mockDialogService.Verify(x => x.ShowInfoAsync(
            "Optimization History", 
            It.IsAny<string>()), 
            Times.Never);
    }

    [Fact]
    public async Task ShowSystemInfoAsync_WhenCalled_ShouldShowSystemInfo()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.ShowSystemInfoAsync();

        // Assert
        _mockDialogService.Verify(x => x.ShowInfoAsync(
            "System Information", 
            It.IsAny<string>()), 
            Times.Once);
    }

    [Fact]
    public async Task ShowSystemInfoAsync_WhenNotInitialized_ShouldNotShow()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.ShowSystemInfoAsync();

        // Assert
        _mockDialogService.Verify(x => x.ShowInfoAsync(
            "System Information", 
            It.IsAny<string>()), 
            Times.Never);
    }

    [Fact]
    public async Task CleanupAsync_WhenCalled_ShouldCleanupServices()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.CleanupAsync();

        // Assert
        _viewModel.IsInitialized.Should().BeFalse();
        _mockPerformanceMonitoringService.Verify(x => x.StopMonitoringAsync(), Times.Once);
        _mockSynchronizationService.Verify(x => x.CleanupAsync(), Times.Once);
    }

    [Fact]
    public async Task CleanupAsync_WhenNotInitialized_ShouldNotThrow()
    {
        // Arrange
        CreateViewModel();

        // Act
        var action = async () => await _viewModel.CleanupAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void OnPropertyChanged_WhenPropertyChanges_ShouldRaisePropertyChanged()
    {
        // Arrange
        CreateViewModel();
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(DashboardViewModel.IsInitialized))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.IsInitialized = true;

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void FormatFileSize_WhenCalledWithValidSize_ShouldReturnFormattedString()
    {
        // Arrange
        CreateViewModel();

        // Act
        var formatted = _viewModel.FormatFileSize(1024L * 1024 * 1024); // 1GB

        // Assert
        formatted.Should().Be("1.00 GB");
    }

    [Fact]
    public void FormatFileSize_WhenCalledWithZero_ShouldReturnZeroString()
    {
        // Arrange
        CreateViewModel();

        // Act
        var formatted = _viewModel.FormatFileSize(0);

        // Assert
        formatted.Should().Be("0 B");
    }

    [Fact]
    public void FormatFileSize_WhenCalledWithLargeSize_ShouldReturnFormattedString()
    {
        // Arrange
        CreateViewModel();

        // Act
        var formatted = _viewModel.FormatFileSize(1024L * 1024 * 1024 * 1024); // 1TB

        // Assert
        formatted.Should().Be("1.00 TB");
    }

    [Fact]
    public void FormatTimeSpan_WhenCalledWithValidTimeSpan_ShouldReturnFormattedString()
    {
        // Arrange
        CreateViewModel();

        // Act
        var formatted = _viewModel.FormatTimeSpan(TimeSpan.FromHours(1));

        // Assert
        formatted.Should().Be("1h 0m");
    }

    [Fact]
    public void FormatTimeSpan_WhenCalledWithZeroTimeSpan_ShouldReturnZeroString()
    {
        // Arrange
        CreateViewModel();

        // Act
        var formatted = _viewModel.FormatTimeSpan(TimeSpan.Zero);

        // Assert
        formatted.Should().Be("0m");
    }

    [Fact]
    public void FormatTimeSpan_WhenCalledWithDaysTimeSpan_ShouldReturnFormattedString()
    {
        // Arrange
        CreateViewModel();

        // Act
        var formatted = _viewModel.FormatTimeSpan(TimeSpan.FromDays(2));

        // Assert
        formatted.Should().Be("2d 0h");
    }
}