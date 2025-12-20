using CommunityToolkit.Mvvm.Input;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.ViewModels;

public partial class MemoryOptimizationViewModel : ViewModelBase
{
    private readonly IMessengerService _messengerService;
    
    [ObservableProperty]
    private string _title = "Memory Optimization";
    
    [ObservableProperty]
    private string _status = "Ready";
    
    [ObservableProperty]
    private double _totalMemory = 0;
    
    [ObservableProperty]
    private double _usedMemory = 0;
    
    [ObservableProperty]
    private double _availableMemory = 0;
    
    [ObservableProperty]
    private int _processesOptimized = 0;
    
    [ObservableProperty]
    private bool _isOptimizationRunning = false;
    
    public IAsyncRelayCommand StartOptimizationCommand { get; }
    public IAsyncRelayCommand StopOptimizationCommand { get; }
    public IAsyncRelayCommand ForceCleanupCommand { get; }
    
    public MemoryOptimizationViewModel(IMessengerService messengerService)
    {
        _messengerService = messengerService;
        Title = "Memory Optimization";
        Status = "Ready";
        
        StartOptimizationCommand = new AsyncRelayCommand(StartOptimization);
        StopOptimizationCommand = new AsyncRelayCommand(StopOptimization);
        ForceCleanupCommand = new AsyncRelayCommand(ForceCleanup);
        
        InitializeMemoryMetrics();
    }
    
    private void InitializeMemoryMetrics()
    {
        // Simulate memory metrics (in real implementation, this would come from actual system monitoring)
        TotalMemory = 16.0; // GB
        UsedMemory = 8.5;
        AvailableMemory = TotalMemory - UsedMemory;
    }
    
    private async Task StartOptimization()
    {
        if (IsOptimizationRunning)
            return;
            
        IsOptimizationRunning = true;
        Status = "Optimization Running...";
        
        await _messengerService.PublishAsync(new StatusMessage("Memory optimization started", true));
        
        // Simulate optimization process
        await Task.Run(async () =>
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);
                ProcessesOptimized = i + 1;
                
                // Simulate memory cleanup
                UsedMemory = Math.Max(2.0, UsedMemory - 0.3);
                AvailableMemory = TotalMemory - UsedMemory;
            }
        });
        
        IsOptimizationRunning = false;
        Status = "Optimization Complete";
        await _messengerService.PublishAsync(new StatusMessage("Memory optimization completed", true));
    }
    
    private async Task StopOptimization()
    {
        IsOptimizationRunning = false;
        Status = "Optimization Stopped";
        await _messengerService.PublishAsync(new StatusMessage("Memory optimization stopped", false));
    }
    
    private async Task ForceCleanup()
    {
        Status = "Force Cleanup Running...";
        await _messengerService.PublishAsync(new StatusMessage("Force cleanup initiated", true));
        
        await Task.Run(async () =>
        {
            await Task.Delay(1000);
            UsedMemory = Math.Max(1.0, UsedMemory - 2.0);
            AvailableMemory = TotalMemory - UsedMemory;
        });
        
        Status = "Force Cleanup Complete";
        await _messengerService.PublishAsync(new StatusMessage("Force cleanup completed", true));
    }
    
    public override async Task OnNavigatedTo(object? parameter = null)
    {
        await base.OnNavigatedTo(parameter);
        await _messengerService.PublishAsync(new SystemMessage("Navigation", "Memory Optimization loaded", DateTime.Now));
    }
}