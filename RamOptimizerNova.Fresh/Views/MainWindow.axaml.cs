using Avalonia.Controls;
using Avalonia.Threading;
using RamOptimizerNova.Services;
using System;

namespace RamOptimizerNova.Views;

public partial class MainWindow : Window
{
    private readonly SystemMetricsService _metricsService;
    private readonly DispatcherTimer _timer;

    // Store references to UI elements
    private TextBlock? _cpuText;
    private TextBlock? _gpuText;
    private TextBlock? _memoryText;
    private TextBlock? _storageText;
    private TextBlock? _cpuPercent;
    private TextBlock? _gpuPercent;
    private TextBlock? _memoryPercent;
    private TextBlock? _storagePercent;

    public MainWindow()
    {
        InitializeComponent();
        
        _metricsService = new SystemMetricsService();
        
        // Find UI elements by name (we'll add names in XAML)
        this.Opened += (s, e) =>
        {
            _cpuText = this.FindControl<TextBlock>("CpuText");
            _gpuText = this.FindControl<TextBlock>("GpuText");
            _memoryText = this.FindControl<TextBlock>("MemoryText");
            _storageText = this.FindControl<TextBlock>("StorageText");
            _cpuPercent = this.FindControl<TextBlock>("CpuPercentText");
            _gpuPercent = this.FindControl<TextBlock>("GpuPercentText");
            _memoryPercent = this.FindControl<TextBlock>("MemoryPercentText");
            _storagePercent = this.FindControl<TextBlock>("StoragePercentText");
            
            // Initial update
            UpdateMetrics();
        };
        
        // Setup timer
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += (s, e) => UpdateMetrics();
        _timer.Start();
    }

    private void UpdateMetrics()
    {
        try
        {
            // Get metrics
            var cpu = _metricsService.GetCpuUsage();
            var gpu = _metricsService.GetGpuUsage();
            var memory = _metricsService.GetMemoryUsage();
            var storage = _metricsService.GetStorageInfo();
            
            // Update UI directly
            if (_cpuText != null) _cpuText.Text = cpu.ToString("F0");
            if (_gpuText != null) _gpuText.Text = gpu.ToString("F0");
            if (_memoryText != null) _memoryText.Text = memory.usedGB.ToString("F1");
            if (_storageText != null) _storageText.Text = storage.freeGB.ToString("F0");
            
            if (_cpuPercent != null) _cpuPercent.Text = $"{cpu:F0}% utilized";
            if (_gpuPercent != null) _gpuPercent.Text = $"{gpu:F0}% utilized";
            if (_memoryPercent != null) _memoryPercent.Text = $"{memory.percentage:F0}% utilized";
            if (_storagePercent != null) _storagePercent.Text = $"{storage.percentage:F0}% utilized";
            
            Console.WriteLine($"Updated: CPU={cpu:F0}%, Memory={memory.usedGB:F1}GB, Storage={storage.freeGB:F0}GB");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating metrics: {ex.Message}");
        }
    }
}