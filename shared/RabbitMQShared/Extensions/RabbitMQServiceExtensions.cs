using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQShared.Configuration;
using RabbitMQShared.Interfaces;
using RabbitMQShared.Services;

namespace RabbitMQShared.Extensions;

/// <summary>
/// Extension methods for adding RabbitMQ services to DI container
/// </summary>
public static class RabbitMQServiceExtensions
{
    /// <summary>
    /// Add RabbitMQ configuration and base services
    /// </summary>
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration
        services.Configure<RabbitMQConfiguration>(options =>
        {
            var section = configuration.GetSection(RabbitMQConfiguration.SectionName);
            section.Bind(options);
        });
        
        return services;
    }
    
    /// <summary>
    /// Add RabbitMQ publisher service
    /// </summary>
    public static IServiceCollection AddRabbitMQPublisher(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMQPublisher>();
        services.AddSingleton<IInitializable>(provider => provider.GetRequiredService<RabbitMQPublisher>());
        return services;
    }
    
    /// <summary>
    /// Add a custom RabbitMQ consumer service that implements BaseRabbitMQConsumer
    /// </summary>
    public static IServiceCollection AddRabbitMQConsumer<TConsumer>(this IServiceCollection services)
        where TConsumer : BaseRabbitMQConsumer
    {
        services.AddSingleton<TConsumer>();
        services.AddSingleton<IInitializable>(provider => provider.GetRequiredService<TConsumer>());
        return services;
    }
}