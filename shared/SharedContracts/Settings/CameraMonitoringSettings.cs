namespace Lightview.Shared.Contracts.Settings;

/// <summary>
/// Camera monitoring configuration settings
/// </summary>
public class CameraMonitoringSettings
{
    /// <summary>
    /// Interval between health checks
    /// </summary>
    public int HealthCheckInterval { get; set; } = 60; // 1 minute
    
    /// <summary>
    /// Timeout for individual health checks
    /// </summary>
    public int HealthCheckTimeout { get; set; } = 10;
    
    /// <summary>
    /// Number of consecutive failures before marking as unhealthy
    /// </summary>
    public int FailureThreshold { get; set; } = 3;
    
    /// <summary>
    /// Number of consecutive successes before marking as healthy again
    /// </summary>
    public int SuccessThreshold { get; set; } = 2;
    
    /// <summary>
    /// Interval between snapshot captures
    /// </summary>
    public int SnapshotInterval { get; set; } = 120; // 2 minutes
}