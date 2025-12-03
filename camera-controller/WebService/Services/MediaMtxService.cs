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

    public async Task<List<CameraProfile>> ConfigureStreamProfilesAsync(Camera camera, List<CameraProfile> profiles, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuring {ProfileCount} stream profiles for camera {CameraId}", profiles.Count, camera.Id);

        var updatedProfiles = new List<CameraProfile>();

        foreach (var profile in profiles)
        {
            if (profile.OriginFeedUri == null)
            {
                _logger.LogWarning("Skipping profile {ProfileToken} for camera {CameraId} - no origin feed URI", profile.Token, camera.Id);
                updatedProfiles.Add(profile);
                continue;
            }

            try
            {
                var streamPath = GenerateStreamPath(camera.Id, profile.Token);
                
                // First, try to remove existing path if it exists
                try
                {
                    var resp = await _httpClient.DeleteAsync($"config/paths/delete/{streamPath}", cancellationToken);
                    _logger.LogDebug("Removed existing stream path {StreamPath} for camera {CameraId} profile {ProfileToken}", 
                        streamPath, camera.Id, profile.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not remove existing path {StreamPath} (may not exist)", streamPath);
                }

                var pathConfig = new
                {
                    name = streamPath,
                    source = profile.OriginFeedUri.ToString(),
                };

                var json = JsonSerializer.Serialize(pathConfig, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"config/paths/add/{streamPath}", content, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    // Create updated profile with local MediaMTX URIs
                    var localRtspUri = new Uri($"{_config.RtspUrl}/{streamPath}");
                    var webRtcUri = new Uri($"{_config.WebRtcUrl}/{streamPath}");
                    
                    var updatedProfile = new CameraProfile
                    {
                        Token = profile.Token,
                        Name = profile.Name,
                        Video = profile.Video,
                        Audio = profile.Audio,
                        OriginFeedUri = profile.OriginFeedUri,
                        RtspUri = localRtspUri,
                        WebRtcUri = webRtcUri,
                        IsMainStream = profile.IsMainStream
                    };
                    
                    updatedProfiles.Add(updatedProfile);
                    
                    _logger.LogInformation("Successfully configured stream for camera {CameraId} profile {ProfileToken} at path {StreamPath}", 
                        camera.Id, profile.Token, streamPath);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to configure stream for camera {CameraId} profile {ProfileToken}. Status: {StatusCode}, Error: {Error}", 
                        camera.Id, profile.Token, response.StatusCode, errorContent);
                    
                    // Add profile without local URIs
                    updatedProfiles.Add(profile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while configuring stream for camera {CameraId} profile {ProfileToken}", camera.Id, profile.Token);
                // Add profile without WebRTC URI
                updatedProfiles.Add(profile);
            }
        }

        return updatedProfiles;
    }

    public async Task RemoveAllStreamConfigurationsAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing all stream configurations for camera {CameraId}", cameraId);

        try
        {
            // Get all paths to find camera-specific streams
            var response = await _httpClient.GetAsync("/config/paths/list", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var pathsData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
                
                var cameraStreamPrefix = $"cam_{cameraId:N}_";
                var streamsToRemove = new List<string>();

                if (pathsData.TryGetProperty("paths", out var paths) && paths.ValueKind == JsonValueKind.Object)
                {
                    foreach (var path in paths.EnumerateObject())
                    {
                        if (path.Name.StartsWith(cameraStreamPrefix))
                        {
                            streamsToRemove.Add(path.Name);
                        }
                    }
                }

                // Remove each stream
                foreach (var streamPath in streamsToRemove)
                {
                    try
                    {
                        var deleteResponse = await _httpClient.DeleteAsync($"/config/paths/delete/{streamPath}", cancellationToken);
                        
                        if (deleteResponse.IsSuccessStatusCode || deleteResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _logger.LogDebug("Successfully removed stream {StreamPath} for camera {CameraId}", streamPath, cameraId);
                        }
                        else
                        {
                            var errorContent = await deleteResponse.Content.ReadAsStringAsync(cancellationToken);
                            _logger.LogWarning("Failed to remove stream {StreamPath} for camera {CameraId}. Status: {StatusCode}, Error: {Error}", 
                                streamPath, cameraId, deleteResponse.StatusCode, errorContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception while removing stream {StreamPath} for camera {CameraId}", streamPath, cameraId);
                    }
                }

                _logger.LogInformation("Successfully removed {StreamCount} stream configurations for camera {CameraId}", streamsToRemove.Count, cameraId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while removing stream configurations for camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<Uri?> GetWebRtcUrlAsync(Guid cameraId, string profileToken, CancellationToken cancellationToken = default)
    {
        var streamPath = GenerateStreamPath(cameraId, profileToken);
        
        try
        {
            // Check if stream exists first
            if (await IsStreamActiveAsync(cameraId, profileToken, cancellationToken))
            {
                var webRtcUrl = new Uri($"{_config.WebRtcUrl}/{streamPath}");
                _logger.LogDebug("Generated WebRTC URL for camera {CameraId} profile {ProfileToken}: {WebRtcUrl}", cameraId, profileToken, webRtcUrl);
                return webRtcUrl;
            }
            else
            {
                _logger.LogWarning("Stream not active for camera {CameraId} profile {ProfileToken}", cameraId, profileToken);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WebRTC URL for camera {CameraId} profile {ProfileToken}", cameraId, profileToken);
            return null;
        }
    }

    public async Task<bool> IsStreamActiveAsync(Guid cameraId, string profileToken, CancellationToken cancellationToken = default)
    {
        var streamPath = GenerateStreamPath(cameraId, profileToken);
        
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
            _logger.LogWarning(ex, "Failed to check stream status for camera {CameraId} profile {ProfileToken}", cameraId, profileToken);
            return false;
        }
    }

    public async Task<MediaMtxStreamStatistics?> GetStreamStatisticsAsync(Guid cameraId, string profileToken, CancellationToken cancellationToken = default)
    {
        var streamPath = GenerateStreamPath(cameraId, profileToken);
        
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
        return $"{_config.StreamPathPrefix}_{cameraId:N}";
    }

    private string GenerateStreamPath(Guid cameraId, string profileToken)
    {
        return $"cam_{cameraId:N}_{profileToken}";
    }
}