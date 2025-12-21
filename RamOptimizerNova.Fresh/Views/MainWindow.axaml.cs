using Avalonia.Controls;
using RamOptimizerNova.ViewModels;
using System;

namespace RamOptimizerNova.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        var viewModel = new DashboardViewModel();
        DataContext = viewModel;
        
        Console.WriteLine("MainWindow: DataContext set to DashboardViewModel");
        Console.WriteLine($"MainWindow: ViewModel CPU = {viewModel.CpuUsage}");
        Console.WriteLine($"MainWindow: ViewModel TestMessage = {viewModel.TestMessage}");
    }
}