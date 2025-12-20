using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.ViewModels;

public partial class NavigationViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    
    [ObservableProperty]
    private ViewModelBase? _currentViewModel;
    
    [ObservableProperty]
    private ObservableCollection<NavigationItemViewModel> _navigationItems = new();
    
    [ObservableProperty]
    private NavigationItemViewModel? _selectedNavigationItem;
    
    [ObservableProperty]
    private bool _isNavigationEnabled = true;
    
    [ObservableProperty]
    private string _currentTitle = "RAM Optimizer Nova";
    
    [ObservableProperty]
    private string _currentStatus = "Ready";
    
    public ICommand NavigateCommand { get; }
    public ICommand GoBackCommand { get; }
    public ICommand GoForwardCommand { get; }
    public ICommand RefreshCommand { get; }

    public NavigationViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        
        NavigateCommand = new AsyncRelayCommand<NavigationItemViewModel>(NavigateToAsync);
        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
        GoForwardCommand = new AsyncRelayCommand(GoForwardAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        
        // Subscribe to navigation events
        _navigationService.Navigated += OnNavigated;
        
        // Initialize navigation items
        InitializeNavigationItems();
        
        // Navigate to dashboard by default
        Task.Run(async () => await NavigateToAsync(NavigationItems.FirstOrDefault()));
    }
    
    private void InitializeNavigationItems()
    {
        var items = new List<NavigationItemViewModel>
        {
            new NavigationItemViewModel
            {
                Title = "Dashboard",
                Icon = "📊",
                ViewModelType = typeof(DashboardViewModel),
                Description = "System overview and real-time metrics"
            },
            new NavigationItemViewModel
            {
                Title = "CPU Optimization",
                Icon = "🔧",
                ViewModelType = typeof(CPUOptimizationViewModel),
                Description = "CPU performance and optimization"
            },
            new NavigationItemViewModel
            {
                Title = "Memory Optimization",
                Icon = "💾",
                ViewModelType = typeof(MemoryOptimizationViewModel),
                Description = "RAM management and optimization"
            },
            new NavigationItemViewModel
            {
                Title = "File Compression",
                Icon = "🗜️",
                ViewModelType = typeof(CompressionViewModel),
                Description = "File compression and optimization"
            },
            new NavigationItemViewModel
            {
                Title = "Storage Optimization",
                Icon = "💿",
                ViewModelType = typeof(StorageOptimizationViewModel),
                Description = "Storage management and cleanup"
            },
            new NavigationItemViewModel
            {
                Title = "Network Optimization",
                Icon = "🌐",
                ViewModelType = typeof(NetworkOptimizationViewModel),
                Description = "Network performance and QoS"
            }
        };
        
        NavigationItems = new ObservableCollection<NavigationItemViewModel>(items);
    }
    
    private async Task NavigateToAsync(NavigationItemViewModel? item)
    {
        if (item == null || !IsNavigationEnabled)
            return;
            
        try
        {
            IsNavigationEnabled = false;
            SelectedNavigationItem = item;
            CurrentTitle = item.Title;
            CurrentStatus = $"Loading {item.Title}...";
            
            await _navigationService.NavigateToAsync(item.ViewModelType);
            
            CurrentStatus = $"{item.Title} loaded";
        }
        catch (Exception ex)
        {
            CurrentStatus = $"Error loading {item.Title}: {ex.Message}";
        }
        finally
        {
            IsNavigationEnabled = true;
        }
    }
    
    private async Task GoBackAsync()
    {
        if (!_navigationService.CanGoBack)
            return;
            
        try
        {
            IsNavigationEnabled = false;
            CurrentStatus = "Navigating back...";
            
            await _navigationService.GoBackAsync();
            
            CurrentStatus = "Navigation complete";
        }
        catch (Exception ex)
        {
            CurrentStatus = $"Error navigating back: {ex.Message}";
        }
        finally
        {
            IsNavigationEnabled = true;
        }
    }
    
    private async Task GoForwardAsync()
    {
        if (!_navigationService.CanGoForward)
            return;
            
        try
        {
            IsNavigationEnabled = false;
            CurrentStatus = "Navigating forward...";
            
            await _navigationService.GoForwardAsync();
            
            CurrentStatus = "Navigation complete";
        }
        catch (Exception ex)
        {
            CurrentStatus = $"Error navigating forward: {ex.Message}";
        }
        finally
        {
            IsNavigationEnabled = true;
        }
    }
    
    private async Task RefreshAsync()
    {
        if (SelectedNavigationItem == null)
            return;
            
        try
        {
            IsNavigationEnabled = false;
            CurrentStatus = "Refreshing...";
            
            // Navigate to the same page to refresh
            await NavigateToAsync(SelectedNavigationItem);
            
            CurrentStatus = "Refresh complete";
        }
        catch (Exception ex)
        {
            CurrentStatus = $"Error refreshing: {ex.Message}";
        }
        finally
        {
            IsNavigationEnabled = true;
        }
    }
    
    private void OnNavigated(object? sender, NavigatedEventArgs e)
    {
        // Update current view model
        if (_serviceProvider.GetService(e.ViewModelType) is ViewModelBase viewModel)
        {
            CurrentViewModel = viewModel;
            
            // Update selected navigation item
            var selectedItem = NavigationItems.FirstOrDefault(item => item.ViewModelType == e.ViewModelType);
            if (selectedItem != null)
            {
                SelectedNavigationItem = selectedItem;
            }
        }
    }
    
    public override async Task OnNavigatedTo(object? parameter = null)
    {
        await base.OnNavigatedTo(parameter);
        CurrentStatus = "Navigation system ready";
    }
    
    public override async Task OnNavigatedFrom()
    {
        await base.OnNavigatedFrom();
        _navigationService.Navigated -= OnNavigated;
    }
}

public class NavigationItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Type ViewModelType { get; set; } = typeof(ViewModelBase);
    
    public bool IsSelected { get; set; }
    public bool IsEnabled { get; set; } = true;
}