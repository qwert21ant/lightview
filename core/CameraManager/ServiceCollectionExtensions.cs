using CameraManager.Interfaces;
using CameraManager.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CameraManager;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCameraManager(this IServiceCollection services)
    {
        services.AddScoped<ICameraService, CameraService>();
        return services;
    }
}