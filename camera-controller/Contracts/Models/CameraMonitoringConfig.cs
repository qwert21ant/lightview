using Lightview.Shared.Contracts;

namespace CameraController.Contracts.Models;

/// <summary>
/// Configuration for camera monitoring
/// </summary>
public class CameraMonitoringConfig
{
    /// <summary>
    /// Interval between health checks
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Timeout for individual health checks
    /// </summary>
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Number of consecutive failures before marking as unhealthy
    /// </summary>
    public int FailureThreshold { get; set; } = 3;
    
    /// <summary>
    /// Number of consecutive successes before marking as healthy again
    /// </summary>
    public int SuccessThreshold { get; set; } = 2;
    
    /// <summary>
    /// Whether to attempt automatic reconnection on failures
    /// </summary>
    public bool AutoReconnect { get; set; } = true;
    
    /// <summary>
    /// Maximum number of reconnection attempts
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 5;
    
    /// <summary>
    /// Delay between reconnection attempts
    /// </summary>
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Whether to publish health events to RabbitMQ
    /// </summary>
    public bool PublishHealthEvents { get; set; } = true;
    
    /// <summary>
    /// Whether to publish detailed statistics
    /// </summary>
    public bool PublishStatistics { get; set; } = false;
}