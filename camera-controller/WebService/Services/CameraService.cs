using System.Collections.Concurrent;
using Lightview.Shared.Contracts;
using CameraController.Contracts.Interfaces;
using CameraController.Contracts.Models;
using WebService.Factories;
using Lightview.Shared.Contracts.InternalApi;

namespace WebService.Services;

/// <summary>
/// Main camera management service that manages camera connections and monitoring
/// </summary>
public class CameraService : ICameraService, IDisposable
{
    private readonly ILogger<CameraService> _logger;
    private readonly IEventPublisherService _eventPublisher;
    private readonly IMediaMtxService _mediaMtxService;
    private readonly ConcurrentDictionary<Guid, ICameraMonitoring> _managedCameras;
    private readonly SemaphoreSlim _operationSemaphore;
    private bool _disposed;

    public IReadOnlyDictionary<Guid, ICameraMonitoring> GetAllCameras()
    {
        return _managedCameras.AsReadOnly();
    }

    public ICameraMonitoring? GetCamera(Guid cameraId)
    {
        return _managedCameras.TryGetValue(cameraId, out var camera) ? camera : null;
    }

    public async Task<ICameraMonitoring> AddCameraAsync(Camera cameraConfig, CameraMonitoringConfig? monitoringConfig = null)
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            if (_managedCameras.ContainsKey(cameraConfig.Id))
            {
                throw new InvalidOperationException($"Camera with ID {cameraConfig.Id} already exists");
            }

            _logger.LogInformation("Adding camera {CameraName} ({CameraId}) at {Url}", 
                cameraConfig.Name, cameraConfig.Id, cameraConfig.Url);

            // Create camera instance (this would be injected in real implementation)
            var camera = CreateCameraInstance(cameraConfig);
            
            // Create monitoring wrapper with configuration
            var monitoring = CreateMonitoringInstance(camera, monitoringConfig ?? new CameraMonitoringConfig());
            
            // Subscribe to camera events
            SubscribeToCameraEvents(camera);
            SubscribeToMonitoringEvents(monitoring);

            // Add to managed cameras
            _managedCameras[cameraConfig.Id] = monitoring;

            // Set initial status to Disabled - monitoring will start only when camera is connected
            cameraConfig.Status = CameraStatus.Disabled;

            // Publish camera added event
            await _eventPublisher.PublishCameraEventAsync(new CameraStatusChangedEvent
            {
                CameraId = cameraConfig.Id,
                PreviousStatus = CameraStatus.Disabled,
                CurrentStatus = CameraStatus.Disabled,
                Reason = "Camera added to management - monitoring disabled until connection"
            });

            // Configure MediaMTX stream if camera has RTSP capabilities
            await ConfigureMediaMtxStreamAsync(camera, cameraConfig);

            // Do not start monitoring automatically - it will start when camera connects

            _logger.LogInformation("Successfully added and started monitoring camera {CameraId}", cameraConfig.Id);
            return monitoring;
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public async Task<bool> RemoveCameraAsync(Guid cameraId)
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            if (!_managedCameras.TryRemove(cameraId, out var monitoring))
            {
                _logger.LogWarning("Attempted to remove non-existent camera {CameraId}", cameraId);
                return false;
            }

            _logger.LogInformation("Removing camera {CameraId}", cameraId);

            // Remove MediaMTX stream configuration
            await RemoveMediaMtxStreamAsync(cameraId);

            // Stop monitoring and dispose
            await monitoring.StopMonitoringAsync();
            monitoring.Dispose();

            // Publish camera removed event
            await _eventPublisher.PublishCameraEventAsync(new CameraDisconnectedEvent
            {
                CameraId = cameraId,
                Reason = "Camera removed from management",
                IsExpected = true,
                LastSeenAt = DateTime.UtcNow
            });

            _logger.LogInformation("Successfully removed camera {CameraId}", cameraId);
            return true;
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public async Task<bool> UpdateCameraAsync(Guid cameraId, Camera updatedConfig)
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            if (!_managedCameras.TryGetValue(cameraId, out var monitoring))
            {
                _logger.LogWarning("Attempted to update non-existent camera {CameraId}", cameraId);
                return false;
            }

            _logger.LogInformation("Updating configuration for camera {CameraId}", cameraId);

            // Stop current monitoring
            await monitoring.StopMonitoringAsync();

            // Update camera configuration (this would require ICamera to support config updates)
            // For now, we'll need to recreate the camera instance
            monitoring.Dispose();
            _managedCameras.TryRemove(cameraId, out _);

            // Add with updated configuration
            await AddCameraAsync(updatedConfig);

            _logger.LogInformation("Successfully updated camera {CameraId}", cameraId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update camera {CameraId}", cameraId);
            return false;
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public async Task<CameraHealthStatus?> GetCameraHealthAsync(Guid cameraId)
    {
        if (!_managedCameras.TryGetValue(cameraId, out var monitoring))
        {
            return null;
        }

        try
        {
            return await monitoring.CheckHealthAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check health for camera {CameraId}", cameraId);
            return new CameraHealthStatus
            {
                IsHealthy = false,
                CheckedAt = DateTime.UtcNow,
                Issues = new List<string> { "Health check failed: " + ex.Message },
                LastError = ex
            };
        }
    }

    public async Task<ServiceHealthSummary> GetServiceHealthAsync()
    {
        var summary = new ServiceHealthSummary
        {
            TotalCameras = _managedCameras.Count
        };

        var healthTasks = _managedCameras.Values.Select(async monitoring =>
        {
            try
            {
                var health = await monitoring.CheckHealthAsync();
                var camera = monitoring.Camera;
                
                return new { CameraId = camera.Id, Status = camera.Status, Health = health };
            }
            catch
            {
                return new { CameraId = monitoring.Camera.Id, Status = CameraStatus.Error, Health = new CameraHealthStatus { IsHealthy = false } };
            }
        });

        var results = await Task.WhenAll(healthTasks);

        foreach (var result in results)
        {
            switch (result.Status)
            {
                case CameraStatus.Online:
                    if (result.Health?.IsHealthy == true)
                        summary.HealthyCameras++;
                    else
                    {
                        summary.UnhealthyCameras++;
                        summary.ProblematicCameras.Add(result.CameraId);
                    }
                    break;
                case CameraStatus.Disabled:
                    // Disabled cameras are not counted as offline - they're intentionally not monitored
                    break;
                case CameraStatus.Offline:
                    summary.OfflineCameras++;
                    break;
                case CameraStatus.Connecting:
                    summary.ConnectingCameras++;
                    break;
                case CameraStatus.Error:
                    summary.ErrorCameras++;
                    summary.ProblematicCameras.Add(result.CameraId);
                    break;
            }
        }

        return summary;
    }

    private readonly ICameraFactory _cameraFactory;
    private readonly ICameraMonitoringFactory _monitoringFactory;

    public CameraService(
        ILogger<CameraService> logger, 
        IEventPublisherService eventPublisher,
        IMediaMtxService mediaMtxService,
        ICameraFactory cameraFactory,
        ICameraMonitoringFactory monitoringFactory)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
        _mediaMtxService = mediaMtxService;
        _cameraFactory = cameraFactory;
        _monitoringFactory = monitoringFactory;
        _managedCameras = new ConcurrentDictionary<Guid, ICameraMonitoring>();
        _operationSemaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Create camera instance using factory
    /// </summary>
    private ICamera CreateCameraInstance(Camera config)
    {
        return _cameraFactory.CreateCamera(config);
    }

    /// <summary>
    /// Create monitoring instance using factory
    /// </summary>
    private ICameraMonitoring CreateMonitoringInstance(ICamera camera, CameraMonitoringConfig config)
    {
        return _monitoringFactory.CreateMonitoring(camera, config);
    }

    private void SubscribeToCameraEvents(ICamera camera)
    {
        camera.StatusChanged += async (sender, args) =>
        {
            await _eventPublisher.PublishCameraEventAsync(new CameraStatusChangedEvent
            {
                CameraId = camera.Id,
                PreviousStatus = args.PreviousStatus,
                CurrentStatus = args.CurrentStatus,
                Reason = args.Reason
            });
        };

        // Subscribe to PTZ events if PTZ is supported
        if (camera.PtzControl != null)
        {
            camera.PtzControl.PositionChanged += async (sender, args) =>
            {
                await _eventPublisher.PublishCameraEventAsync(new PtzMovedEvent
                {
                    CameraId = camera.Id,
                    PreviousPosition = args.PreviousPosition,
                    CurrentPosition = args.CurrentPosition,
                    MoveType = args.MoveType
                });
            };
        }
    }

    private void SubscribeToMonitoringEvents(ICameraMonitoring monitoring)
    {
        monitoring.HealthChanged += async (sender, args) =>
        {
            if (!args.CurrentHealth.IsHealthy && args.PreviousHealth.IsHealthy)
            {
                // Camera became unhealthy
                await _eventPublisher.PublishCameraEventAsync(new CameraErrorEvent
                {
                    CameraId = args.CameraId,
                    ErrorCode = "HEALTH_CHECK_FAILED",
                    ErrorMessage = string.Join(", ", args.CurrentHealth.Issues),
                    Severity = ErrorSeverity.Warning,
                    IsRecoverable = true
                });
            }
            else if (args.CurrentHealth.IsHealthy && !args.PreviousHealth.IsHealthy)
            {
                // Camera became healthy again
                await _eventPublisher.PublishCameraEventAsync(new CameraStatusChangedEvent
                {
                    CameraId = args.CameraId,
                    PreviousStatus = CameraStatus.Error,
                    CurrentStatus = CameraStatus.Online,
                    Reason = "Health check recovered"
                });
            }
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing CameraService with {Count} managed cameras", _managedCameras.Count);

        // Stop and dispose all monitoring instances
        var disposeTasks = _managedCameras.Values.Select(async monitoring =>
        {
            try
            {
                await monitoring.StopMonitoringAsync();
                monitoring.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing camera monitoring for {CameraId}", monitoring.Camera.Id);
            }
        });

        Task.WaitAll(disposeTasks.ToArray(), TimeSpan.FromSeconds(30));

        _managedCameras.Clear();
        _operationSemaphore.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Configure MediaMTX stream for a camera
    /// </summary>
    private async Task ConfigureMediaMtxStreamAsync(ICamera camera, Camera cameraConfig)
    {
        try
        {
            // Only configure MediaMTX for cameras that provide RTSP streams
            if (cameraConfig.Protocol == CameraProtocol.Rtsp || cameraConfig.Protocol == CameraProtocol.Onvif)
            {
                var streamUri = await camera.GetStreamUriAsync("main");
                if (streamUri != null)
                {
                    var streamPath = await _mediaMtxService.ConfigureRtspInputAsync(cameraConfig, streamUri.ToString());
                    _logger.LogInformation("Configured MediaMTX stream for camera {CameraId} at path {StreamPath}", 
                        cameraConfig.Id, streamPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure MediaMTX stream for camera {CameraId}", cameraConfig.Id);
            // Don't throw - camera can still work without MediaMTX integration
        }
    }

    /// <summary>
    /// Remove MediaMTX stream configuration for a camera
    /// </summary>
    private async Task RemoveMediaMtxStreamAsync(Guid cameraId)
    {
        try
        {
            await _mediaMtxService.RemoveStreamConfigurationAsync(cameraId);
            _logger.LogInformation("Removed MediaMTX stream configuration for camera {CameraId}", cameraId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove MediaMTX stream configuration for camera {CameraId}", cameraId);
            // Don't throw - this is cleanup, shouldn't prevent camera removal
        }
    }

    public async Task<bool> ConnectCameraAsync(Guid cameraId)
    {
        _logger.LogInformation("Attempting to connect camera {CameraId}", cameraId);
        
        var monitoring = GetCamera(cameraId);
        if (monitoring == null)
        {
            _logger.LogWarning("Cannot connect camera {CameraId}: Camera not found", cameraId);
            return false;
        }

        if (monitoring.Camera.Status == CameraStatus.Online)
        {
            _logger.LogInformation("Camera {CameraId} is already connected", cameraId);
            return true;
        }

        try
        {
            var connected = await monitoring.Camera.ConnectAsync();
            if (connected)
            {
                _logger.LogInformation("Successfully connected to camera {CameraId}", cameraId);
                
                // Start monitoring now that camera is connected
                await monitoring.StartMonitoringAsync();
                _logger.LogInformation("Started monitoring for camera {CameraId}", cameraId);
                
                // Publish connection event
                await _eventPublisher.PublishCameraEventAsync(new CameraStatusChangedEvent
                {
                    CameraId = cameraId,
                    PreviousStatus = monitoring.Camera.Status == CameraStatus.Disabled ? CameraStatus.Disabled : CameraStatus.Offline,
                    CurrentStatus = CameraStatus.Online,
                    Reason = "Manual connection request"
                });
            }
            else
            {
                _logger.LogWarning("Failed to connect to camera {CameraId}: Connection attempt returned false", cameraId);
            }
            
            return connected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to camera {CameraId}", cameraId);
            return false;
        }
    }

    public async Task<bool> DisconnectCameraAsync(Guid cameraId)
    {
        _logger.LogInformation("Attempting to disconnect camera {CameraId}", cameraId);
        
        var monitoring = GetCamera(cameraId);
        if (monitoring == null)
        {
            _logger.LogWarning("Cannot disconnect camera {CameraId}: Camera not found", cameraId);
            return false;
        }

        if (monitoring.Camera.Status == CameraStatus.Offline)
        {
            _logger.LogInformation("Camera {CameraId} is already disconnected", cameraId);
            return true;
        }

        try
        {
            // Stop monitoring first
            await monitoring.StopMonitoringAsync();
            _logger.LogInformation("Stopped monitoring for camera {CameraId}", cameraId);
            
            await monitoring.Camera.DisconnectAsync();
            _logger.LogInformation("Successfully disconnected from camera {CameraId}", cameraId);
            
            // Publish disconnection event - camera goes to Disabled state
            await _eventPublisher.PublishCameraEventAsync(new CameraStatusChangedEvent
            {
                CameraId = cameraId,
                PreviousStatus = CameraStatus.Online,
                CurrentStatus = CameraStatus.Disabled,
                Reason = "Manual disconnection request - monitoring disabled"
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from camera {CameraId}", cameraId);
            return false;
        }
    }
}