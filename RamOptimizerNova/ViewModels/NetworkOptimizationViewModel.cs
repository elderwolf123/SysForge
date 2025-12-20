using CommunityToolkit.Mvvm.Input;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.ViewModels;

public partial class NetworkOptimizationViewModel : ViewModelBase
{
    private readonly IMessengerService _messengerService;
    
    [ObservableProperty]
    private string _title = "Network Optimization";
    
    [ObservableProperty]
    private string _status = "Ready";
    
    [ObservableProperty]
    private double _downloadSpeed = 0;
    
    [ObservableProperty]
    private double _uploadSpeed = 0;
    
    [ObservableProperty]
    private double _latency = 0;
    
    [ObservableProperty]
    private double _packetLoss = 0;
    
    [ObservableProperty]
    private int _activeConnections = 0;
    
    [ObservableProperty]
    private int _optimizedConnections = 0;
    
    [ObservableProperty]
    private bool _isOptimizationRunning = false;
    
    [ObservableProperty]
    private string _selectedProfile = "Gaming";
    
    public IAsyncRelayCommand StartOptimizationCommand { get; }
    public IAsyncRelayCommand StopOptimizationCommand { get; }
    public IAsyncRelayCommand SelectProfileCommand { get; }
    
    public NetworkOptimizationViewModel(IMessengerService messengerService)
    {
        _messengerService = messengerService;
        Title = "Network Optimization";
        Status = "Ready";
        
        StartOptimizationCommand = new AsyncRelayCommand(StartOptimization);
        StopOptimizationCommand = new AsyncRelayCommand(StopOptimization);
        SelectProfileCommand = new AsyncRelayCommand<string>(SelectProfile);
        
        InitializeNetworkMetrics();
    }
    
    private void InitializeNetworkMetrics()
    {
        // Simulate network metrics
        DownloadSpeed = 100.0; // Mbps
        UploadSpeed = 50.0; // Mbps
        Latency = 25.0; // ms
        PacketLoss = 0.5; // %
        ActiveConnections = 15;
        OptimizedConnections = 0;
    }
    
    private async Task StartOptimization()
    {
        if (IsOptimizationRunning)
            return;
            
        IsOptimizationRunning = true;
        Status = "Network Optimization Running...";
        
        await _messengerService.PublishAsync(new StatusMessage("Network optimization started", true));
        
        // Simulate network optimization process
        await Task.Run(async () =>
        {
            var profiles = new[] { "Gaming", "Streaming", "Downloads", "VoIP" };
            var improvements = new[]
            {
                (download: 1.5, upload: 1.3, latency: -0.3, packetLoss: -0.2), // Gaming
                (download: 1.2, upload: 1.1, latency: -0.1, packetLoss: -0.1), // Streaming
                (download: 1.8, upload: 1.6, latency: -0.2, packetLoss: -0.1), // Downloads
                (download: 1.1, upload: 1.4, latency: -0.4, packetLoss: -0.3)  // VoIP
            };
            
            var profileIndex = Array.IndexOf(profiles, SelectedProfile);
            var improvement = improvements[profileIndex];
            
            for (int i = 0; i < 20; i++)
            {
                if (!IsOptimizationRunning) break;
                
                await Task.Delay(200);
                OptimizedConnections = Math.Min(ActiveConnections, OptimizedConnections + 1);
                
                // Simulate network improvements
                DownloadSpeed *= (1 + improvement.download * 0.05);
                UploadSpeed *= (1 + improvement.upload * 0.05);
                Latency = Math.Max(1, Latency + improvement.latency);
                PacketLoss = Math.Max(0, PacketLoss + improvement.packetLoss);
            }
        });
        
        IsOptimizationRunning = false;
        Status = "Optimization Complete";
        await _messengerService.PublishAsync(new StatusMessage("Network optimization completed", true));
    }
    
    private async Task StopOptimization()
    {
        IsOptimizationRunning = false;
        Status = "Optimization Stopped";
        await _messengerService.PublishAsync(new StatusMessage("Network optimization stopped", false));
    }
    
    private async Task SelectProfile(string profile)
    {
        SelectedProfile = profile;
        Status = $"Profile: {profile}";
        await _messengerService.PublishAsync(new StatusMessage($"Network profile set to {profile}", true));
    }
    
    public override async Task OnNavigatedTo(object? parameter = null)
    {
        await base.OnNavigatedTo(parameter);
        await _messengerService.PublishAsync(new SystemMessage("Navigation", "Network Optimization loaded", DateTime.Now));
    }
}