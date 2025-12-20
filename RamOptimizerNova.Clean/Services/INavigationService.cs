using System;
using System.Threading.Tasks;

namespace RamOptimizerNova.Services;

public interface INavigationService
{
    Task NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
    Task NavigateToAsync(Type viewModelType, object? parameter = null);
    Task GoBackAsync();
    Task ClearBackStackAsync();
    bool CanGoBack { get; }
    int BackStackCount { get; }
    event EventHandler<NavigatedEventArgs>? Navigated;
}

public class NavigatedEventArgs : EventArgs
{
    public Type ViewModelType { get; }
    public object? Parameter { get; }
    
    public NavigatedEventArgs(Type viewModelType, object? parameter)
    {
        ViewModelType = viewModelType;
        Parameter = parameter;
    }
}