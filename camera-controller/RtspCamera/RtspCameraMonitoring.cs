using CameraController.Contracts.Interfaces;
using CameraController.Contracts.Models;
using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;
using Microsoft.Extensions.Logging;

namespace RtspCamera;

/// <summary>
/// Basic camera monitoring implementation for RTSP cameras
/// </summary>
public class RtspCameraMonitoring : ICameraMonitoring
{
    private readonly ICamera _camera;
    private CameraMonitoringConfig _config;
    private readonly ILogger<RtspCameraMonitoring>? _logger;
    private CancellationTokenSource? _monitoringCancellationTokenSource;
    private Task? _monitoringTask;
    private bool _isMonitoring;
    private bool _disposed;
    
    // Health monitoring state
    private int _consecutiveFailures = 0;
    private readonly int _maxConsecutiveFailures = 3; // Number of failures before marking as degraded
    
    // Snapshot capture state
    private DateTime _lastSnapshotCapture = DateTime.MinValue;
    private byte[]? _latestSnapshotData;
    private DateTime? _latestSnapshotTimestamp;
    private string? _latestSnapshotProfileToken;
    private readonly object _snapshotLock = new();

    public ICamera Camera => _camera;
    public CameraMonitoringConfig Config => _config;
    public bool IsMonitoring => _isMonitoring;
    public DateTime LastHealthCheck { get; private set; }
    public CameraHealthStatus LastHealthStatus { get; private set; } = new();
    public event EventHandler<CameraSnapshotCapturedEventArgs>? SnapshotCaptured;

    public RtspCameraMonitoring(
        ICamera camera, 
        CameraMonitoringConfig config, 
        ILogger<RtspCameraMonitoring>? logger = null)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger;
    }

    /// <summary>
    /// Get the latest captured snapshot data
    /// </summary>
    public (byte[]? Data, DateTime? Timestamp, string? ProfileToken) GetLatestSnapshot()
    {
        lock (_snapshotLock)
        {
            return (_latestSnapshotData, _latestSnapshotTimestamp, _latestSnapshotProfileToken);
        }
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_isMonitoring)
            return;

        _logger?.LogInformation("Starting monitoring for camera {CameraId}", _camera.Id);

        try
        {
            _isMonitoring = true;
            
            // Create cancellation token source for monitoring loop
            _monitoringCancellationTokenSource = new CancellationTokenSource();
            
            // Start background monitoring task
            _monitoringTask = MonitoringLoopAsync(_monitoringCancellationTokenSource.Token);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start monitoring for camera {CameraId}", _camera.Id);
            _isMonitoring = false;
            _monitoringCancellationTokenSource?.Dispose();
            _monitoringCancellationTokenSource = null;
            throw;
        }
    }

    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
            return;

        _logger?.LogInformation("Stopping monitoring for camera {CameraId}", _camera.Id);

        try
        {
            _isMonitoring = false;
            
            // Cancel the monitoring loop
            _monitoringCancellationTokenSource?.Cancel();
            
            // Wait for monitoring task to complete
            if (_monitoringTask != null)
            {
                try
                {
                    await _monitoringTask.WaitAsync(TimeSpan.FromSeconds(10));
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation token is used
                }
                catch (TimeoutException)
                {
                    _logger?.LogWarning("Monitoring task did not complete within timeout for camera {CameraId}", _camera.Id);
                }
            }
            
            // Clean up resources
            _monitoringCancellationTokenSource?.Dispose();
            _monitoringCancellationTokenSource = null;
            _monitoringTask = null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to stop monitoring for camera {CameraId}", _camera.Id);
            throw;
        }
    }

    public async Task<CameraHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var healthStatus = new CameraHealthStatus
        {
            CheckedAt = DateTime.UtcNow,
            Issues = new List<string>()
        };

        try
        {
            // Only perform health checks if camera is online or degraded
            if (_camera.Status == CameraStatus.Online || _camera.Status == CameraStatus.Degraded)
            {
                // Perform comprehensive health checks
                var healthResults = await _camera.PerformAllHealthChecksAsync(cancellationToken);
                
                healthStatus.IsHealthy = healthResults.OverallHealthy;
                healthStatus.ResponseTime = healthResults.TotalResponseTime;
                
                if (!healthResults.OverallHealthy)
                {
                    foreach (var result in healthResults.Results.Where(r => !r.IsSuccessful))
                    {
                        healthStatus.Issues.Add($"{result.CheckName}: {result.ErrorMessage}");
                    }
                }
            }
            else
            {
                // Camera is not in a state where health checks are meaningful
                healthStatus.IsHealthy = false;
                healthStatus.Issues.Add($"Camera status is {_camera.Status}, not monitoring health");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Health check failed for camera {CameraId}", _camera.Id);
            healthStatus.IsHealthy = false;
            healthStatus.Issues.Add($"Health check failed: {ex.Message}");
            healthStatus.LastError = ex;
        }

        return healthStatus;
    }

    private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Starting monitoring loop for camera {CameraId}", _camera.Id);
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var previousHealth = LastHealthStatus;
                    LastHealthStatus = await CheckHealthAsync(cancellationToken);
                    LastHealthCheck = DateTime.UtcNow;

                    _logger?.LogDebug("Health check for camera {CameraId}: Healthy={IsHealthy}, Status={Status}", 
                        _camera.Id, LastHealthStatus.IsHealthy, _camera.Status);

                    // Update camera status based on health check results
                    await UpdateCameraStatusBasedOnHealthAsync(LastHealthStatus);
                    
                    // Check if we need to capture a snapshot
                    await TryCaptureSnapshotAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during health check for camera {CameraId}", _camera.Id);
                }

                // Wait for the configured interval before next check
                try
                {
                    await Task.Delay(_config.HealthCheckInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error in monitoring loop for camera {CameraId}", _camera.Id);
        }
        finally
        {
            _logger?.LogDebug("Monitoring loop ended for camera {CameraId}", _camera.Id);
        }
    }
    
    private async Task UpdateCameraStatusBasedOnHealthAsync(CameraHealthStatus healthStatus)
    {
        try
        {
            var currentStatus = _camera.Status;
            
            // Only update status for cameras that are online or degraded
            if (currentStatus == CameraStatus.Online || currentStatus == CameraStatus.Degraded)
            {
                if (healthStatus.IsHealthy)
                {
                    // Health check passed
                    _consecutiveFailures = 0;
                    
                    if (currentStatus == CameraStatus.Degraded)
                    {
                        // Recover from degraded to online
                        _camera.UpdateStatus(CameraStatus.Online, "Health checks recovered");
                        _logger?.LogInformation("Camera {CameraId} recovered from degraded state", _camera.Id);
                    }
                }
                else
                {
                    // Health check failed
                    _consecutiveFailures++;
                    
                    if (_consecutiveFailures >= _maxConsecutiveFailures && currentStatus == CameraStatus.Online)
                    {
                        // Mark as degraded after consecutive failures
                        var issues = string.Join("; ", healthStatus.Issues);
                        _camera.UpdateStatus(CameraStatus.Degraded, $"Health checks failed {_consecutiveFailures} times: {issues}");
                        _logger?.LogWarning("Camera {CameraId} marked as degraded after {FailureCount} consecutive health check failures", 
                            _camera.Id, _consecutiveFailures);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating camera status based on health for camera {CameraId}", _camera.Id);
        }
        
        await Task.CompletedTask;
    }
    
    private async Task TryCaptureSnapshotAsync(CancellationToken cancellationToken)
    {
        try
        {                
            // Check if it's time for the next snapshot
            var timeSinceLastSnapshot = DateTime.UtcNow - _lastSnapshotCapture;
            if (timeSinceLastSnapshot < _config.SnapshotInterval)
                return;
                
            // Only capture snapshots if camera is online
            if (_camera.Status != CameraStatus.Online)
            {
                _logger?.LogDebug("Skipping snapshot capture for camera {CameraId} - status is {Status}", 
                    _camera.Id, _camera.Status);
                return;
            }

            _logger?.LogDebug("Capturing scheduled snapshot for camera {CameraId}", _camera.Id);
            
            var captureStartTime = DateTime.UtcNow;
            var snapshotBytes = await _camera.CaptureSnapshotAsync(_config.SnapshotProfileToken, cancellationToken);
            var captureTime = DateTime.UtcNow - captureStartTime;
            
            if (snapshotBytes != null && snapshotBytes.Length > 0)
            {
                var captureTimestamp = DateTime.UtcNow;
                _lastSnapshotCapture = captureTimestamp;
                
                // Store the latest snapshot
                lock (_snapshotLock)
                {
                    _latestSnapshotData = snapshotBytes;
                    _latestSnapshotTimestamp = captureTimestamp;
                    _latestSnapshotProfileToken = _config.SnapshotProfileToken;
                }
                
                _logger?.LogInformation("Snapshot captured for camera {CameraId}: {ImageSize} bytes, capture took {CaptureTime}ms", 
                    _camera.Id, snapshotBytes.Length, captureTime.TotalMilliseconds);
                
                // Raise C# event with snapshot data
                SnapshotCaptured?.Invoke(this, new CameraSnapshotCapturedEventArgs
                {
                    CameraId = _camera.Id,
                    SnapshotData = snapshotBytes,
                    ProfileToken = _config.SnapshotProfileToken,
                    CaptureTime = captureTime
                });
            }
            else
            {
                _logger?.LogWarning("Failed to capture snapshot for camera {CameraId} - returned null or empty", _camera.Id);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error capturing scheduled snapshot for camera {CameraId}", _camera.Id);
        }
    }

    public void UpdateConfig(CameraMonitoringConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        // Note: The new configuration will be picked up on the next health check cycle
        // The monitoring loop reads _config.HealthCheckInterval for each delay
        _logger?.LogDebug("Updated monitoring configuration for camera {CameraId}", _camera.Id);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            // Stop monitoring if it's running
            if (_isMonitoring)
            {
                try
                {
                    StopMonitoringAsync().Wait(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error stopping monitoring during dispose for camera {CameraId}", _camera.Id);
                }
            }
            
            // Clean up resources
            _monitoringCancellationTokenSource?.Dispose();
            _monitoringTask?.Dispose();
        }
    }
}