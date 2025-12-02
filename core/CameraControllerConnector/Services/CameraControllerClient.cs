using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CameraControllerConnector.Interfaces;
using CameraControllerConnector.Models;
using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;
using Microsoft.Extensions.Logging;

namespace CameraControllerConnector.Services;

/// <summary>
/// HTTP client for communicating with the camera controller service
/// </summary>
public class CameraControllerClient : ICameraControllerClient
{
    private readonly HttpClient _httpClient;
    private readonly CameraControllerConfiguration _config;
    private readonly ILogger<CameraControllerClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CameraControllerClient(
        HttpClient httpClient,
        CameraControllerConfiguration config,
        ILogger<CameraControllerClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_config.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<CameraStatusResponse>> GetAllCamerasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching all cameras from controller");
            
            var response = await _httpClient.GetAsync("/api/cameras", cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<CameraStatusResponse>>>(_jsonOptions, cancellationToken);
            
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                _logger.LogDebug("Retrieved {Count} cameras", apiResponse.Data.Count);
                return apiResponse.Data;
            }

            _logger.LogWarning("Failed to get cameras: {Error}", apiResponse?.ErrorMessage);
            return new List<CameraStatusResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cameras from controller");
            throw;
        }
    }

    public async Task<CameraStatusResponse?> GetCameraAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching camera {CameraId} from controller", cameraId);
            
            var response = await _httpClient.GetAsync($"/api/cameras/{cameraId}", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CameraStatusResponse>>(_jsonOptions, cancellationToken);
            
            if (apiResponse?.Success == true)
            {
                return apiResponse.Data;
            }

            _logger.LogWarning("Failed to get camera {CameraId}: {Error}", cameraId, apiResponse?.ErrorMessage);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching camera {CameraId} from controller", cameraId);
            throw;
        }
    }

    public async Task<CameraStatusResponse> AddCameraAsync(Guid id, AddCameraRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Adding camera {CameraName} to controller with ID {CameraId}", request.Name, id);
            
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/cameras/{id}", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CameraStatusResponse>>(_jsonOptions, cancellationToken);
            
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                _logger.LogInformation("Successfully added camera {CameraId}", id);
                return apiResponse.Data;
            }

            throw new InvalidOperationException($"Failed to add camera: {apiResponse?.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding camera {CameraName} to controller", request.Name);
            throw;
        }
    }

    public async Task<CameraStatusResponse> UpdateCameraAsync(Guid cameraId, UpdateCameraRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating camera {CameraId} in controller", cameraId);
            
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/api/cameras/{cameraId}", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CameraStatusResponse>>(_jsonOptions, cancellationToken);
            
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                _logger.LogInformation("Successfully updated camera {CameraId}", cameraId);
                return apiResponse.Data;
            }

            throw new InvalidOperationException($"Failed to update camera: {apiResponse?.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating camera {CameraId} in controller", cameraId);
            throw;
        }
    }

    public async Task<bool> RemoveCameraAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing camera {CameraId} from controller", cameraId);
            
            var response = await _httpClient.DeleteAsync($"/api/cameras/{cameraId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions, cancellationToken);
            
            if (apiResponse?.Success == true)
            {
                _logger.LogInformation("Successfully removed camera {CameraId}", cameraId);
                return true;
            }

            _logger.LogWarning("Failed to remove camera {CameraId}: {Error}", cameraId, apiResponse?.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing camera {CameraId} from controller", cameraId);
            throw;
        }
    }

    public async Task<bool> ConnectCameraAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Connecting camera {CameraId}", cameraId);
            
            var response = await _httpClient.PostAsync($"/api/cameras/{cameraId}/connect", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions, cancellationToken);
            return apiResponse?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<bool> DisconnectCameraAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Disconnecting camera {CameraId}", cameraId);
            
            var response = await _httpClient.PostAsync($"/api/cameras/{cameraId}/disconnect", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions, cancellationToken);
            return apiResponse?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<CameraHealthStatus?> GetCameraHealthAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching health status for camera {CameraId}", cameraId);
            
            var response = await _httpClient.GetAsync($"/api/cameras/{cameraId}/health", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CameraHealthStatus>>(_jsonOptions, cancellationToken);
            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health for camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<byte[]?> CaptureSnapshotAsync(Guid cameraId, string? profileToken = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Capturing snapshot from camera {CameraId}", cameraId);
            
            var url = $"/api/cameras/{cameraId}/snapshot";
            if (!string.IsNullOrEmpty(profileToken))
            {
                url += $"?profileToken={Uri.EscapeDataString(profileToken)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing snapshot from camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<string?> GetWebRtcStreamUrlAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting WebRTC stream URL for camera {CameraId}", cameraId);
            
            var response = await _httpClient.GetAsync($"/api/streams/{cameraId}/webrtc", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<StreamUrlResponse>>(_jsonOptions, cancellationToken);
            return apiResponse?.Data?.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WebRTC URL for camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<PtzMoveResponse> MovePtzAsync(Guid cameraId, PtzMoveRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Moving PTZ for camera {CameraId}, type: {MoveType}", cameraId, request.MoveType);
            
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/cameras/{cameraId}/ptz/move", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PtzMoveResponse>>(_jsonOptions, cancellationToken);
            
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                return apiResponse.Data;
            }

            return new PtzMoveResponse { IsMoving = false, ErrorMessage = apiResponse?.ErrorMessage };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving PTZ for camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<bool> StopPtzAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Stopping PTZ for camera {CameraId}", cameraId);
            
            var response = await _httpClient.PostAsync($"/api/cameras/{cameraId}/ptz/stop", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions, cancellationToken);
            return apiResponse?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping PTZ for camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<bool> GotoPtzPresetAsync(Guid cameraId, string presetToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Going to PTZ preset {PresetToken} for camera {CameraId}", presetToken, cameraId);
            
            var json = JsonSerializer.Serialize(new { presetToken }, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/cameras/{cameraId}/ptz/preset", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions, cancellationToken);
            return apiResponse?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error going to PTZ preset for camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking camera controller health");
            return false;
        }
    }
}
