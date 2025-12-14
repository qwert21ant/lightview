using Lightview.Shared.Contracts.InternalApi;
using Lightview.Shared.Contracts;
using CameraController.Contracts.Interfaces;
using WebService.Configuration;
using RabbitMQShared.Interfaces;
using CoreConnector;

namespace WebService.Services;

/// <summary>
/// Background service that initializes camera monitoring on startup by fetching cameras from core service
/// </summary>
public class CoreSyncService : IInitializable
{
    private readonly ILogger<CoreSyncService> _logger;
    private readonly ICoreConnector _coreConnector;
    private readonly ICameraService _cameraService;
    private readonly CoreServiceConfiguration _config;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ISettingsService _settingsService;

    public CoreSyncService(
        ILogger<CoreSyncService> logger,
        ICoreConnector coreConnector,
        ICameraService cameraService,
        CoreServiceConfiguration config,
        ISettingsService settingsService,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _coreConnector = coreConnector;
        _cameraService = cameraService;
        _config = config;
        _settingsService = settingsService;
        _applicationLifetime = applicationLifetime;
    }

    public string ServiceName => "Core Sync Service";

    public async Task InitializeAsync(CancellationToken stoppingToken = default)
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

        // Fetch and cache camera monitoring settings
        var settings = await _coreConnector.GetCameraMonitoringSettingsAsync(stoppingToken);
        if (settings == null)
        {
            _logger.LogCritical("Failed to retrieve camera monitoring settings from core service. Camera-controller cannot function without these settings. Shutting down application.");
            _applicationLifetime.StopApplication();
            return;
        }
        _settingsService.CameraMonitoringSettings = settings;
        _logger.LogInformation("Successfully fetched and cached camera monitoring settings");

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
                
                var healthy = await _coreConnector.IsHealthyAsync(cancellationToken);
                if (healthy)
                {
                    _logger.LogInformation("Successfully connected to core service on attempt {Attempt}", attempt);
                    return true;
                }
                _logger.LogWarning("Core service is not healthy on attempt {Attempt}", attempt);
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
        var cameras = await _coreConnector.GetCamerasAsync(cancellationToken);
        if (cameras != null)
        {
            _logger.LogInformation("Successfully fetched {Count} cameras from core service", cameras.Count);
        }
        else
        {
            _logger.LogWarning("Core service returned no cameras or an error occurred");
        }
        return cameras;
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

            // Add camera; monitoring configuration is applied internally using cached settings
            await _cameraService.AddCameraAsync(camera);
            
            // Auto-start monitoring and connection for cameras that were not offline
            if (cameraInit.Status != CameraStatus.Offline)
            {
                _logger.LogInformation("Auto-connecting camera {CameraId} ({Name}) with status {Status}", 
                    cameraInit.Id, cameraInit.Name, cameraInit.Status);

                try
                {
                    var connected = await _cameraService.ConnectCameraAsync(cameraInit.Id);
                    if (connected)
                    {
                        _logger.LogInformation("Successfully auto-connected camera {CameraId} during startup", cameraInit.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Auto-connection failed for camera {CameraId} during startup", cameraInit.Id);
                    }
                }
                catch (Exception connectEx)
                {
                    _logger.LogError(connectEx, "Exception during auto-connection of camera {CameraId}", cameraInit.Id);
                }
            }
            else
            {
                _logger.LogDebug("Camera {CameraId} has Offline status, skipping auto-connection", cameraInit.Id);
            }
            
            _logger.LogInformation("Successfully initialized camera {CameraId} ({Name})", cameraInit.Id, cameraInit.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize camera {CameraId} ({Name})", cameraInit.Id, cameraInit.Name);
        }
    }
}
