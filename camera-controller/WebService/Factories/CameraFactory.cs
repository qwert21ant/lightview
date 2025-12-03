using CameraController.Contracts.Interfaces;
using Lightview.Shared.Contracts;
using RtspCamera;

namespace WebService.Factories;

/// <summary>
/// Camera factory for creating camera instances based on protocol
/// </summary>
public class CameraFactory : ICameraFactory
{
    private readonly ILogger<CameraFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public CameraFactory(ILogger<CameraFactory> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public ICamera CreateCamera(Camera configuration)
    {
        _logger.LogInformation("Creating camera instance for {CameraName} ({Protocol})", 
            configuration.Name, configuration.Protocol);

        // Create camera based on protocol
        switch (configuration.Protocol)
        {
            case CameraProtocol.Onvif:
                // return new OnvifCamera(configuration, _logger);
                throw new NotImplementedException($"ONVIF camera implementation not yet implemented");
            case CameraProtocol.Rtsp:
                var rtspLogger = _loggerFactory.CreateLogger<RtspCameraDevice>();
                return new RtspCameraDevice(configuration, rtspLogger);
        }

        throw new NotImplementedException($"ICamera implementation for protocol {configuration.Protocol} not yet implemented");
    }
}