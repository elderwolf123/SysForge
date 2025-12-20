using CommunityToolkit.Mvvm.Input;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IMessengerService _messengerService;
    
    [ObservableProperty]
    private string _welcomeMessage = "System Dashboard";
    
    [ObservableProperty]
    private string _systemStatus = "All Systems Operational";
    
    [ObservableProperty]
    private double _cpuUsage = 0;
    
    [ObservableProperty]
    private double _memoryUsage = 0;
    
    [ObservableProperty]
    private double _diskUsage = 0;
    
    [ObservableProperty]
    private double _networkUsage = 0;
    
    public DashboardViewModel(IMessengerService messengerService)
    {
        _messengerService = messengerService;
        Title = "Dashboard";
        Status = "Active";
        
        // Start real-time updates
        StartRealTimeUpdates();
    }
    
    private async void StartRealTimeUpdates()
    {
        while (true)
        {
            await UpdateSystemMetrics();
            await Task.Delay(1000); // Update every second
        }
    }
    
    private async Task UpdateSystemMetrics()
    {
        await Task.Run(() =>
        {
            // Simulate real-time metrics (in real implementation, this would come from actual system monitoring)
            var random = new Random();
            CpuUsage = random.Next(20, 80);
            MemoryUsage = random.Next(30, 90);
            DiskUsage = random.Next(10, 60);
            NetworkUsage = random.Next(5, 50);
        });
    }
    
    public override async Task OnNavigatedTo(object? parameter = null)
    {
        await base.OnNavigatedTo(parameter);
        
        await _messengerService.PublishAsync(new SystemMessage("Navigation", "Dashboard loaded", DateTime.Now));
    }
}