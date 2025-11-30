using System.Net.Http.Json;
using System.Text.Json;
using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;
using CameraController.Contracts.Interfaces;
using CameraController.Contracts.Models;
using WebService.Configuration;

namespace WebService.Services;

/// <summary>
/// Background service that initializes camera monitoring on startup by fetching cameras from core service
/// </summary>
public class CoreSyncService : BackgroundService
{
    private readonly ILogger<CoreSyncService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ICameraService _cameraService;
    private readonly CoreServiceConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public CoreSyncService(
        ILogger<CoreSyncService> logger,
        IHttpClientFactory httpClientFactory,
        ICameraService cameraService,
        CoreServiceConfiguration config,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _cameraService = cameraService;
        _config = config;
        _applicationLifetime = applicationLifetime;

        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CoreSyncService starting - waiting to connect to core service at {BaseUrl}", _config.BaseUrl);
        
        // Wait for core service to be available
        if (!await WaitForCoreServiceAsync(stoppingToken))
        {
            _logger.LogCritical("Failed to connect to core service after {RetryAttempts} attempts. Camera-controller cannot function without core service. Shutting down application.", _config.RetryAttempts);
            _applicationLifetime.StopApplication();
            return;
        }

        _logger.LogInformation("Successfully connected to core service");

        // Fetch cameras from core service
        var cameras = await FetchCamerasFromCoreAsync(stoppingToken);
        
        if (cameras == null)
        {
            _logger.LogCritical("Failed to retrieve cameras from core service. Camera-controller cannot function without camera data. Shutting down application.");
            _applicationLifetime.StopApplication();
            return;
        }
        
        if (cameras.Count == 0)
        {
            _logger.LogInformation("No cameras found in core service. Camera-controller is ready.");
            return;
        }

        _logger.LogInformation("Found {Count} cameras in core service. Initializing monitoring...", cameras.Count);

        // Initialize monitoring for each camera
        foreach (var camera in cameras)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Startup canceled before all cameras could be initialized");
                break;
            }

            await InitializeCameraAsync(camera, stoppingToken);
        }

        _logger.LogInformation("Camera monitoring initialization complete. Camera-controller is ready.");
    }

    private async Task<bool> WaitForCoreServiceAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= _config.RetryAttempts; attempt++)
        {
            try
            {
                _logger.LogDebug("Attempting to connect to core service (attempt {Attempt}/{MaxAttempts})", attempt, _config.RetryAttempts);
                
                var response = await _httpClient.GetAsync("/api/CameraController/cameras", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully connected to core service on attempt {Attempt}", attempt);
                    return true;
                }
                
                _logger.LogWarning("Core service returned {StatusCode} on attempt {Attempt}", response.StatusCode, attempt);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning("Failed to connect to core service on attempt {Attempt}: {Message}", attempt, ex.Message);
            }

            if (attempt < _config.RetryAttempts)
            {
                _logger.LogDebug("Waiting {Delay} seconds before next attempt...", _config.RetryDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(_config.RetryDelaySeconds), cancellationToken);
            }
        }

        return false;
    }

    private async Task<List<CameraInitializationResponse>?> FetchCamerasFromCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching cameras from core service at {Url}", $"{_config.BaseUrl}/api/CameraController/cameras");
            
            var response = await _httpClient.GetAsync("/api/CameraController/cameras", cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<CameraInitializationResponse>>>(_jsonOptions, cancellationToken);
            
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                _logger.LogInformation("Successfully fetched {Count} cameras from core service", apiResponse.Data.Count);
                return apiResponse.Data;
            }

            _logger.LogWarning("Core service returned unsuccessful response: {Error}", apiResponse?.ErrorMessage);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch cameras from core service");
            return null;
        }
    }

    private async Task InitializeCameraAsync(CameraInitializationResponse cameraInit, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing camera {CameraId} ({Name})", cameraInit.Id, cameraInit.Name);

            // Check if camera already exists
            var existingCameras = _cameraService.GetAllCameras();
            if (existingCameras.ContainsKey(cameraInit.Id))
            {
                _logger.LogDebug("Camera {CameraId} already exists, skipping", cameraInit.Id);
                return;
            }

            // Create camera configuration with credentials from core service
            var camera = new Camera
            {
                Id = cameraInit.Id,
                Name = cameraInit.Name,
                Url = cameraInit.Url,
                Username = cameraInit.Username,
                Password = cameraInit.Password,
                Protocol = cameraInit.Protocol,
                Status = cameraInit.Status,
                CreatedAt = DateTime.UtcNow,
                DeviceInfo = cameraInit.DeviceInfo,
                Capabilities = cameraInit.Capabilities
            };

            // Add camera with monitoring enabled
            var monitoringConfig = new CameraMonitoringConfig
            {
                Enabled = true,
                HealthCheckInterval = TimeSpan.FromMinutes(1),
                HealthCheckTimeout = TimeSpan.FromSeconds(10),
                FailureThreshold = 3,
                SuccessThreshold = 2,
                AutoReconnect = true,
                MaxReconnectAttempts = 5,
                ReconnectDelay = TimeSpan.FromSeconds(30),
                PublishHealthEvents = true,
                PublishStatistics = false
            };

            await _cameraService.AddCameraAsync(camera, monitoringConfig);
            
            _logger.LogInformation("Successfully initialized camera {CameraId} ({Name})", cameraInit.Id, cameraInit.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize camera {CameraId} ({Name})", cameraInit.Id, cameraInit.Name);
        }
    }
}
