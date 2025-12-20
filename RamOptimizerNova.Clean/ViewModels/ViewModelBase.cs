using CommunityToolkit.Mvvm.ComponentModel;

namespace RamOptimizerNova.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string _status = string.Empty;
    
    [ObservableProperty]
    private bool _isBusy;
    
    public virtual Task OnNavigatedTo(object? parameter = null)
    {
        return Task.CompletedTask;
    }
    
    public virtual Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }
}
