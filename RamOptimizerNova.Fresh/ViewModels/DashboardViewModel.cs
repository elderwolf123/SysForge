using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    // Simple test property
    public string TestMessage { get; set; } = "ViewModel is WORKING!";
    
    // Metric properties with initial values
    public float CpuUsage { get; set; } = 50f;
    public float GpuUsage { get; set; } = 35f;
    public float MemoryUsedGB { get; set; } = 8.0f;
    public float MemoryTotalGB { get; set; } = 16f;
    public float MemoryPercentage { get; set; } = 50f;
    public float StorageFreeGB { get; set; } = 200f;
    public float StorageTotalGB { get; set; } = 500f;
    public float StoragePercentage { get; set; } = 60f;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DashboardViewModel()
    {
        Console.WriteLine("DashboardViewModel created!");
        Console.WriteLine($"CPU: {CpuUsage}, Memory: {MemoryUsedGB}");
    }
}
