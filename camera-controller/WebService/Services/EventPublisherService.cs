using Lightview.Shared.Contracts;

namespace WebService.Services;

public interface IEventPublisherService
{
    Task PublishCameraEventAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : BaseEvent;
    Task PublishSystemEventAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : BaseEvent;
}

public class EventPublisherService : IEventPublisherService
{
    private readonly ILogger<EventPublisherService> _logger;

    public EventPublisherService(ILogger<EventPublisherService> logger)
    {
        _logger = logger;
    }

    public async Task PublishCameraEventAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        // Here you would implement actual RabbitMQ publishing
        _logger.LogInformation("Publishing camera event: {EventType} for Camera: {CameraId}", 
            eventData.EventType, eventData.CameraId);
        
        // Example of what the actual implementation would look like:
        // await _rabbitMqClient.PublishAsync(eventData, RabbitMqConstants.CameraEventsExchange, 
        //     GetRoutingKey(eventData.EventType), cancellationToken);
        
        await Task.CompletedTask; // Placeholder
    }

    public async Task PublishSystemEventAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        _logger.LogInformation("Publishing system event: {EventType}", eventData.EventType);
        
        // Example implementation:
        // await _rabbitMqClient.PublishAsync(eventData, RabbitMqConstants.SystemEventsExchange, 
        //     RabbitMqConstants.SystemRoutingKey, cancellationToken);
        
        await Task.CompletedTask; // Placeholder
    }

    private static string GetRoutingKey(string eventType)
    {
        return eventType switch
        {
            "CameraStatusChanged" or "CameraConnected" or "CameraDisconnected" => RabbitMqConstants.CameraStatusRoutingKey,
            "CameraError" => RabbitMqConstants.CameraErrorRoutingKey,
            "StreamStarted" or "StreamStopped" or "StreamHealth" => RabbitMqConstants.StreamRoutingKey,
            "PtzMoved" or "PtzPreset" => RabbitMqConstants.PtzRoutingKey,
            "MotionDetected" => RabbitMqConstants.MotionRoutingKey,
            "OnvifEventReceived" => RabbitMqConstants.OnvifRoutingKey,
            _ => "unknown"
        };
    }
}