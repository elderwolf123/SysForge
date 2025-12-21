using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly SystemMetricsService _metricsService;
    private readonly DispatcherTimer _refreshTimer;

    private float _cpuUsage = 50; // Default values to show something
    private float _gpuUsage = 35;
    private float _memoryUsedGB = 8;
    private float _memoryTotalGB = 16;
    private float _memoryPercentage = 50;
    private float _storageFreeGB = 200;
    private float _storageTotalGB = 500;
    private float _storagePercentage = 60;

    public float CpuUsage
    {
        get => _cpuUsage;
        set { _cpuUsage = value; OnPropertyChanged(); }
    }

    public float GpuUsage
    {
        get => _gpuUsage;
        set { _gpuUsage = value; OnPropertyChanged(); }
    }

    public float MemoryUsedGB
    {
        get => _memoryUsedGB;
        set { _memoryUsedGB = value; OnPropertyChanged(); }
    }

    public float MemoryTotalGB
    {
        get => _memoryTotalGB;
        set { _memoryTotalGB = value; OnPropertyChanged(); }
    }

    public float MemoryPercentage
    {
        get => _memoryPercentage;
        set { _memoryPercentage = value; OnPropertyChanged(); }
    }

    public float StorageFreeGB
    {
        get => _storageFreeGB;
        set { _storageFreeGB = value; OnPropertyChanged(); }
    }

    public float StorageTotalGB
    {
        get => _storageTotalGB;
        set { _storageTotalGB = value; OnPropertyChanged(); }
    }

    public float StoragePercentage
    {
        get => _storagePercentage;
        set { _storagePercentage = value; OnPropertyChanged(); }
    }

    public DashboardViewModel()
    {
        Console.WriteLine("DashboardViewModel: Constructor started");
        _metricsService = new SystemMetricsService();

        // Create timer for auto-refresh every 2 seconds
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _refreshTimer.Tick += async (s, e) => await UpdateMetricsAsync();

        // Initial update - run synchronously to ensure values are set
        Task.Run(async () =>
        {
            await Task.Delay(500); // Small delay
            await Dispatcher.UIThread.InvokeAsync(async () => await InitializeAsync());
        });
        
        Console.WriteLine($"DashboardViewModel: Initialized with CPU={CpuUsage}");
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("DashboardViewModel: InitializeAsync started");
        await UpdateMetricsAsync();
        _refreshTimer.Start();
        Console.WriteLine("DashboardViewModel: Timer started");
    }

    private async Task UpdateMetricsAsync()
    {
        try
        {
            Console.WriteLine("UpdateMetricsAsync: Starting update...");
            
            // Get CPU usage
            CpuUsage = _metricsService.GetCpuUsage();
            Console.WriteLine($"CPU: {CpuUsage}");

            // Get GPU usage
            GpuUsage = _metricsService.GetGpuUsage();
            Console.WriteLine($"GPU: {GpuUsage}");

            // Get Memory usage
            var memory = _metricsService.GetMemoryUsage();
            MemoryUsedGB = memory.usedGB;
            MemoryTotalGB = memory.totalGB;
            MemoryPercentage = memory.percentage;
            Console.WriteLine($"Memory: {MemoryUsedGB}/{MemoryTotalGB} GB ({MemoryPercentage}%)");

            // Get Storage info
            var storage = _metricsService.GetStorageInfo();
            StorageFreeGB = storage.freeGB;
            StorageTotalGB = storage.totalGB;
            StoragePercentage = storage.percentage;
            Console.WriteLine($"Storage: {StorageFreeGB}/{StorageTotalGB} GB ({StoragePercentage}%)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in UpdateMetricsAsync: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _refreshTimer?.Stop();
        _metricsService?.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        Console.WriteLine($"PropertyChanged: {propertyName} = {GetType().GetProperty(propertyName)?.GetValue(this)}");
    }
}
