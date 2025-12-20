using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAssertions;
using Moq;
using Xunit;
using RamOptimizerNova.ViewModels;
using RamOptimizerNova.Services.Interfaces;
using RamOptimizerNova.Models;

namespace RamOptimizerNova.Tests.ViewModels;

/// <summary>
/// Unit tests for MainViewModel
/// </summary>
public class MainViewModelTests
{
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly Mock<IThemeService> _mockThemeService;
    private readonly Mock<IPerformanceMonitoringService> _mockPerformanceMonitoringService;
    private readonly Mock<IRamOptimizerBackendService> _mockBackendService;
    private readonly Mock<IRealTimeDataSynchronizationService> _mockSynchronizationService;
    private readonly Mock<IErrorHandlingAndLoggingService> _mockErrorHandlingService;
    private readonly Mock<INetworkService> _mockNetworkService;
    private readonly Mock<IMetricsService> _mockMetricsService;
    private readonly Mock<ISystemService> _mockSystemService;
    private readonly Mock<IHardwareService> _mockHardwareService;
    private readonly Mock<IOptimizationService> _mockOptimizationService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ILoggerService> _mockLoggerService;

    private MainViewModel _viewModel;

    public MainViewModelTests()
    {
        // Mock all dependencies
        _mockNavigationService = new Mock<INavigationService>();
        _mockThemeService = new Mock<IThemeService>();
        _mockPerformanceMonitoringService = new Mock<IPerformanceMonitoringService>();
        _mockBackendService = new Mock<IRamOptimizerBackendService>();
        _mockSynchronizationService = new Mock<IRealTimeDataSynchronizationService>();
        _mockErrorHandlingService = new Mock<IErrorHandlingAndLoggingService>();
        _mockNetworkService = new Mock<INetworkService>();
        _mockMetricsService = new Mock<IMetricsService>();
        _mockSystemService = new Mock<ISystemService>();
        _mockHardwareService = new Mock<IHardwareService>();
        _mockOptimizationService = new Mock<IOptimizationService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockLoggerService = new Mock<ILoggerService>();

        // Setup default behavior
        SetupDefaultBehavior();
    }

    private void SetupDefaultBehavior()
    {
        // Navigation service
        _mockNavigationService.Setup(x => x.NavigateToAsync<DashboardViewModel>())
            .Returns(Task.CompletedTask);

        // Theme service
        _mockThemeService.Setup(x => x.GetThemeAsync())
            .ReturnsAsync(ThemeType.Dark);
        _mockThemeService.Setup(x => x.ApplyThemeAsync(It.IsAny<ThemeType>()))
            .Returns(Task.CompletedTask);

        // Performance monitoring service
        _mockPerformanceMonitoringService.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);
        _mockPerformanceMonitoringService.Setup(x => x.StartMonitoringAsync())
            .Returns(Task.CompletedTask);
        _mockPerformanceMonitoringService.Setup(x => x.StopMonitoringAsync())
            .Returns(Task.CompletedTask);

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
            .ReturnsAsync(new SystemMetrics());
        _mockSynchronizationService.Setup(x => x.GetHardwareStateAsync())
            .ReturnsAsync(new HardwareState());
        _mockSynchronizationService.Setup(x => x.GetOptimizationResultsAsync())
            .ReturnsAsync(new OptimizationResults());

        // Error handling service
        _mockErrorHandlingService.Setup(x => x.InitializeAsync())
            .ReturnsAsync(true);
        _mockErrorHandlingService.Setup(x => x.LogErrorAsync(It.IsAny<string>(), It.IsAny<Exception>()))
            .Returns(Task.CompletedTask);
        _mockErrorHandlingService.Setup(x => x.LogWarningAsync(It.IsAny<string>(), It.IsAny<Exception>()))
            .Returns(Task.CompletedTask);
        _mockErrorHandlingService.Setup(x => x.LogInfoAsync(It.IsAny<string>(), It.IsAny<Exception>()))
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
    }

    private void CreateViewModel()
    {
        _viewModel = new MainViewModel(
            _mockNavigationService.Object,
            _mockThemeService.Object,
            _mockPerformanceMonitoringService.Object,
            _mockBackendService.Object,
            _mockSynchronizationService.Object,
            _mockErrorHandlingService.Object,
            _mockNetworkService.Object,
            _mockMetricsService.Object,
            _mockSystemService.Object,
            _mockHardwareService.Object,
            _mockOptimizationService.Object,
            _mockDialogService.Object,
            _mockLoggerService.Object);
    }

    [Fact]
    public void Constructor_WhenCalled_ShouldInitializeProperties()
    {
        // Arrange & Act
        CreateViewModel();

        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Title.Should().Be("RAM Optimizer Nova");
        _viewModel.Version.Should().NotBeNullOrEmpty();
        _viewModel.IsInitialized.Should().BeFalse();
        _viewModel.IsLoading.Should().BeTrue();
        _viewModel.CurrentPage.Should().BeNull();
        _viewModel.NavigationItems.Should().NotBeNull();
        _viewModel.NavigationItems.Should().NotBeEmpty();
        _viewModel.NavigationItems.Should().HaveCount(8);
    }

    [Fact]
    public void Constructor_WhenCalled_ShouldSetupNavigationItems()
    {
        // Arrange & Act
        CreateViewModel();

        // Assert
        var dashboardItem = _viewModel.NavigationItems[0];
        dashboardItem.Name.Should().Be("Dashboard");
        dashboardItem.Icon.Should().Be("📊");
        dashboardItem.PageType.Should().Be(typeof(DashboardViewModel));

        var cpuItem = _viewModel.NavigationItems[1];
        cpuItem.Name.Should().Be("CPU Optimization");
        cpuItem.Icon.Should().Be("⚡");
        cpuItem.PageType.Should().Be(typeof(CpuOptimizationViewModel));

        var memoryItem = _viewModel.NavigationItems[2];
        memoryItem.Name.Should().Be("Memory Optimization");
        memoryItem.Icon.Should().Be("💾");
        memoryItem.PageType.Should().Be(typeof(MemoryOptimizationViewModel));

        var compressionItem = _viewModel.NavigationItems[3];
        compressionItem.Name.Should().Be("Compression");
        compressionItem.Icon.Should().Be("🗜️");
        compressionItem.PageType.Should().Be(typeof(CompressionViewModel));

        var storageItem = _viewModel.NavigationItems[4];
        storageItem.Name.Should().Be("Storage");
        storageItem.Icon.Should().Be("💿");
        storageItem.PageType.Should().Be(typeof(StorageOptimizationViewModel));

        var networkItem = _viewModel.NavigationItems[5];
        networkItem.Name.Should().Be("Network");
        networkItem.Icon.Should().Be("🌐");
        networkItem.PageType.Should().Be(typeof(NetworkOptimizationViewModel));

        var settingsItem = _viewModel.NavigationItems[6];
        settingsItem.Name.Should().Be("Settings");
        settingsItem.Icon.Should().Be("⚙️");
        settingsItem.PageType.Should().Be(typeof(SettingsViewModel));

        var aboutItem = _viewModel.NavigationItems[7];
        aboutItem.Name.Should().Be("About");
        aboutItem.Icon.Should().Be("ℹ️");
        aboutItem.PageType.Should().Be(typeof(AboutViewModel));
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
        _mockNavigationService.Verify(x => x.NavigateToAsync<DashboardViewModel>(), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenBackendInitializationFails_ShouldLogError()
    {
        // Arrange
        _mockBackendService.Setup(x => x.InitializeAsync())
            .ReturnsAsync(false);

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
    public async Task NavigateToAsync_WhenCalledWithValidPage_ShouldNavigate()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.NavigateToAsync<CpuOptimizationViewModel>();

        // Assert
        _viewModel.CurrentPage.Should().Be(typeof(CpuOptimizationViewModel));
        _mockNavigationService.Verify(x => x.NavigateToAsync<CpuOptimizationViewModel>(), Times.Once);
    }

    [Fact]
    public async Task NavigateToAsync_WhenCalledWithInvalidPage_ShouldNotNavigate()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.NavigateToAsync<InvalidViewModelType>();

        // Assert
        _viewModel.CurrentPage.Should().BeNull();
        _mockNavigationService.Verify(x => x.NavigateToAsync<InvalidViewModelType>(), Times.Never);
    }

    [Fact]
    public void NavigateToAsync_WhenNotInitialized_ShouldNotNavigate()
    {
        // Arrange
        CreateViewModel();

        // Act
        var action = async () => await _viewModel.NavigateToAsync<CpuOptimizationViewModel>();

        // Assert
        action.Should().NotThrow();
        _mockNavigationService.Verify(x => x.NavigateToAsync<CpuOptimizationViewModel>(), Times.Never);
    }

    [Fact]
    public void NavigateToCommand_WhenCalledWithValidPage_ShouldNavigate()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        _viewModel.NavigateToCommand.Execute(typeof(CpuOptimizationViewModel));

        // Assert
        _viewModel.CurrentPage.Should().Be(typeof(CpuOptimizationViewModel));
        _mockNavigationService.Verify(x => x.NavigateToAsync<CpuOptimizationViewModel>(), Times.Once);
    }

    [Fact]
    public void NavigateToCommand_WhenCalledWithInvalidPage_ShouldNotNavigate()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        _viewModel.NavigateToCommand.Execute(typeof(InvalidViewModelType));

        // Assert
        _viewModel.CurrentPage.Should().BeNull();
        _mockNavigationService.Verify(x => x.NavigateToAsync<InvalidViewModelType>(), Times.Never);
    }

    [Fact]
    public void NavigateToCommand_WhenNotInitialized_ShouldNotNavigate()
    {
        // Arrange
        CreateViewModel();

        // Act
        _viewModel.NavigateToCommand.Execute(typeof(CpuOptimizationViewModel));

        // Assert
        _viewModel.CurrentPage.Should().BeNull();
        _mockNavigationService.Verify(x => x.NavigateToAsync<CpuOptimizationViewModel>(), Times.Never);
    }

    [Fact]
    public void NavigateToCommand_WhenParameterIsNull_ShouldNotNavigate()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        _viewModel.NavigateToCommand.Execute(null);

        // Assert
        _viewModel.CurrentPage.Should().BeNull();
        _mockNavigationService.Verify(x => x.NavigateToAsync(It.IsAny<Type>()), Times.Never);
    }

    [Fact]
    public void NavigateToCommand_WhenParameterIsNotType_ShouldNotNavigate()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        _viewModel.NavigateToCommand.Execute("InvalidParameter");

        // Assert
        _viewModel.CurrentPage.Should().BeNull();
        _mockNavigationService.Verify(x => x.NavigateToAsync(It.IsAny<Type>()), Times.Never);
    }

    [Fact]
    public async Task ToggleThemeAsync_WhenCalled_ShouldToggleTheme()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.ToggleThemeAsync();

        // Assert
        _mockThemeService.Verify(x => x.ApplyThemeAsync(ThemeType.Light), Times.Once);
    }

    [Fact]
    public async Task ToggleThemeAsync_WhenCalledTwice_ShouldToggleThemeTwice()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.ToggleThemeAsync();
        await _viewModel.ToggleThemeAsync();

        // Assert
        _mockThemeService.Verify(x => x.ApplyThemeAsync(ThemeType.Light), Times.Once);
        _mockThemeService.Verify(x => x.ApplyThemeAsync(ThemeType.Dark), Times.Once);
    }

    [Fact]
    public async Task ToggleThemeAsync_WhenNotInitialized_ShouldNotToggleTheme()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.ToggleThemeAsync();

        // Assert
        _mockThemeService.Verify(x => x.ApplyThemeAsync(It.IsAny<ThemeType>()), Times.Never);
    }

    [Fact]
    public async Task ShowAboutAsync_WhenCalled_ShouldShowAboutDialog()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.ShowAboutAsync();

        // Assert
        _mockDialogService.Verify(x => x.ShowInfoAsync(
            "About RAM Optimizer Nova", 
            It.IsAny<string>()), 
            Times.Once);
    }

    [Fact]
    public async Task ShowAboutAsync_WhenNotInitialized_ShouldNotShowAboutDialog()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.ShowAboutAsync();

        // Assert
        _mockDialogService.Verify(x => x.ShowInfoAsync(
            "About RAM Optimizer Nova", 
            It.IsAny<string>()), 
            Times.Never);
    }

    [Fact]
    public async Task ShowSettingsAsync_WhenCalled_ShouldNavigateToSettings()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        await _viewModel.ShowSettingsAsync();

        // Assert
        _viewModel.CurrentPage.Should().Be(typeof(SettingsViewModel));
        _mockNavigationService.Verify(x => x.NavigateToAsync<SettingsViewModel>(), Times.Once);
    }

    [Fact]
    public async Task ShowSettingsAsync_WhenNotInitialized_ShouldNotNavigate()
    {
        // Arrange
        CreateViewModel();

        // Act
        await _viewModel.ShowSettingsAsync();

        // Assert
        _viewModel.CurrentPage.Should().BeNull();
        _mockNavigationService.Verify(x => x.NavigateToAsync<SettingsViewModel>(), Times.Never);
    }

    [Fact]
    public void CanNavigateTo_WhenValidPage_ShouldReturnTrue()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var canNavigate = _viewModel.CanNavigateTo(typeof(CpuOptimizationViewModel));

        // Assert
        canNavigate.Should().BeTrue();
    }

    [Fact]
    public void CanNavigateTo_WhenInvalidPage_ShouldReturnFalse()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var canNavigate = _viewModel.CanNavigateTo(typeof(InvalidViewModelType));

        // Assert
        canNavigate.Should().BeFalse();
    }

    [Fact]
    public void CanNavigateTo_WhenNotInitialized_ShouldReturnFalse()
    {
        // Arrange
        CreateViewModel();

        // Act
        var canNavigate = _viewModel.CanNavigateTo(typeof(CpuOptimizationViewModel));

        // Assert
        canNavigate.Should().BeFalse();
    }

    [Fact]
    public void CanNavigateTo_WhenParameterIsNull_ShouldReturnFalse()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var canNavigate = _viewModel.CanNavigateTo(null);

        // Assert
        canNavigate.Should().BeFalse();
    }

    [Fact]
    public void CanNavigateTo_WhenParameterIsNotType_ShouldReturnFalse()
    {
        // Arrange
        CreateViewModel();
        await _viewModel.InitializeAsync();

        // Act
        var canNavigate = _viewModel.CanNavigateTo("InvalidParameter");

        // Assert
        canNavigate.Should().BeFalse();
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
        _mockErrorHandlingService.Verify(x => x.CleanupAsync(), Times.Once);
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
            if (e.PropertyName == nameof(MainViewModel.IsInitialized))
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
    public void OnPropertyChanged_WhenSamePropertySetMultipleTimes_ShouldRaisePropertyChangedEachTime()
    {
        // Arrange
        CreateViewModel();
        int propertyChangedCount = 0;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsInitialized))
            {
                propertyChangedCount++;
            }
        };

        // Act
        _viewModel.IsInitialized = true;
        _viewModel.IsInitialized = false;
        _viewModel.IsInitialized = true;

        // Assert
        propertyChangedCount.Should().Be(3);
    }

    [Fact]
    public void OnPropertyChanged_WhenDifferentPropertiesChange_ShouldRaisePropertyChangedForEach()
    {
        // Arrange
        CreateViewModel();
        int propertyChangedCount = 0;
        _viewModel.PropertyChanged += (sender, e) => { propertyChangedCount++; };

        // Act
        _viewModel.IsInitialized = true;
        _viewModel.IsLoading = false;
        _viewModel.CurrentPage = typeof(CpuOptimizationViewModel);

        // Assert
        propertyChangedCount.Should().Be(3);
    }

    // Helper class for testing invalid navigation
    private class InvalidViewModelType { }
}