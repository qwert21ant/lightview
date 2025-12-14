using Lightview.Shared.Contracts.Events;
using RabbitMQShared.Configuration;
using RabbitMQShared.Services;

namespace WebService.Services.Events;

/// <summary>
/// RabbitMQ-based implementation of camera event publisher
/// </summary>
public class CameraEventPublisher
{
    private readonly RabbitMQPublisher _publisher;
    private readonly ILogger<CameraEventPublisher> _logger;

    public CameraEventPublisher(RabbitMQPublisher publisher, ILogger<CameraEventPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishCameraStatusChangedAsync(CameraStatusChangedEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.StatusChanged, cancellationToken);
    }

    public async Task PublishCameraErrorAsync(CameraErrorEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.Error, cancellationToken);
    }

    public async Task PublishPtzMovedAsync(PtzMovedEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.PtzMoved, cancellationToken);
    }

    public async Task PublishCameraStatisticsAsync(CameraStatisticsEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.CameraStatistics, cancellationToken);
    }

    public async Task PublishCameraMetadataUpdatedAsync(CameraMetadataUpdatedEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing camera metadata profiles: {Profiles}", 
            cameraEvent.Profiles != null ? string.Join(", ", cameraEvent.Profiles.Select(p => p.WebRtcUri?.ToString() ?? "null")) : "null");
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.MetadataUpdated, cancellationToken);
    }

    public async Task PublishCameraSnapshotCapturedAsync(CameraSnapshotCapturedEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing camera snapshot captured event for camera {CameraId}: {ImageSize} bytes", 
            cameraEvent.CameraId, cameraEvent.ImageSize);
        await PublishCameraEventAsync(cameraEvent, CameraEventRoutingKeys.SnapshotCaptured, cancellationToken);
    }

    public async Task PublishCameraEventAsync<T>(T cameraEvent, CancellationToken cancellationToken = default) where T : CameraEventBase
    {
        await _publisher.DeclareExchangeAsync(ExchangesConfiguration.CameraEventsExchange, ExchangesConfiguration.ExchangeType, durable: true); // todo

        var routingKey = GetRoutingKeyForEventType<T>();
        await PublishCameraEventAsync(cameraEvent, routingKey, cancellationToken);
    }

    private async Task PublishCameraEventAsync<T>(T cameraEvent, string routingKey, CancellationToken cancellationToken = default) where T : CameraEventBase
    {
        // Use base shared publisher to publish message
        await _publisher.PublishMessageAsync(ExchangesConfiguration.CameraEventsExchange, routingKey, cameraEvent, persistent: true);

        _logger.LogDebug("Published {EventType} event for camera {CameraId} with routing key {RoutingKey}", 
            cameraEvent.EventType, cameraEvent.CameraId, routingKey);
    }

    private string GetRoutingKeyForEventType<T>() where T : CameraEventBase
    {
        return typeof(T).Name switch
        {
            nameof(CameraStatusChangedEvent) => CameraEventRoutingKeys.StatusChanged,
            nameof(CameraErrorEvent) => CameraEventRoutingKeys.Error,
            nameof(PtzMovedEvent) => CameraEventRoutingKeys.PtzMoved,
            nameof(CameraStatisticsEvent) => CameraEventRoutingKeys.CameraStatistics,
            nameof(CameraMetadataUpdatedEvent) => CameraEventRoutingKeys.MetadataUpdated,
            nameof(CameraSnapshotCapturedEvent) => CameraEventRoutingKeys.SnapshotCaptured,
            _ => "camera.unknown"
        };
    }
}