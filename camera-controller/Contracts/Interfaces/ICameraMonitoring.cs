using Lightview.Shared.Contracts;
using CameraController.Contracts.Models;
using Lightview.Shared.Contracts.Settings;

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
    /// Whether monitoring is currently active
    /// </summary>
    bool IsMonitoring { get; }
    
    /// <summary>
    /// Last health check result
    /// </summary>
    CameraHealthStatus LastHealthStatus { get; }
    
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
    /// Event raised when a snapshot is captured during monitoring
    /// </summary>
    event EventHandler<CameraSnapshotCapturedEventArgs>? SnapshotCaptured;
    
    /// <summary>
    /// Get the latest captured snapshot data
    /// </summary>
    /// <returns>Tuple containing snapshot data, timestamp, and profile token. Returns null values if no snapshot available.</returns>
    (byte[]? Data, DateTime? Timestamp, string? ProfileToken) GetLatestSnapshot();
}