using Lightview.Shared.Contracts.Events;

namespace Lightview.Shared.Contracts.Interfaces;

/// <summary>
/// Interface for publishing camera events to message broker
/// </summary>
public interface ICameraEventPublisher
{
    /// <summary>
    /// Publish a camera status changed event
    /// </summary>
    Task PublishCameraStatusChangedAsync(CameraStatusChangedEvent cameraEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish a camera error event
    /// </summary>
    Task PublishCameraErrorAsync(CameraErrorEvent cameraEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish a PTZ moved event
    /// </summary>
    Task PublishPtzMovedAsync(PtzMovedEvent cameraEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish camera statistics event
    /// </summary>
    Task PublishCameraStatisticsAsync(CameraStatisticsEvent cameraEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish camera metadata updated event
    /// </summary>
    Task PublishCameraMetadataUpdatedAsync(CameraMetadataUpdatedEvent cameraEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish any camera event
    /// </summary>
    Task PublishCameraEventAsync<T>(T cameraEvent, CancellationToken cancellationToken = default) where T : CameraEventBase;
}

/// <summary>
/// Interface for consuming camera events from message broker
/// </summary>
public interface ICameraEventConsumer
{
    /// <summary>
    /// Start consuming camera events
    /// </summary>
    Task StartConsumingAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop consuming camera events
    /// </summary>
    Task StopConsumingAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when a camera status changed event is received
    /// </summary>
    event EventHandler<CameraStatusChangedEvent>? CameraStatusChanged;
    
    /// <summary>
    /// Event fired when a camera error event is received
    /// </summary>
    event EventHandler<CameraErrorEvent>? CameraError;
    
    /// <summary>
    /// Event fired when a PTZ moved event is received
    /// </summary>
    event EventHandler<PtzMovedEvent>? PtzMoved;
    
    /// <summary>
    /// Event fired when camera statistics event is received
    /// </summary>
    event EventHandler<CameraStatisticsEvent>? CameraStatistics;
    
    /// <summary>
    /// Event fired when camera metadata is updated
    /// </summary>
    event EventHandler<CameraMetadataUpdatedEvent>? CameraMetadataUpdated;
}