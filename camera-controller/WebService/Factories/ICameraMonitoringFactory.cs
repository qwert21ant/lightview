using CameraController.Contracts.Interfaces;
using Lightview.Shared.Contracts.InternalApi;

namespace WebService.Factories;

/// <summary>
/// Factory for creating camera monitoring instances - placeholder for future implementation
/// </summary>
public interface ICameraMonitoringFactory
{
    ICameraMonitoring CreateMonitoring(ICamera camera, CameraMonitoringConfig config);
}