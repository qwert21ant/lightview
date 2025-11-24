using Lightview.Shared.Contracts;
using CameraController.Contracts.Models;
using Lightview.Shared.Contracts.InternalApi;

namespace CameraController.Contracts.Interfaces;

/// <summary>
/// Responsible for monitoring camera health and reporting status to RabbitMQ
/// </summary>
public interface ICameraMonitoring : IDisposable
{
    /// <summary>
    /// The camera being monitored
    /// </summary>
    ICamera Camera { get; }
    
    /// <summary>
    /// Monitoring configuration
    /// </summary>
    CameraMonitoringConfig Config { get; }
    
    /// <summary>
    /// Whether monitoring is currently active
    /// </summary>
    bool IsMonitoring { get; }
    
    /// <summary>
    /// Last health check result
    /// </summary>
    CameraHealthStatus LastHealthStatus { get; }
    
    /// <summary>
    /// Event raised when health status changes
    /// </summary>
    event EventHandler<CameraHealthChangedEventArgs> HealthChanged;
    
    /// <summary>
    /// Start monitoring the camera
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop monitoring the camera
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// Perform immediate health check
    /// </summary>
    Task<CameraHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update monitoring configuration
    /// </summary>
    void UpdateConfig(CameraMonitoringConfig config);
}