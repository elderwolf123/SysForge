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

    private float _cpuUsage;
    private float _gpuUsage;
    private float _memoryUsedGB;
    private float _memoryTotalGB;
    private float _memoryPercentage;
    private float _storageFreeGB;
    private float _storageTotalGB;
    private float _storagePercentage;

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
        _metricsService = new SystemMetricsService();

        // Create timer for auto-refresh every 2 seconds
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _refreshTimer.Tick += async (s, e) => await UpdateMetricsAsync();

        // Initial update
        Task.Run(async () => await InitializeAsync());
    }

    public async Task InitializeAsync()
    {
        await UpdateMetricsAsync();
        _refreshTimer.Start();
    }

    private async Task UpdateMetricsAsync()
    {
        // Get CPU usage
        await Task.Delay(100); // Small delay for CPU counter
        CpuUsage = _metricsService.GetCpuUsage();

        // Get GPU usage
        GpuUsage = _metricsService.GetGpuUsage();

        // Get Memory usage
        var memory = _metricsService.GetMemoryUsage();
        MemoryUsedGB = memory.usedGB;
        MemoryTotalGB = memory.totalGB;
        MemoryPercentage = memory.percentage;

        // Get Storage info
        var storage = _metricsService.GetStorageInfo();
        StorageFreeGB = storage.freeGB;
        StorageTotalGB = storage.totalGB;
        StoragePercentage = storage.percentage;
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
    }
}
