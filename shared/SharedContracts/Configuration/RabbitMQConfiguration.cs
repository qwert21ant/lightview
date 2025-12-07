namespace Lightview.Shared.Contracts.Configuration;

/// <summary>
/// RabbitMQ connection configuration
/// </summary>
public class RabbitMQConfiguration
{
    public const string SectionName = "RabbitMQ";
    
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    
    // Exchange configuration
    public string EventsExchange { get; set; } = "lightview.camera.events";
    public string EventsExchangeType { get; set; } = "topic";
    
    // Queue configuration for core service
    public string CoreQueueName { get; set; } = "lightview.core.camera.events";
    public string CoreRoutingKey { get; set; } = "camera.#";
    
    // Connection settings
    public int ConnectionTimeoutMs { get; set; } = 30000;
    public int NetworkRecoveryInterval { get; set; } = 5000;
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public int RequestedHeartbeat { get; set; } = 60;
    
    /// <summary>
    /// Get connection string for RabbitMQ
    /// </summary>
    public string GetConnectionString()
    {
        return $"amqp://{Username}:{Password}@{Host}:{Port}{VirtualHost}";
    }
}

/// <summary>
/// Message broker routing keys for different event types
/// </summary>
public static class CameraEventRoutingKeys
{
    public const string StatusChanged = "camera.status.changed";
    public const string Error = "camera.error";
    public const string PtzMoved = "camera.ptz.moved";
    public const string CameraStatistics = "camera.statistics";
    public const string MetadataUpdated = "camera.metadata.updated";
    public const string SnapshotCaptured = "camera.snapshot.captured";
}