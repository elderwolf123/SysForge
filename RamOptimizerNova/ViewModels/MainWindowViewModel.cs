using CommunityToolkit.Mvvm.Input;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IMessengerService _messengerService;
    
    [ObservableProperty]
    private string _greeting = "Welcome to RAM Optimizer Nova";
    
    [ObservableProperty]
    private string _currentStatus = "System Ready";
    
    public IAsyncRelayCommand NavigateToDashboardCommand { get; }
    public IAsyncRelayCommand NavigateToMemoryCommand { get; }
    public IAsyncRelayCommand NavigateToCPUCommand { get; }
    public IAsyncRelayCommand NavigateToCompressionCommand { get; }
    public IAsyncRelayCommand NavigateToStorageCommand { get; }
    public IAsyncRelayCommand NavigateToNetworkCommand { get; }
    
    public MainWindowViewModel(INavigationService navigationService, IMessengerService messengerService)
    {
        _navigationService = navigationService;
        _messengerService = messengerService;
        
        Title = "RAM Optimizer Nova";
        Status = "Ready";
        
        // Initialize navigation commands
        NavigateToDashboardCommand = new AsyncRelayCommand(NavigateToDashboard);
        NavigateToMemoryCommand = new AsyncRelayCommand(NavigateToMemory);
        NavigateToCPUCommand = new AsyncRelayCommand(NavigateToCPU);
        NavigateToCompressionCommand = new AsyncRelayCommand(NavigateToCompression);
        NavigateToStorageCommand = new AsyncRelayCommand(NavigateToStorage);
        NavigateToNetworkCommand = new AsyncRelayCommand(NavigateToNetwork);
        
        // Subscribe to messenger events
        _messengerService.SubscribeAsync<SystemMessage>(OnSystemMessage);
        _messengerService.SubscribeAsync<StatusMessage>(OnStatusMessage);
    }
    
    private async Task NavigateToDashboard()
    {
        await _navigationService.NavigateToAsync<DashboardViewModel>();
    }
    
    private async Task NavigateToMemory()
    {
        await _navigationService.NavigateToAsync<MemoryOptimizationViewModel>();
    }
    
    private async Task NavigateToCPU()
    {
        await _navigationService.NavigateToAsync<CPUOptimizationViewModel>();
    }
    
    private async Task NavigateToCompression()
    {
        await _navigationService.NavigateToAsync<CompressionViewModel>();
    }
    
    private async Task NavigateToStorage()
    {
        await _navigationService.NavigateToAsync<StorageOptimizationViewModel>();
    }
    
    private async Task NavigateToNetwork()
    {
        await _navigationService.NavigateToAsync<NetworkOptimizationViewModel>();
    }
    
    private async Task OnSystemMessage(SystemMessage message)
    {
        await Task.Run(() =>
        {
            CurrentStatus = $"{message.Type}: {message.Message}";
        });
    }
    
    private async Task OnStatusMessage(StatusMessage message)
    {
        await Task.Run(() =>
        {
            CurrentStatus = message.Status;
            Status = message.IsSuccess ? "Success" : "Error";
        });
    }
    
    public override async Task OnNavigatedTo(object? parameter = null)
    {
        await base.OnNavigatedTo(parameter);
        
        // Send initial system message
        await _messengerService.PublishAsync(new SystemMessage("System", "RAM Optimizer Nova initialized", DateTime.Now));
    }
    
    public override async Task OnNavigatedFrom()
    {
        await base.OnNavigatedFrom();
        
        // Unsubscribe from messenger events
        await _messengerService.UnsubscribeAsync<SystemMessage>(OnSystemMessage);
        await _messengerService.UnsubscribeAsync<StatusMessage>(OnStatusMessage);
    }
}
