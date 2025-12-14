using System.Net.Http.Json;
using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;
using Lightview.Shared.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace CoreConnector;

public class CoreConnectorService : ICoreConnector
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoreConnectorService> _logger;

    public CoreConnectorService(HttpClient httpClient, ILogger<CoreConnectorService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/CameraController/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Core health check failed");
            return false;
        }
    }

    public async Task<List<CameraInitializationResponse>?> GetCamerasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/CameraController/cameras", cancellationToken);
            response.EnsureSuccessStatusCode();

            var cameras = await response.Content.ReadFromJsonAsync<List<CameraInitializationResponse>>(cancellationToken: cancellationToken);
            return cameras;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch cameras from core");
            return null;
        }
    }

    public async Task<CameraMonitoringSettings?> GetCameraMonitoringSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/CameraController/settings", cancellationToken);
            response.EnsureSuccessStatusCode();

            var settings = await response.Content.ReadFromJsonAsync<CameraMonitoringSettings>(cancellationToken: cancellationToken);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch camera monitoring settings from core");
            return null;
        }
    }
}
