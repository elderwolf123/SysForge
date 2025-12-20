using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RamOptimizerNova.Services;
using RamOptimizerNova.ViewModels;
using RamOptimizerNova.Views;

namespace RamOptimizerNova;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            // Set up dependency injection
            SetupServices();
            
            // Create main window with DI
            desktop.MainWindow = new MainWindow
            {
                DataContext = _serviceProvider!.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupServices()
    {
        var services = new ServiceCollection();
        
        // Add Nova services
        services.AddNovaServices();
        
        // Add ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<MemoryOptimizationViewModel>();
        services.AddSingleton<CPUOptimizationViewModel>();
        services.AddSingleton<CompressionViewModel>();
        services.AddSingleton<StorageOptimizationViewModel>();
        services.AddSingleton<NetworkOptimizationViewModel>();
        
        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}