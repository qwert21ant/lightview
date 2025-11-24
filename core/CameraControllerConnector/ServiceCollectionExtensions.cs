using CameraControllerConnector.Interfaces;
using CameraControllerConnector.Models;
using CameraControllerConnector.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace CameraControllerConnector;

/// <summary>
/// Extension methods for registering CameraControllerConnector services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add camera controller client services to the service collection
    /// </summary>
    public static IServiceCollection AddCameraControllerClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var config = configuration.GetSection("CameraController").Get<CameraControllerConfiguration>()
            ?? throw new InvalidOperationException("CameraController configuration is required");

        services.AddSingleton(config);

        // Register HTTP client with typed client
        var clientBuilder = services.AddHttpClient<ICameraControllerClient, CameraControllerClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(config.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            });

        // Add retry policy if enabled
        if (config.EnableRetry)
        {
            clientBuilder.AddTransientHttpErrorPolicy(policyBuilder =>
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        config.MaxRetryAttempts,
                        retryAttempt => TimeSpan.FromMilliseconds(config.RetryDelayMs * retryAttempt)));
        }

        return services;
    }

    /// <summary>
    /// Add camera controller client services with custom configuration
    /// </summary>
    public static IServiceCollection AddCameraControllerClient(
        this IServiceCollection services,
        Action<CameraControllerConfiguration> configureOptions)
    {
        var config = new CameraControllerConfiguration { BaseUrl = "http://localhost:5002" };
        configureOptions(config);

        services.AddSingleton(config);

        // Register HTTP client with typed client
        var clientBuilder = services.AddHttpClient<ICameraControllerClient, CameraControllerClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(config.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            });

        // Add retry policy if enabled
        if (config.EnableRetry)
        {
            clientBuilder.AddTransientHttpErrorPolicy(policyBuilder =>
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        config.MaxRetryAttempts,
                        retryAttempt => TimeSpan.FromMilliseconds(config.RetryDelayMs * retryAttempt)));
        }

        return services;
    }
}
