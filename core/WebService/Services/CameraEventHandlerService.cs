using Lightview.Shared.Contracts.Events;
using Lightview.Shared.Contracts.SignalRModels;
using Microsoft.AspNetCore.SignalR;
using CameraManager.Interfaces;
using WebService.Hubs;
using WebService.Services.Events;

namespace WebService.Services;

/// <summary>
/// Service that processes RabbitMQ camera events and forwards them to SignalR clients with unified "CameraChanged" notifications
/// and updates camera status in persistence. Sequential processing is handled by separate RabbitMQ queues.
/// </summary>
public class CameraEventHandlerService : IDisposable
{
    private readonly CameraEventConsumer _eventConsumer;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHubContext<CameraHub> _hubContext;
    private readonly ILogger<CameraEventHandlerService> _logger;
    private bool _disposed;

    public CameraEventHandlerService(
        CameraEventConsumer eventConsumer,
        IServiceScopeFactory serviceScopeFactory,
        IHubContext<CameraHub> hubContext,
        ILogger<CameraEventHandlerService> logger)
    {
        _eventConsumer = eventConsumer;
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
        _logger = logger;

        // Subscribe to all camera events with async handlers
        _eventConsumer.CameraStatusChanged += OnCameraStatusChangedAsync;
        _eventConsumer.CameraError += OnCameraErrorAsync;
        _eventConsumer.PtzMoved += OnPtzMovedAsync;
        _eventConsumer.CameraStatistics += OnCameraStatisticsAsync;
        _eventConsumer.CameraMetadataUpdated += OnCameraMetadataUpdatedAsync;
        _eventConsumer.CameraSnapshotCaptured += OnCameraSnapshotCapturedAsync;

        _logger.LogInformation("Camera event handler service initialized with async processing");
    }

    // Async event handlers - sequential processing is guaranteed by separate RabbitMQ queues with prefetch=1
    private async Task OnCameraStatusChangedAsync(CameraStatusChangedEvent e)
    {
        try
        {
            _logger.LogDebug("Processing camera status changed event: Camera {CameraId} from {PreviousStatus} to {CurrentStatus}", 
                e.CameraId, e.PreviousStatus, e.CurrentStatus);

            // Update camera status in persistence only (don't update camera-controller as it's the source)
            using var scope = _serviceScopeFactory.CreateScope();
            var cameraService = scope.ServiceProvider.GetRequiredService<ICameraService>();
            await cameraService.UpdateCameraMetadataAsync(e.CameraId, status: e.CurrentStatus);

            // Forward to SignalR clients
            await _hubContext.Clients.All.SendAsync("CameraChanged", new CameraChangedNotification
            {
                CameraId = e.CameraId,
                EventType = CameraEventTypes.StatusChanged,
                Data = new CameraStatusChangedData
                {
                    PreviousStatus = e.PreviousStatus,
                    CurrentStatus = e.CurrentStatus,
                    Reason = e.Reason
                },
                Timestamp = e.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing camera status changed event for camera {CameraId}", e.CameraId);
            throw; // Re-throw to trigger message retry via RabbitMQ
        }
    }

    private async Task OnCameraErrorAsync(CameraErrorEvent e)
    {
        try
        {
            _logger.LogDebug("Processing camera error event: Camera {CameraId}, Error: {ErrorCode}", 
                e.CameraId, e.ErrorCode);

            // No persistence update needed - status will be updated via StatusChanged event from camera-controller
            // Just forward error details to clients
            await _hubContext.Clients.All.SendAsync("CameraChanged", new CameraChangedNotification
            {
                CameraId = e.CameraId,
                EventType = CameraEventTypes.Error,
                Data = new CameraErrorData
                {
                    ErrorCode = e.ErrorCode,
                    ErrorMessage = e.ErrorMessage,
                    Severity = e.Severity,
                    IsRecoverable = e.IsRecoverable
                },
                Timestamp = e.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing camera error event for camera {CameraId}", e.CameraId);
            throw; // Re-throw to trigger message retry via RabbitMQ
        }
    }

    private async Task OnPtzMovedAsync(PtzMovedEvent e)
    {
        try
        {
            _logger.LogDebug("Forwarding PTZ moved event: Camera {CameraId}", e.CameraId);

            await _hubContext.Clients.All.SendAsync("CameraChanged", new CameraChangedNotification
            {
                CameraId = e.CameraId,
                EventType = CameraEventTypes.PtzMoved,
                Data = new PtzMovedData
                {
                    PreviousPosition = e.PreviousPosition,
                    CurrentPosition = e.CurrentPosition,
                    MoveType = e.MoveType
                },
                Timestamp = e.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding PTZ moved event for camera {CameraId}", e.CameraId);
            throw; // Re-throw to trigger message retry via RabbitMQ
        }
    }

    private async Task OnCameraStatisticsAsync(CameraStatisticsEvent e)
    {
        try
        {
            _logger.LogDebug("Forwarding camera statistics event: Camera {CameraId}", e.CameraId);

            await _hubContext.Clients.All.SendAsync("CameraChanged", new CameraChangedNotification
            {
                CameraId = e.CameraId,
                EventType = CameraEventTypes.Statistics,
                Data = new CameraStatisticsData
                {
                    Uptime = e.Uptime,
                    BytesReceived = e.BytesReceived,
                    AverageFps = e.AverageFps,
                    DroppedFrames = e.DroppedFrames,
                    AverageLatency = e.AverageLatency
                },
                Timestamp = e.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding camera statistics event for camera {CameraId}", e.CameraId);
            throw; // Re-throw to trigger message retry via RabbitMQ
        }
    }

    private async Task OnCameraMetadataUpdatedAsync(CameraMetadataUpdatedEvent e)
    {
        try
        {
            _logger.LogDebug("Processing camera metadata updated event: Camera {CameraId}, UpdateType: {UpdateType}", e.CameraId, e.UpdateType);

            _logger.LogInformation("Payload: " +
                "Profiles webrtc: {Profiles}, " +
                "Capabilities: {Capabilities}, " +
                "DeviceInfo: {DeviceInfo}",
                e.Profiles != null ? string.Join(", ", e.Profiles.Select(p => p.WebRtcUri?.ToString() ?? "null")) : "null",
                e.Capabilities != null ? string.Join(", ", e.Capabilities) : "null",
                e.DeviceInfo != null ? e.DeviceInfo.Model : "null");

            // Update metadata in persistence based on what was updated
            using var scope = _serviceScopeFactory.CreateScope();
            var cameraService = scope.ServiceProvider.GetRequiredService<ICameraService>();
            
            if ((e.UpdateType & CameraMetadataUpdateType.Profiles) != 0 && e.Profiles != null)
            {
                await cameraService.UpdateCameraProfilesAsync(e.CameraId, e.Profiles);
            }
            if ((e.UpdateType & (CameraMetadataUpdateType.Capabilities | CameraMetadataUpdateType.DeviceInfo)) != 0)
            {
                await cameraService.UpdateCameraMetadataAsync(e.CameraId, 
                    capabilities: (e.UpdateType & CameraMetadataUpdateType.Capabilities) != 0 ? e.Capabilities : null,
                    deviceInfo: (e.UpdateType & CameraMetadataUpdateType.DeviceInfo) != 0 ? e.DeviceInfo : null);
            }

            // Forward to SignalR clients
            await _hubContext.Clients.All.SendAsync("CameraChanged", new CameraChangedNotification
            {
                CameraId = e.CameraId,
                EventType = CameraEventTypes.MetadataUpdated,
                Data = new CameraMetadataUpdatedData
                {
                    Profiles = e.Profiles,
                    Capabilities = e.Capabilities,
                    DeviceInfo = e.DeviceInfo,
                    UpdateType = e.UpdateType.ToString()
                },
                Timestamp = e.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing camera metadata updated event for camera {CameraId}", e.CameraId);
            throw; // Re-throw to trigger message retry via RabbitMQ
        }
    }

    private async Task OnCameraSnapshotCapturedAsync(CameraSnapshotCapturedEvent e)
    {
        try
        {
            _logger.LogDebug("Processing camera snapshot captured event: Camera {CameraId}", e.CameraId);

            // Download the latest snapshot from camera-controller and save to database
            using var scope = _serviceScopeFactory.CreateScope();
            var cameraService = scope.ServiceProvider.GetRequiredService<ICameraService>();
            var cameraControllerClient = scope.ServiceProvider.GetRequiredService<CameraControllerConnector.Interfaces.ICameraControllerClient>();

            try
            {
                // Get the latest snapshot from camera-controller
                var (imageData, capturedAt, profileToken) = await cameraControllerClient.GetLatestSnapshotAsync(e.CameraId);
                
                if (imageData != null && imageData.Length > 0)
                {
                    // Save the snapshot to database
                    await cameraService.SaveSnapshotAsync(e.CameraId, imageData, profileToken, capturedAt);
                    
                    _logger.LogInformation("Camera {CameraId} snapshot downloaded and saved: {FileSize} bytes, capture time: {CaptureTime}ms", 
                        e.CameraId, imageData.Length, e.CaptureTime.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("Camera {CameraId} snapshot event received but no image data available from camera-controller", e.CameraId);
                }
            }
            catch (Exception downloadEx)
            {
                _logger.LogError(downloadEx, "Failed to download and save snapshot for camera {CameraId}", e.CameraId);
                // Don't re-throw download errors - we still want to send the notification
            }

            // Send snapshot notification to SignalR clients (without the actual image data to keep it lightweight)
            await _hubContext.Clients.All.SendAsync("CameraSnapshotCaptured", new
            {
                CameraId = e.CameraId,
                Timestamp = e.Timestamp,
                CaptureTime = e.CaptureTime.TotalMilliseconds,
                ProfileToken = e.ProfileToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing camera snapshot captured event for camera {CameraId}", e.CameraId);
            throw; // Re-throw to trigger message retry via RabbitMQ
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            // Unsubscribe from events
            _eventConsumer.CameraStatusChanged -= OnCameraStatusChangedAsync;
            _eventConsumer.CameraError -= OnCameraErrorAsync;
            _eventConsumer.PtzMoved -= OnPtzMovedAsync;
            _eventConsumer.CameraStatistics -= OnCameraStatisticsAsync;
            _eventConsumer.CameraMetadataUpdated -= OnCameraMetadataUpdatedAsync;
            _eventConsumer.CameraSnapshotCaptured -= OnCameraSnapshotCapturedAsync;

            _logger.LogInformation("Camera event handler service disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing camera event handler service");
        }

        _disposed = true;
    }
}