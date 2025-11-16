namespace Lightview.Shared.Contracts;

// RabbitMQ Configuration
public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public bool UseSSL { get; set; } = false;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(10);
    public bool AutomaticRecoveryEnabled { get; set; } = true;
}

// Exchange and Queue Names
public static class RabbitMqConstants
{
    // Exchanges
    public const string CameraEventsExchange = "lightview.camera.events";
    public const string SystemEventsExchange = "lightview.system.events";
    
    // Routing Keys
    public const string CameraStatusRoutingKey = "camera.status";
    public const string CameraConnectionRoutingKey = "camera.connection";
    public const string CameraErrorRoutingKey = "camera.error";
    public const string StreamRoutingKey = "stream";
    public const string PtzRoutingKey = "ptz";
    public const string MotionRoutingKey = "motion";
    public const string OnvifRoutingKey = "onvif";
    public const string SystemRoutingKey = "system";
    
    // Queue Names (for Core service)
    public const string CoreCameraEventsQueue = "lightview.core.camera.events";
    public const string CoreSystemEventsQueue = "lightview.core.system.events";
    
    // Queue Names (for other potential consumers)
    public const string NotificationEventsQueue = "lightview.notifications.events";
    public const string AnalyticsEventsQueue = "lightview.analytics.events";
    public const string RecordingEventsQueue = "lightview.recording.events";
}

// Event Publishing Interface
public interface IEventPublisher
{
    Task PublishAsync<T>(T eventData, string routingKey, CancellationToken cancellationToken = default) where T : BaseEvent;
    Task PublishCameraEventAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : BaseEvent;
    Task PublishSystemEventAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : BaseEvent;
}

// Event Handler Interface
public interface IEventHandler<in T> where T : BaseEvent
{
    Task HandleAsync(T eventData, CancellationToken cancellationToken = default);
}

// Message Envelope for RabbitMQ
public class EventMessage<T> where T : BaseEvent
{
    public T Event { get; set; } = default!;
    public string RoutingKey { get; set; } = string.Empty;
    public Dictionary<string, object> Headers { get; set; } = new();
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public string Publisher { get; set; } = string.Empty;
    public int RetryCount { get; set; } = 0;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}