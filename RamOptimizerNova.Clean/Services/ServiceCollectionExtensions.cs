using Microsoft.Extensions.DependencyInjection;
using RamOptimizerNova.Services;

namespace RamOptimizerNova.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNovaServices(this IServiceCollection services)
    {
        // Register navigation service
        services.AddSingleton<INavigationService, NavigationService>();
        
        // Register messenger service
        services.AddSingleton<IMessengerService, MessengerService>();
        
        return services;
    }
    
    public static IServiceCollection AddNovaViewModels(this IServiceCollection services)
    {
        // ViewModels will be registered individually
        // This allows for constructor injection and better control over lifecycle
        
        return services;
    }
}