using CommunityToolkit.Mvvm.Input;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.ViewModels;

public partial class StorageOptimizationViewModel : ViewModelBase
{
    private readonly IMessengerService _messengerService;
    
    [ObservableProperty]
    private string _title = "Storage Optimization";
    
    [ObservableProperty]
    private string _status = "Ready";
    
    [ObservableProperty]
    private long _totalSpace = 0;
    
    [ObservableProperty]
    private long _usedSpace = 0;
    
    [ObservableProperty]
    private long _freeSpace = 0;
    
    [ObservableProperty]
    private int _tempFilesCleaned = 0;
    
    [ObservableProperty]
    private int _duplicatesFound = 0;
    
    [ObservableProperty]
    private bool _isOptimizationRunning = false;
    
    [ObservableProperty]
    private double _spaceReclaimed = 0;
    
    public IAsyncRelayCommand StartOptimizationCommand { get; }
    public IAsyncRelayCommand StopOptimizationCommand { get; }
    public IAsyncRelayCommand CleanTempFilesCommand { get; }
    public IAsyncRelayCommand FindDuplicatesCommand { get; }
    
    public StorageOptimizationViewModel(IMessengerService messengerService)
    {
        _messengerService = messengerService;
        Title = "Storage Optimization";
        Status = "Ready";
        
        StartOptimizationCommand = new AsyncRelayCommand(StartOptimization);
        StopOptimizationCommand = new AsyncRelayCommand(StopOptimization);
        CleanTempFilesCommand = new AsyncRelayCommand(CleanTempFiles);
        FindDuplicatesCommand = new AsyncRelayCommand(FindDuplicates);
        
        InitializeStorageMetrics();
    }
    
    private void InitializeStorageMetrics()
    {
        // Simulate storage metrics
        TotalSpace = 512L * 1024 * 1024 * 1024; // 512GB
        UsedSpace = 350L * 1024 * 1024 * 1024; // 350GB
        FreeSpace = TotalSpace - UsedSpace;
        SpaceReclaimed = 0;
    }
    
    private async Task StartOptimization()
    {
        if (IsOptimizationRunning)
            return;
            
        IsOptimizationRunning = true;
        Status = "Storage Optimization Running...";
        
        await _messengerService.PublishAsync(new StatusMessage("Storage optimization started", true));
        
        // Run both optimizations
        await Task.WhenAll(
            CleanTempFiles(),
            FindDuplicates()
        );
        
        IsOptimizationRunning = false;
        Status = "Optimization Complete";
        await _messengerService.PublishAsync(new StatusMessage("Storage optimization completed", true));
    }
    
    private async Task StopOptimization()
    {
        IsOptimizationRunning = false;
        Status = "Optimization Stopped";
        await _messengerService.PublishAsync(new StatusMessage("Storage optimization stopped", false));
    }
    
    private async Task CleanTempFiles()
    {
        Status = "Cleaning temporary files...";
        
        await Task.Run(async () =>
        {
            var tempFilesToClean = 500;
            for (int i = 0; i < tempFilesToClean; i += 25)
            {
                if (!IsOptimizationRunning) break;
                
                await Task.Delay(100);
                TempFilesCleaned = Math.Min(tempFilesToClean, TempFilesCleaned + 25);
                
                // Simulate space reclaimed
                var spaceReclaimedThisBatch = 25L * 1024 * 1024; // 25MB per batch
                SpaceReclaimed += spaceReclaimedThisBatch;
                UsedSpace = Math.Max(0, UsedSpace - spaceReclaimedThisBatch);
                FreeSpace = TotalSpace - UsedSpace;
            }
        });
        
        Status = "Temporary files cleaned";
    }
    
    private async Task FindDuplicates()
    {
        Status = "Finding duplicate files...";
        
        await Task.Run(async () =>
        {
            var duplicatesToFind = 200;
            for (int i = 0; i < duplicatesToFind; i += 10)
            {
                if (!IsOptimizationRunning) break;
                
                await Task.Delay(150);
                DuplicatesFound = Math.Min(duplicatesToFind, DuplicatesFound + 10);
                
                // Simulate space reclaimed from duplicates
                var spaceReclaimedThisBatch = 10L * 1024 * 1024; // 10MB per batch
                SpaceReclaimed += spaceReclaimedThisBatch;
                UsedSpace = Math.Max(0, UsedSpace - spaceReclaimedThisBatch);
                FreeSpace = TotalSpace - UsedSpace;
            }
        });
        
        Status = "Duplicate files found";
    }
    
    public override async Task OnNavigatedTo(object? parameter = null)
    {
        await base.OnNavigatedTo(parameter);
        await _messengerService.PublishAsync(new SystemMessage("Navigation", "Storage Optimization loaded", DateTime.Now));
    }
}