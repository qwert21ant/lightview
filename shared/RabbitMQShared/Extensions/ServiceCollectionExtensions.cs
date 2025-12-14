using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQShared.Interfaces;

namespace RabbitMQShared.Extensions;

/// <summary>
/// Extensions for service collection to handle IInitializable services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Initialize all IInitializable services before application startup.
    /// This method should be called after building the service provider but before running the app.
    /// </summary>
    /// <param name="serviceProvider">The service provider containing IInitializable services</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the initialization operation</returns>
    public static async Task InitializeMandatoryServicesAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("MandatoryServicesInitializer");
        
        logger.LogInformation("Starting mandatory service initialization...");
        
        try
        {
            // Get all services that implement IInitializable
            var initializableServices = serviceProvider.GetServices<IInitializable>().ToList();
            
            if (!initializableServices.Any())
            {
                logger.LogInformation("No IInitializable services found");
                return;
            }
            
            logger.LogInformation("Found {Count} services requiring initialization: {Services}", 
                initializableServices.Count, 
                string.Join(", ", initializableServices.Select(s => s.ServiceName)));
            
            // Initialize all services
            foreach (var service in initializableServices)
            {
                logger.LogInformation("Initializing service: {ServiceName}", service.ServiceName);
                
                try
                {
                    await service.InitializeAsync(cancellationToken);
                    logger.LogInformation("Successfully initialized service: {ServiceName}", service.ServiceName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize mandatory service: {ServiceName}", service.ServiceName);
                    
                    throw new InvalidOperationException(
                        $"Mandatory service '{service.ServiceName}' failed to initialize. Application cannot start.", ex);
                }
            }
            
            logger.LogInformation("All mandatory services initialized successfully");
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            logger.LogCritical(ex, "Critical error during service initialization");
            throw;
        }
    }

    /// <summary>
    /// Registers a singleton service that implements IInitializable.
    /// </summary>
    public static IServiceCollection AddInitializableSingleton<TImplementation>(this IServiceCollection services)
        where TImplementation : class, IInitializable
    {
        services.AddSingleton<TImplementation>();
        services.AddSingleton<IInitializable>(provider => provider.GetRequiredService<TImplementation>());
        return services;
    }
}