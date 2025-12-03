using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using Microsoft.Extensions.Logging;
using CameraController.Contracts.Interfaces;
using CameraController.Contracts.Models;
using Lightview.Shared.Contracts;

namespace RtspCamera;

/// <summary>
/// RTSP camera implementation for basic RTSP streaming cameras
/// </summary>
public class RtspCameraDevice : ICamera
{
    private readonly Camera _configuration;
    private readonly ILogger<RtspCameraDevice>? _logger;
    private readonly HttpClient _httpClient;
    private readonly List<CameraProfile> _profiles;
    private bool _disposed;
    
    // Health check timeouts
    private readonly TimeSpan _pingTimeout = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _streamTimeout = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _credentialsTimeout = TimeSpan.FromSeconds(8);

    public Guid Id => _configuration.Id;
    public CameraStatus Status { get; private set; } = CameraStatus.Offline;
    public Camera Configuration => _configuration;
    public CameraCapabilities? Capabilities { get; private set; }
    public IReadOnlyList<CameraProfile> Profiles => _profiles.AsReadOnly();
    public IPtzControl? PtzControl => null; // Basic RTSP cameras typically don't have PTZ

    public event EventHandler<CameraStatusChangedEventArgs>? StatusChanged;

    public void UpdateStatus(CameraStatus status, string? reason = null)
    {
        var previousStatus = Status;
        if (previousStatus != status)
        {
            Status = status;
            OnStatusChanged(previousStatus, status, reason ?? $"Status updated to {status}");
        }
    }

    public void UpdateProfiles(List<CameraProfile> profiles)
    {
        _profiles.Clear();
        _profiles.AddRange(profiles);
        _logger?.LogDebug("Updated {ProfileCount} profiles for camera {CameraId}", profiles.Count, Id);
    }

    private void UpdateProfilesWithRtspUri()
    {
        // Update all profiles with the origin feed URI from the camera configuration
        foreach (var profile in _profiles)
        {
            profile.OriginFeedUri = _configuration.Url;
        }
        _logger?.LogDebug("Updated {ProfileCount} profiles with origin feed URI for camera {CameraId}", _profiles.Count, Id);
    }

    public RtspCameraDevice(Camera configuration, ILogger<RtspCameraDevice>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
        _profiles = new List<CameraProfile>();
        
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        // Set up basic authentication if credentials are provided
        if (!string.IsNullOrEmpty(_configuration.Username) && !string.IsNullOrEmpty(_configuration.Password))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_configuration.Username}:{_configuration.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        }

        // Initialize basic capabilities for RTSP cameras
        Capabilities = new CameraCapabilities
        {
            SupportsPtz = false,
            SupportsAudio = true, // Most RTSP cameras support audio
            SupportsMotionDetection = false, // Not typically exposed via RTSP
            SupportsIrCut = false, // Unknown for basic RTSP
            SupportsPresets = false,
            SupportsSnapshot = true, // Most RTSP cameras provide snapshot capability
            SupportsZoom = false,
            SupportsFocus = false,
            SupportsIris = false,
            SupportedProfiles = new List<string> { "Main", "High" }
        };

        // Add default profile
        _profiles.Add(new CameraProfile
        {
            Token = "main",
            Name = "Main Stream",
            Video = new VideoSettings
            {
                Codec = "H.264",
                Resolution = new Resolution { Width = 1920, Height = 1080 },
                Framerate = 30,
                Bitrate = 4000000,
                BitrateControl = BitrateControl.CBR,
                Quality = 8
            },
            IsMainStream = true
        });
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Connecting to RTSP camera {CameraName} at {Url}", 
                _configuration.Name, _configuration.Url);

            UpdateStatus(CameraStatus.Connecting, "Starting connection process");

            // Perform all health checks during connection
            var healthResults = await PerformAllHealthChecksAsync(cancellationToken);
            
            if (healthResults.OverallHealthy)
            {
                // Update profiles with RTSP URIs after successful connection
                UpdateProfilesWithRtspUri();
                
                UpdateStatus(CameraStatus.Online, "All health checks passed");
                _configuration.LastConnectedAt = DateTime.UtcNow;
                _logger?.LogInformation("Successfully connected to RTSP camera {CameraName} - all checks passed", _configuration.Name);
                return true;
            }
            else
            {
                var failedChecks = string.Join(", ", healthResults.Results.Where(r => !r.IsSuccessful).Select(r => r.CheckName));
                UpdateStatus(CameraStatus.Offline, $"Health checks failed: {failedChecks}");
                _logger?.LogWarning("Connection failed for RTSP camera {CameraName} - failed checks: {FailedChecks}", 
                    _configuration.Name, failedChecks);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to RTSP camera {CameraName}", _configuration.Name);
            UpdateStatus(CameraStatus.Error, $"Connection error: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _logger?.LogInformation("Disconnecting from RTSP camera {CameraName}", _configuration.Name);
            UpdateStatus(CameraStatus.Offline, "Disconnected");
            await Task.CompletedTask; // RTSP doesn't require explicit disconnection
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during disconnect from RTSP camera {CameraName}", _configuration.Name);
        }
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        var result = await PingCheckAsync(cancellationToken);
        return result.IsSuccessful;
    }

    public async Task<HealthCheckResult> PingCheckAsync(CancellationToken cancellationToken = default)
    {
        var result = new HealthCheckResult { CheckName = "Ping" };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync(_configuration.Url.Host, (int)_pingTimeout.TotalMilliseconds);
            
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.IsSuccessful = reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            
            if (!result.IsSuccessful)
            {
                result.ErrorMessage = $"Ping failed: {reply.Status}";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.IsSuccessful = false;
            result.ErrorMessage = $"Ping error: {ex.Message}";
            _logger?.LogDebug(ex, "Ping check failed for {Host}", _configuration.Url.Host);
        }
        
        return result;
    }

    public async Task<HealthCheckResult> CredentialsCheckAsync(CancellationToken cancellationToken = default)
    {
        var result = new HealthCheckResult { CheckName = "Credentials" };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // If no credentials are provided, consider it successful
            if (string.IsNullOrEmpty(_configuration.Username) && string.IsNullOrEmpty(_configuration.Password))
            {
                result.IsSuccessful = true;
                stopwatch.Stop();
                result.ResponseTime = stopwatch.Elapsed;
                return result;
            }
            
            // Test credentials by attempting an HTTP request to camera (if HTTP interface available)
            var httpUrl = $"http://{_configuration.Url.Host}:{(_configuration.Url.Port > 0 ? _configuration.Url.Port : 80)}";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, httpUrl);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_credentialsTimeout);
            
            var response = await _httpClient.SendAsync(request, cts.Token);
            
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            
            // Any response (including 401) means credentials check passed - we can reach the camera
            result.IsSuccessful = true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.IsSuccessful = false;
            result.ErrorMessage = $"Credentials check error: {ex.Message}";
            _logger?.LogDebug(ex, "Credentials check failed for camera {CameraName}", _configuration.Name);
        }
        
        return result;
    }

    public async Task<HealthCheckResult> StreamCheckAsync(CancellationToken cancellationToken = default)
    {
        var result = new HealthCheckResult { CheckName = "Stream" };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var rtspUrl = _configuration.Url.ToString();
            var streamAvailable = await TestRtspStreamAsync(rtspUrl, cancellationToken);
            
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.IsSuccessful = streamAvailable;
            
            if (!result.IsSuccessful)
            {
                result.ErrorMessage = "RTSP stream is not accessible";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.IsSuccessful = false;
            result.ErrorMessage = $"Stream check error: {ex.Message}";
            _logger?.LogDebug(ex, "Stream check failed for RTSP stream {RtspUrl}", _configuration.Url);
        }
        
        return result;
    }

    public async Task<CameraHealthCheckResults> PerformAllHealthChecksAsync(CancellationToken cancellationToken = default)
    {
        var overallStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = new CameraHealthCheckResults();
        
        try
        {
            // Perform all health checks
            var pingTask = PingCheckAsync(cancellationToken);
            var credentialsTask = CredentialsCheckAsync(cancellationToken);
            var streamTask = StreamCheckAsync(cancellationToken);
            
            await Task.WhenAll(pingTask, credentialsTask, streamTask);
            
            results.Results.Add(await pingTask);
            results.Results.Add(await credentialsTask);
            results.Results.Add(await streamTask);
            
            // Overall health is successful if all checks pass
            results.OverallHealthy = results.Results.All(r => r.IsSuccessful);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error performing health checks for camera {CameraName}", _configuration.Name);
            results.OverallHealthy = false;
            
            // Add error result if no individual results were added
            if (results.Results.Count == 0)
            {
                results.Results.Add(new HealthCheckResult
                {
                    CheckName = "Overall",
                    IsSuccessful = false,
                    ErrorMessage = $"Health check error: {ex.Message}"
                });
            }
        }
        finally
        {
            overallStopwatch.Stop();
            results.TotalResponseTime = overallStopwatch.Elapsed;
        }
        
        return results;
    }

    public async Task<OnvifDeviceInfo?> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        // RTSP cameras don't typically expose ONVIF device info
        // This would return basic info if available
        await Task.CompletedTask;
        return new OnvifDeviceInfo
        {
            Manufacturer = _configuration.DeviceInfo?.Manufacturer ?? "Unknown",
            Model = _configuration.DeviceInfo?.Model ?? "RTSP Camera",
            FirmwareVersion = _configuration.DeviceInfo?.FirmwareVersion ?? "Unknown",
            SerialNumber = _configuration.DeviceInfo?.SerialNumber ?? "Unknown",
            HardwareId = Id.ToString()
        };
    }

    public async Task<List<CameraProfile>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return new List<CameraProfile>(_profiles);
    }

    public async Task<Uri?> GetStreamUriAsync(string profileToken, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        // For RTSP cameras, we return the origin feed URL regardless of profile
        // This is the direct camera stream URL that will be proxied through MediaMTX
        return _configuration.Url;
    }

    public async Task<byte[]?> CaptureSnapshotAsync(string? profileToken = null, CancellationToken cancellationToken = default)
    {
        return null;
    }

    public async Task<ImageSettings?> GetImageSettingsAsync(CancellationToken cancellationToken = default)
    {
        // RTSP cameras don't typically expose image settings via RTSP
        // This would need camera-specific HTTP API calls
        await Task.CompletedTask;
        return null;
    }

    public async Task<bool> SetImageSettingsAsync(ImageSettings settings, CancellationToken cancellationToken = default)
    {
        // RTSP cameras don't typically allow image settings via RTSP
        // This would need camera-specific HTTP API calls
        await Task.CompletedTask;
        return false;
    }



    private async Task<bool> TestRtspStreamAsync(string rtspUrl, CancellationToken cancellationToken)
    {
        try
        {
            // Use ffprobe to test RTSP stream if available
            // This is a basic implementation - in production you might use a dedicated RTSP library
            return await TestRtspWithFfprobeAsync(rtspUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "RTSP stream test failed for {RtspUrl}", rtspUrl);
            return false;
        }
    }

    private async Task<bool> TestRtspWithFfprobeAsync(string rtspUrl, CancellationToken cancellationToken)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v quiet -print_format json -show_streams -analyzeduration 1000000 -probesize 1000000 \"{rtspUrl}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var timeout = TimeSpan.FromSeconds(10);
            await process.WaitForExitAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            var completed = process.HasExited;
            
            if (!completed)
            {
                process.Kill();
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "FFprobe test failed for RTSP stream");
            
            // Fallback: try to connect to RTSP port
            return await TestRtspPortAsync(cancellationToken);
        }
    }

    private async Task<bool> TestRtspPortAsync(CancellationToken cancellationToken)
    {
        try
        {
            var port = _configuration.Url.Port > 0 ? _configuration.Url.Port : 554; // Default RTSP port
            using var tcpClient = new System.Net.Sockets.TcpClient();
            var connectTask = tcpClient.ConnectAsync(_configuration.Url.Host, port);
            await connectTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            return tcpClient.Connected;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "TCP port test failed for {Host}:{Port}", _configuration.Url.Host, _configuration.Url.Port);
            return false;
        }
    }

    private void OnStatusChanged(CameraStatus previousStatus, CameraStatus currentStatus, string? reason = null)
    {
        if (previousStatus != currentStatus)
        {
            StatusChanged?.Invoke(this, new CameraStatusChangedEventArgs
            {
                PreviousStatus = previousStatus,
                CurrentStatus = currentStatus,
                Reason = reason ?? $"Status changed from {previousStatus} to {currentStatus}"
            });
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}