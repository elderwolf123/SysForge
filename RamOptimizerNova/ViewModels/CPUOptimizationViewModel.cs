using CommunityToolkit.Mvvm.Input;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.ViewModels;

public partial class CPUOptimizationViewModel : ViewModelBase
{
    private readonly IMessengerService _messengerService;
    
    [ObservableProperty]
    private string _title = "CPU Optimization";
    
    [ObservableProperty]
    private string _status = "Ready";
    
    [ObservableProperty]
    private double _cpuUsage = 0;
    
    [ObservableProperty]
    private double _temperature = 0;
    
    [ObservableProperty]
    private int _activeProcesses = 0;
    
    [ObservableProperty]
    private int _optimizedProcesses = 0;
    
    [ObservableProperty]
    private bool _isOptimizationRunning = false;
    
    [ObservableProperty]
    private int _optimizationLevel = 1;
    
    public IAsyncRelayCommand StartOptimizationCommand { get; }
    public IAsyncRelayCommand StopOptimizationCommand { get; }
    public IAsyncRelayCommand SetOptimizationLevelCommand { get; }
    
    public CPUOptimizationViewModel(IMessengerService messengerService)
    {
        _messengerService = messengerService;
        Title = "CPU Optimization";
        Status = "Ready";
        
        StartOptimizationCommand = new AsyncRelayCommand(StartOptimization);
        StopOptimizationCommand = new AsyncRelayCommand(StopOptimization);
        SetOptimizationLevelCommand = new AsyncRelayCommand<int>(SetOptimizationLevel);
        
        InitializeCPUMetrics();
    }
    
    private void InitializeCPUMetrics()
    {
        // Simulate CPU metrics (in real implementation, this would come from actual system monitoring)
        CpuUsage = 45.0;
        Temperature = 65.0;
        ActiveProcesses = 120;
    }
    
    private async Task StartOptimization()
    {
        if (IsOptimizationRunning)
            return;
            
        IsOptimizationRunning = true;
        Status = "CPU Optimization Running...";
        
        await _messengerService.PublishAsync(new StatusMessage("CPU optimization started", true));
        
        // Simulate CPU optimization process
        await Task.Run(async () =>
        {
            for (int i = 0; i < 15; i++)
            {
                await Task.Delay(300);
                OptimizedProcesses = i + 1;
                
                // Simulate CPU optimization
                CpuUsage = Math.Max(10.0, CpuUsage - (OptimizationLevel * 2.0));
                Temperature = Math.Max(45.0, Temperature - (OptimizationLevel * 1.5));
                ActiveProcesses = Math.Max(50, ActiveProcesses - (OptimizationLevel * 3));
            }
        });
        
        IsOptimizationRunning = false;
        Status = "Optimization Complete";
        await _messengerService.PublishAsync(new StatusMessage("CPU optimization completed", true));
    }
    
    private async Task StopOptimization()
    {
        IsOptimizationRunning = false;
        Status = "Optimization Stopped";
        await _messengerService.PublishAsync(new StatusMessage("CPU optimization stopped", false));
    }
    
    private async Task SetOptimizationLevel(int level)
    {
        OptimizationLevel = Math.Max(1, Math.Min(7, level));
        Status = $"Optimization Level: {OptimizationLevel}";
        await _messengerService.PublishAsync(new StatusMessage($"CPU optimization level set to {OptimizationLevel}", true));
    }
    
    public override async Task OnNavigatedTo(object? parameter = null)
    {
        await base.OnNavigatedTo(parameter);
        await _messengerService.PublishAsync(new SystemMessage("Navigation", "CPU Optimization loaded", DateTime.Now));
    }
}