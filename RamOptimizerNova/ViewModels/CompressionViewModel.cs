using CommunityToolkit.Mvvm.Input;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.ViewModels;

public partial class CompressionViewModel : ViewModelBase
{
    private readonly IMessengerService _messengerService;
    
    [ObservableProperty]
    private string _title = "File Compression";
    
    [ObservableProperty]
    private string _status = "Ready";
    
    [ObservableProperty]
    private long _totalFiles = 0;
    
    [ObservableProperty]
    private long _compressedFiles = 0;
    
    [ObservableProperty]
    private long _totalSize = 0;
    
    [ObservableProperty]
    private long _compressedSize = 0;
    
    [ObservableProperty]
    private double _compressionRatio = 0;
    
    [ObservableProperty]
    private bool _isCompressionRunning = false;
    
    [ObservableProperty]
    private string _selectedAlgorithm = "LZ4";
    
    public IAsyncRelayCommand StartCompressionCommand { get; }
    public IAsyncRelayCommand StopCompressionCommand { get; }
    public IAsyncRelayCommand SelectAlgorithmCommand { get; }
    
    public CompressionViewModel(IMessengerService messengerService)
    {
        _messengerService = messengerService;
        Title = "File Compression";
        Status = "Ready";
        
        StartCompressionCommand = new AsyncRelayCommand(StartCompression);
        StopCompressionCommand = new AsyncRelayCommand(StopCompression);
        SelectAlgorithmCommand = new AsyncRelayCommand<string>(SelectAlgorithm);
        
        InitializeCompressionMetrics();
    }
    
    private void InitializeCompressionMetrics()
    {
        // Simulate compression metrics
        TotalFiles = 1000;
        CompressedFiles = 0;
        TotalSize = 1024 * 1024 * 1024; // 1GB
        CompressedSize = 0;
        CompressionRatio = 0;
    }
    
    private async Task StartCompression()
    {
        if (IsCompressionRunning)
            return;
            
        IsCompressionRunning = true;
        Status = "Compression Running...";
        
        await _messengerService.PublishAsync(new StatusMessage("File compression started", true));
        
        // Simulate compression process
        await Task.Run(async () =>
        {
            var algorithms = new[] { "LZ4", "Zstandard", "Gzip", "Brotli" };
            var compressionRates = new[] { 0.6, 0.4, 0.5, 0.3 }; // Different compression ratios
            
            for (int i = 0; i < TotalFiles; i += 50)
            {
                if (!IsCompressionRunning) break;
                
                await Task.Delay(200);
                CompressedFiles = Math.Min(TotalFiles, CompressedFiles + 50);
                
                // Simulate compression
                var filesProcessed = Math.Min(50, TotalFiles - CompressedFiles + 50);
                var originalSize = filesProcessed * (TotalSize / TotalFiles);
                var compressedSize = (long)(originalSize * compressionRates[Array.IndexOf(algorithms, SelectedAlgorithm)]);
                
                TotalSize += originalSize;
                CompressedSize += compressedSize;
                CompressionRatio = (double)CompressedSize / TotalSize;
            }
        });
        
        IsCompressionRunning = false;
        Status = "Compression Complete";
        await _messengerService.PublishAsync(new StatusMessage("File compression completed", true));
    }
    
    private async Task StopCompression()
    {
        IsCompressionRunning = false;
        Status = "Compression Stopped";
        await _messengerService.PublishAsync(new StatusMessage("File compression stopped", false));
    }
    
    private async Task SelectAlgorithm(string algorithm)
    {
        SelectedAlgorithm = algorithm;
        Status = $"Algorithm: {algorithm}";
        await _messengerService.PublishAsync(new StatusMessage($"Compression algorithm set to {algorithm}", true));
    }
    
    public override async Task OnNavigatedTo(object? parameter = null)
    {
        await base.OnNavigatedTo(parameter);
        await _messengerService.PublishAsync(new SystemMessage("Navigation", "File Compression loaded", DateTime.Now));
    }
}