using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimeViews;
using Avalonia.Threading;
using RamOptimizerNova.ViewModels;

namespace RamOptimizerNova.Services;

public interface INavigationService
{
    Task NavigateToAsync<TViewModel>() where TViewModel : ViewModelBase;
    Task NavigateToAsync(Type viewModelType);
    Task GoBackAsync();
    Task GoForwardAsync();
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    IReadOnlyList<Type> NavigationHistory { get; }
    event EventHandler<NavigatedEventArgs>? Navigated;
}

public class NavigatedEventArgs : EventArgs
{
    public Type ViewModelType { get; }
    public object? Parameter { get; }
    public DateTime Timestamp { get; }

    public NavigatedEventArgs(Type viewModelType, object? parameter = null)
    {
        ViewModelType = viewModelType;
        Parameter = parameter;
        Timestamp = DateTime.Now;
    }
}

public class NavigationService : INavigationService
{
    private readonly Dictionary<Type, Type> _viewModelToViewMap = new();
    private readonly Stack<Type> _backStack = new();
    private readonly Stack<Type> _forwardStack = new();
    private Type? _currentViewModelType;
    private readonly IServiceProvider _serviceProvider;
    private readonly IViewLocator _viewLocator;

    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;
    public IReadOnlyList<Type> NavigationHistory => _backStack.Reverse().Concat(new[] { _currentViewModelType! }).Concat(_forwardStack).ToList();

    public event EventHandler<NavigatedEventArgs>? Navigated;

    public NavigationService(IServiceProvider serviceProvider, IViewLocator viewLocator)
    {
        _serviceProvider = serviceProvider;
        _viewLocator = viewLocator;
        
        // Register default view mappings
        RegisterViewMappings();
    }

    private void RegisterViewMappings()
    {
        // Register view mappings for all ViewModels
        _viewModelToViewMap[typeof(DashboardViewModel)] = typeof(DashboardView);
        _viewModelToViewMap[typeof(CPUOptimizationViewModel)] = typeof(CPUOptimizationView);
        _viewModelToViewMap[typeof(MemoryOptimizationViewModel)] = typeof(MemoryOptimizationView);
        _viewModelToViewMap[typeof(CompressionViewModel)] = typeof(CompressionView);
        _viewModelToViewMap[typeof(StorageOptimizationViewModel)] = typeof(StorageOptimizationView);
        _viewModelToViewMap[typeof(NetworkOptimizationViewModel)] = typeof(NetworkOptimizationView);
    }

    public async Task NavigateToAsync<TViewModel>() where TViewModel : ViewModelBase
    {
        await NavigateToAsync(typeof(TViewModel));
    }

    public async Task NavigateToAsync(Type viewModelType)
    {
        if (!typeof(ViewModelBase).IsAssignableFrom(viewModelType))
        {
            throw new ArgumentException($"ViewModel type must inherit from {nameof(ViewModelBase)}");
        }

        if (!_viewModelToViewMap.ContainsKey(viewModelType))
        {
            throw new ArgumentException($"No view mapping found for {viewModelType.Name}");
        }

        // Add current to back stack if not null
        if (_currentViewModelType != null)
        {
            _backStack.Push(_currentViewModelType);
            _forwardStack.Clear(); // Clear forward stack when navigating to new page
        }

        _currentViewModelType = viewModelType;

        // Raise navigated event
        Navigated?.Invoke(this, new NavigatedEventArgs(viewModelType));

        // Notify the current view model of navigation
        if (_serviceProvider.GetService(viewModelType) is ViewModelBase currentViewModel)
        {
            await currentViewModel.OnNavigatedTo();
        }
    }

    public async Task GoBackAsync()
    {
        if (!CanGoBack)
            return;

        var currentType = _currentViewModelType;
        var previousType = _backStack.Pop();

        if (currentType != null)
        {
            _forwardStack.Push(currentType);
        }

        _currentViewModelType = previousType;

        // Raise navigated event
        Navigated?.Invoke(this, new NavigatedEventArgs(previousType));

        // Notify the current view model of navigation
        if (_serviceProvider.GetService(previousType) is ViewModelBase currentViewModel)
        {
            await currentViewModel.OnNavigatedTo();
        }
    }

    public async Task GoForwardAsync()
    {
        if (!CanGoForward)
            return;

        var currentType = _currentViewModelType;
        var nextType = _forwardStack.Pop();

        if (currentType != null)
        {
            _backStack.Push(currentType);
        }

        _currentViewModelType = nextType;

        // Raise navigated event
        Navigated?.Invoke(this, new NavigatedEventArgs(nextType));

        // Notify the current view model of navigation
        if (_serviceProvider.GetService(nextType) is ViewModelBase currentViewModel)
        {
            await currentViewModel.OnNavigatedTo();
        }
    }

    public Control? GetViewForViewModel(Type viewModelType)
    {
        if (!_viewModelToViewMap.TryGetValue(viewModelType, out var viewType))
        {
            return null;
        }

        return _viewLocator.ResolveView(viewType);
    }

    public T? GetViewForViewModel<T>() where T : Control
    {
        var viewModelType = typeof(T).Name.Replace("View", "ViewModel");
        var fullViewModelType = Type.GetType($"RamOptimizerNova.ViewModels.{viewModelType}");
        
        if (fullViewModelType == null)
            return null;

        var view = GetViewForViewModel(fullViewModelType);
        return view as T;
    }
}

public interface IViewLocator
{
    Control ResolveView(Type viewType);
}

public class DefaultViewLocator : IViewLocator
{
    private readonly IServiceProvider _serviceProvider;

    public DefaultViewLocator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Control ResolveView(Type viewType)
    {
        // Try to resolve view from DI container
        if (_serviceProvider.GetService(viewType) is Control view)
        {
            return view;
        }

        // Try to create view instance
        try
        {
            return (Control)Activator.CreateInstance(viewType)!;
        }
        catch
        {
            return null;
        }
    }
}