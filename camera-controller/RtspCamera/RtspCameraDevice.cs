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

    public Guid Id => _configuration.Id;
    public CameraStatus Status { get; private set; } = CameraStatus.Disabled;
    public Camera Configuration => _configuration;
    public CameraCapabilities? Capabilities { get; private set; }
    public IReadOnlyList<CameraProfile> Profiles => _profiles.AsReadOnly();
    public IPtzControl? PtzControl => null; // Basic RTSP cameras typically don't have PTZ

    public event EventHandler<CameraStatusChangedEventArgs>? StatusChanged;

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

            var previousStatus = Status;
            Status = CameraStatus.Connecting;
            OnStatusChanged(previousStatus, Status);

            // Test network connectivity first
            if (!await TestNetworkConnectivityAsync(cancellationToken))
            {
                Status = CameraStatus.Offline;
                OnStatusChanged(CameraStatus.Connecting, Status);
                return false;
            }
            else
            {
                // Network is reachable - camera is offline but reachable
                Status = CameraStatus.Offline;
                OnStatusChanged(CameraStatus.Connecting, Status);
            }

            // Test RTSP stream availability
            var rtspUrl = _configuration.Url.ToString();
            if (await TestRtspStreamAsync(rtspUrl, cancellationToken))
            {
                Status = CameraStatus.Online;
                OnStatusChanged(CameraStatus.Offline, Status);
                _logger?.LogInformation("Successfully connected to RTSP camera {CameraName} - stream available", _configuration.Name);
                return true;
            }
            else
            {
                // Network is reachable but stream is not available - stay offline
                _logger?.LogWarning("RTSP stream test failed for camera {CameraName} - network reachable but stream unavailable", _configuration.Name);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to RTSP camera {CameraName}", _configuration.Name);
            Status = CameraStatus.Error;
            OnStatusChanged(CameraStatus.Connecting, Status);
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _logger?.LogInformation("Disconnecting from RTSP camera {CameraName}", _configuration.Name);
            var previousStatus = Status;
            Status = CameraStatus.Disabled;
            OnStatusChanged(previousStatus, Status);
            await Task.CompletedTask; // RTSP doesn't require explicit disconnection
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during disconnect from RTSP camera {CameraName}", _configuration.Name);
        }
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await TestNetworkConnectivityAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ping test failed for RTSP camera {CameraName}", _configuration.Name);
            return false;
        }
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
        
        // For RTSP cameras, we return the RTSP URL regardless of profile
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

    private async Task<bool> TestNetworkConnectivityAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(_configuration.Url.Host, 5000);
            return reply.Status == IPStatus.Success;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Ping test failed for {Host}", _configuration.Url.Host);
            return false;
        }
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

    private void OnStatusChanged(CameraStatus previousStatus, CameraStatus currentStatus)
    {
        if (previousStatus != currentStatus)
        {
            StatusChanged?.Invoke(this, new CameraStatusChangedEventArgs
            {
                PreviousStatus = previousStatus,
                CurrentStatus = currentStatus,
                Reason = $"Status changed from {previousStatus} to {currentStatus}"
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