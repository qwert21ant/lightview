using CameraController.Contracts.Interfaces;
using Lightview.Shared.Contracts.Settings;
using RtspCamera;

namespace WebService.Factories;

/// <summary>
/// Placeholder monitoring factory - will be implemented when ICameraMonitoring implementations are ready
/// </summary>
public class CameraMonitoringFactory : ICameraMonitoringFactory
{
    private readonly ISettingsService _settingsProvider;
    private readonly ILogger<CameraMonitoringFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public CameraMonitoringFactory(
        ISettingsService settingsProvider,
        ILogger<CameraMonitoringFactory> logger,
        ILoggerFactory loggerFactory)
    {
        _settingsProvider = settingsProvider;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public ICameraMonitoring CreateMonitoring(ICamera camera)
    {
        _logger.LogInformation("Creating monitoring instance for camera {CameraId}", camera.Id);

        // For now, use RtspCameraMonitoring for all cameras
        // In the future, this could be based on camera type or protocol
        var monitoringLogger = _loggerFactory.CreateLogger<RtspCameraMonitoring>();
        return new RtspCameraMonitoring(camera, _settingsProvider, monitoringLogger);
    }
}