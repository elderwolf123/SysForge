using Avalonia.Controls;
using RamOptimizerNova.ViewModels;

namespace RamOptimizerNova.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}