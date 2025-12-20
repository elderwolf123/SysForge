using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace RamOptimizerNova.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private string _title = "RAM Optimizer Nova";
    private string _status = "Ready";
    
    public string Title
    {
        get => _title;
        protected set => SetProperty(ref _title, value);
    }
    
    public string Status
    {
        get => _status;
        protected set => SetProperty(ref _status, value);
    }
    
    public virtual void OnNavigatedTo(object? parameter = null)
    {
    }
    
    public virtual void OnNavigatedFrom()
    {
    }
    
    public virtual bool CanGoBack => false;
    
    public virtual IAsyncRelayCommand GoBackCommand => new AsyncRelayCommand(ExecuteGoBack);
    
    protected virtual async Task ExecuteGoBack()
    {
        await Task.CompletedTask;
    }
}
