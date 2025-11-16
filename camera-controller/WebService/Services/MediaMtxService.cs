using System.Text;
using System.Text.Json;
using CameraController.Contracts.Interfaces;
using CameraController.Contracts.Models;
using Lightview.Shared.Contracts;

namespace WebService.Services;

/// <summary>
/// Service for managing MediaMTX streaming server integration
/// </summary>
public class MediaMtxService : IMediaMtxService
{
    private readonly HttpClient _httpClient;
    private readonly MediaMtxConfiguration _config;
    private readonly ILogger<MediaMtxService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MediaMtxService(
        HttpClient httpClient,
        MediaMtxConfiguration config,
        ILogger<MediaMtxService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_config.ApiUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<string> ConfigureRtspInputAsync(Camera camera, string rtspUrl, CancellationToken cancellationToken = default)
    {
        var streamPath = GenerateStreamPath(camera);
        
        _logger.LogInformation("Configuring RTSP input for camera {CameraId} at path {StreamPath}", camera.Id, streamPath);

        try
        {
            var pathConfig = new
            {
                name = streamPath,
                source = rtspUrl,
            };

            var json = JsonSerializer.Serialize(pathConfig, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"config/paths/add/{streamPath}", content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully configured stream for camera {CameraId} at path {StreamPath}", 
                    camera.Id, streamPath);
                return streamPath;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to configure stream for camera {CameraId}. Status: {StatusCode}, Error: {Error}", 
                    camera.Id, response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to configure MediaMTX stream: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while configuring stream for camera {CameraId}", camera.Id);
            throw;
        }
    }

    public async Task RemoveStreamConfigurationAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        var streamPath = GenerateStreamPath(cameraId);
        
        _logger.LogInformation("Removing stream configuration for camera {CameraId} at path {StreamPath}", 
            cameraId, streamPath);

        try
        {
            var response = await _httpClient.DeleteAsync($"/config/paths/delete/{streamPath}", cancellationToken);
            
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Successfully removed stream configuration for camera {CameraId}", cameraId);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to remove stream configuration for camera {CameraId}. Status: {StatusCode}, Error: {Error}", 
                    cameraId, response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while removing stream configuration for camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<string> GetWebRtcUrlAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        var streamPath = GenerateStreamPath(cameraId);
        var webRtcUrl = $"{_config.WebRtcUrl}/{streamPath}";
        
        _logger.LogDebug("Generated WebRTC URL for camera {CameraId}: {WebRtcUrl}", cameraId, webRtcUrl);
        
        return await Task.FromResult(webRtcUrl);
    }

    public async Task<bool> IsStreamActiveAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        var streamPath = GenerateStreamPath(cameraId);
        
        try
        {
            var response = await _httpClient.GetAsync($"/paths/get/{streamPath}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var pathInfo = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
                
                // Check if the path exists and has active readers/publishers
                if (pathInfo.TryGetProperty("ready", out var ready))
                {
                    return ready.GetBoolean();
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check stream status for camera {CameraId}", cameraId);
            return false;
        }
    }

    public async Task<MediaMtxStreamStatistics?> GetStreamStatisticsAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        var streamPath = GenerateStreamPath(cameraId);
        
        try
        {
            var response = await _httpClient.GetAsync($"/paths/get/{streamPath}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var pathInfo = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            var statistics = new MediaMtxStreamStatistics
            {
                CameraId = cameraId,
                StreamPath = streamPath,
                LastActivity = DateTime.UtcNow
            };

            // Parse MediaMTX response and populate statistics
            if (pathInfo.TryGetProperty("ready", out var ready))
            {
                statistics.IsActive = ready.GetBoolean();
            }

            if (pathInfo.TryGetProperty("readerCount", out var readerCount))
            {
                statistics.ClientCount = readerCount.GetInt32();
            }

            if (pathInfo.TryGetProperty("bytesReceived", out var bytesReceived))
            {
                statistics.BytesReceived = bytesReceived.GetInt64();
            }

            if (pathInfo.TryGetProperty("bytesSent", out var bytesSent))
            {
                statistics.BytesSent = bytesSent.GetInt64();
            }

            // Try to get track information for codec and resolution details
            if (pathInfo.TryGetProperty("tracks", out var tracks) && tracks.ValueKind == JsonValueKind.Array)
            {
                foreach (var track in tracks.EnumerateArray())
                {
                    if (track.TryGetProperty("codec", out var codec))
                    {
                        var codecName = codec.GetString();
                        if (codecName?.StartsWith("H264") == true || codecName?.StartsWith("H265") == true)
                        {
                            statistics.VideoCodec = codecName;
                        }
                        else if (codecName?.Contains("AAC") == true || codecName?.Contains("G711") == true)
                        {
                            statistics.AudioCodec = codecName;
                        }
                    }
                }
            }

            _logger.LogDebug("Retrieved statistics for camera {CameraId}: Active={IsActive}, Clients={ClientCount}", 
                cameraId, statistics.IsActive, statistics.ClientCount);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve stream statistics for camera {CameraId}", cameraId);
            return null;
        }
    }

    private string GenerateStreamPath(Camera camera)
    {
        return GenerateStreamPath(camera.Id);
    }

    private string GenerateStreamPath(Guid cameraId)
    {
        return $"{_config.StreamPathPrefix}_{cameraId}";
    }
}