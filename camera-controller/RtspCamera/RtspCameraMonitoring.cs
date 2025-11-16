using CameraController.Contracts.Interfaces;
using CameraController.Contracts.Models;
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

    public ICamera Camera => _camera;
    public CameraMonitoringConfig Config => _config;
    public bool IsMonitoring => _isMonitoring;
    public DateTime LastHealthCheck { get; private set; }
    public CameraHealthStatus LastHealthStatus { get; private set; } = new();

    public event EventHandler<CameraHealthChangedEventArgs>? HealthChanged;

    public RtspCameraMonitoring(
        ICamera camera, 
        CameraMonitoringConfig config, 
        ILogger<RtspCameraMonitoring>? logger = null)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger;
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

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Perform ping test to check if camera is reachable
            var isReachable = await _camera.PingAsync(cancellationToken);
            
            if (!isReachable)
            {
                healthStatus.IsHealthy = false;
                healthStatus.Issues.Add("Camera is not reachable");
                healthStatus.ConsecutiveFailures++;
                healthStatus.ConsecutiveSuccesses = 0;
                return healthStatus;
            }

            healthStatus.IsHealthy = true;
            healthStatus.ConsecutiveSuccesses++;
            healthStatus.ConsecutiveFailures = 0;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Health check failed for camera {CameraId}", _camera.Id);
            healthStatus.IsHealthy = false;
            healthStatus.Issues.Add($"Health check failed: {ex.Message}");
            healthStatus.LastError = ex;
            healthStatus.ConsecutiveFailures++;
            healthStatus.ConsecutiveSuccesses = 0;
        }
        finally
        {
            stopwatch.Stop();
            healthStatus.ResponseTime = stopwatch.Elapsed;
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

                    _logger?.LogDebug("Health check for camera {CameraId}: Healthy={IsHealthy}", _camera.Id, LastHealthStatus.IsHealthy);

                    // Raise event if health status changed significantly
                    if (previousHealth.IsHealthy != LastHealthStatus.IsHealthy)
                    {
                        HealthChanged?.Invoke(this, new CameraHealthChangedEventArgs
                        {
                            PreviousHealth = previousHealth,
                            CurrentHealth = LastHealthStatus
                        });
                    }
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