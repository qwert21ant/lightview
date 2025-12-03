using Lightview.Shared.Contracts;
using CameraController.Contracts.Interfaces;
using CameraController.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Lightview.Shared.Contracts.InternalApi;

namespace WebService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CamerasController : ControllerBase
{
    private readonly ILogger<CamerasController> _logger;
    private readonly ICameraService _cameraService;

    public CamerasController(ILogger<CamerasController> logger, ICameraService cameraService)
    {
        _logger = logger;
        _cameraService = cameraService;
    }

    [HttpGet]
    public ActionResult<ApiResponse<List<Camera>>> GetCameras()
    {
        _logger.LogDebug("Getting all cameras");
        var managedCameras = _cameraService.GetAllCameras();
        
        var response = managedCameras.Values.Select(monitoring => new Camera
        {
            Id = monitoring.Camera.Id,
            Name = monitoring.Camera.Configuration.Name,
            Url = monitoring.Camera.Configuration.Url,
            Username = monitoring.Camera.Configuration.Username,
            Password = monitoring.Camera.Configuration.Password,
            Protocol = monitoring.Camera.Configuration.Protocol,
            Status = monitoring.Camera.Status,
            CreatedAt = monitoring.Camera.Configuration.CreatedAt,
            LastConnectedAt = monitoring.Camera.Configuration.LastConnectedAt,
            DeviceInfo = monitoring.Camera.Configuration.DeviceInfo,
            Capabilities = monitoring.Camera.Capabilities,
            Profiles = monitoring.Camera.Profiles.ToList()
        }).ToList();

        return Ok(new ApiResponse<List<Camera>>
        {
            Success = true,
            Data = response
        });
    }

    [HttpGet("{id}")]
    public ActionResult<ApiResponse<Camera>> GetCamera(Guid id)
    {
        _logger.LogDebug("Getting camera {CameraId}", id);
        var monitoring = _cameraService.GetCamera(id);
        if (monitoring == null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera not found",
                ErrorCode = "CAMERA_NOT_FOUND"
            });
        }

        var response = new Camera
        {
            Id = monitoring.Camera.Id,
            Name = monitoring.Camera.Configuration.Name,
            Url = monitoring.Camera.Configuration.Url,
            Username = monitoring.Camera.Configuration.Username,
            Password = monitoring.Camera.Configuration.Password,
            Protocol = monitoring.Camera.Configuration.Protocol,
            Status = monitoring.Camera.Status,
            CreatedAt = monitoring.Camera.Configuration.CreatedAt,
            LastConnectedAt = monitoring.Camera.Configuration.LastConnectedAt,
            DeviceInfo = monitoring.Camera.Configuration.DeviceInfo,
            Capabilities = monitoring.Camera.Capabilities,
            Profiles = monitoring.Camera.Profiles.ToList()
        };

        return Ok(new ApiResponse<Camera>
        {
            Success = true,
            Data = response
        });
    }

    [HttpPost("{id}")]
    public async Task<ActionResult<ApiResponse<Camera>>> CreateCamera(Guid id, [FromBody] AddCameraRequest request)
    {
        _logger.LogInformation("Creating new camera {CameraName} with URL {Url}", request.Name, request.Url);
        try
        {
            var camera = new Camera
            {
                Id = id,
                Name = request.Name,
                Url = request.Url,
                Username = request.Username,
                Password = request.Password,
                Protocol = request.Protocol,
                Status = CameraStatus.Offline,
                CreatedAt = DateTime.UtcNow
            };

            CameraMonitoringConfig? monitoringConfig = null;
            if (request.MonitoringConfig != null)
            {
                monitoringConfig = new CameraMonitoringConfig
                {
                    Enabled = request.MonitoringConfig.Enabled,
                    HealthCheckInterval = request.MonitoringConfig.HealthCheckInterval,
                    HealthCheckTimeout = request.MonitoringConfig.HealthCheckTimeout,
                    FailureThreshold = request.MonitoringConfig.FailureThreshold,
                    SuccessThreshold = request.MonitoringConfig.SuccessThreshold,
                    AutoReconnect = request.MonitoringConfig.AutoReconnect,
                    MaxReconnectAttempts = request.MonitoringConfig.MaxReconnectAttempts,
                    ReconnectDelay = request.MonitoringConfig.ReconnectDelay,
                    PublishHealthEvents = request.MonitoringConfig.PublishHealthEvents,
                    PublishStatistics = request.MonitoringConfig.PublishStatistics
                };
            }
            
            var monitoring = await _cameraService.AddCameraAsync(camera, monitoringConfig);

            var response = new Camera
            {
                Id = monitoring.Camera.Id,
                Name = monitoring.Camera.Configuration.Name,
                Url = monitoring.Camera.Configuration.Url,
                Username = monitoring.Camera.Configuration.Username,
                Password = monitoring.Camera.Configuration.Password,
                Protocol = monitoring.Camera.Configuration.Protocol,
                Status = monitoring.Camera.Status,
                CreatedAt = monitoring.Camera.Configuration.CreatedAt,
                LastConnectedAt = monitoring.Camera.Configuration.LastConnectedAt,
                DeviceInfo = monitoring.Camera.Configuration.DeviceInfo,
                Capabilities = monitoring.Camera.Capabilities,
                Profiles = monitoring.Camera.Profiles.ToList()
            };

            return CreatedAtAction(nameof(GetCamera), 
                new { id = id }, 
                new ApiResponse<Camera>
                {
                    Success = true,
                    Data = response
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create camera {CameraName}", request.Name);
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "CAMERA_CREATION_FAILED"
            });
        }
    }

    [HttpPost("{id}/ptz/move")]
    public async Task<ActionResult<ApiResponse<PtzMoveResponse>>> MovePtz(Guid id, [FromBody] PtzMoveRequest request)
    {
        _logger.LogInformation("PTZ move request for camera {CameraId}, MoveType: {MoveType}", id, request.MoveType);
        var monitoring = _cameraService.GetCamera(id);
        if (monitoring == null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera not found"
            });
        }

        var ptzControl = monitoring.Camera.PtzControl;
        if (ptzControl == null || !ptzControl.IsSupported)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera does not support PTZ"
            });
        }

        try
        {
            bool moveResult = false;
            PtzPosition? newPosition = null;

            switch (request.MoveType)
            {
                case PtzMoveType.Absolute:
                    if (request.AbsolutePosition != null)
                    {
                        moveResult = await ptzControl.MoveAbsoluteAsync(request.AbsolutePosition, request.Speed);
                        newPosition = await ptzControl.GetPositionAsync();
                    }
                    break;
                case PtzMoveType.Relative:
                    if (request.RelativeMovement != null)
                    {
                        moveResult = await ptzControl.MoveRelativeAsync(request.RelativeMovement, request.Speed);
                        newPosition = await ptzControl.GetPositionAsync();
                    }
                    break;
                case PtzMoveType.Continuous:
                    if (request.ContinuousSpeed != null)
                    {
                        moveResult = await ptzControl.MoveContinuousAsync(request.ContinuousSpeed);
                    }
                    break;
                case PtzMoveType.Stop:
                    moveResult = await ptzControl.StopMovementAsync();
                    newPosition = await ptzControl.GetPositionAsync();
                    break;
            }

            var response = new PtzMoveResponse
            {
                NewPosition = newPosition ?? ptzControl.CurrentPosition ?? new PtzPosition(),
                IsMoving = request.MoveType == PtzMoveType.Continuous,
                ErrorMessage = moveResult ? null : "PTZ move operation failed"
            };

            return Ok(new ApiResponse<PtzMoveResponse>
            {
                Success = moveResult,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move PTZ for camera {CameraId}", id);
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "PTZ_MOVE_FAILED"
            });
        }
    }

    [HttpPost("{id}/connect")]
    public async Task<ActionResult<ApiResponse>> ConnectCamera(Guid id)
    {
        _logger.LogInformation("Connect request for camera {CameraId}", id);
        try
        {
            var result = await _cameraService.ConnectCameraAsync(id);
            if (!result)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to connect to camera",
                    ErrorCode = "CAMERA_CONNECTION_FAILED"
                });
            }

            return Ok(new ApiResponse
            {
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect camera {CameraId}", id);
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "CAMERA_CONNECTION_FAILED"
            });
        }
    }

    [HttpPost("{id}/disconnect")]
    public async Task<ActionResult<ApiResponse>> DisconnectCamera(Guid id)
    {
        _logger.LogInformation("Disconnect request for camera {CameraId}", id);
        try
        {
            var result = await _cameraService.DisconnectCameraAsync(id);
            if (!result)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to disconnect from camera",
                    ErrorCode = "CAMERA_DISCONNECTION_FAILED"
                });
            }

            return Ok(new ApiResponse
            {
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect camera {CameraId}", id);
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "CAMERA_DISCONNECTION_FAILED"
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteCamera(Guid id)
    {
        _logger.LogInformation("Delete request for camera {CameraId}", id);
        try
        {
            var result = await _cameraService.RemoveCameraAsync(id);
            if (!result)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    ErrorMessage = "Camera not found",
                    ErrorCode = "CAMERA_NOT_FOUND"
                });
            }

            return Ok(new ApiResponse
            {
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete camera {CameraId}", id);
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "CAMERA_DELETE_FAILED"
            });
        }
    }

    [HttpGet("{id}/health")]
    public async Task<ActionResult<ApiResponse<CameraHealthStatus>>> GetCameraHealth(Guid id)
    {
        _logger.LogDebug("Getting health status for camera {CameraId}", id);
        try
        {
            var health = await _cameraService.GetCameraHealthAsync(id);
            if (health == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    ErrorMessage = "Camera not found",
                    ErrorCode = "CAMERA_NOT_FOUND"
                });
            }

            return Ok(new ApiResponse<CameraHealthStatus>
            {
                Success = true,
                Data = health
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health for camera {CameraId}", id);
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "HEALTH_CHECK_FAILED"
            });
        }
    }

    [HttpGet("{id}/ptz/presets")]
    public async Task<ActionResult<ApiResponse<List<PtzPreset>>>> GetPtzPresets(Guid id)
    {
        _logger.LogDebug("Getting PTZ presets for camera {CameraId}", id);
        var monitoring = _cameraService.GetCamera(id);
        if (monitoring == null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera not found"
            });
        }

        var ptzControl = monitoring.Camera.PtzControl;
        if (ptzControl == null || !ptzControl.IsSupported)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera does not support PTZ"
            });
        }

        try
        {
            var presets = await ptzControl.GetPresetsAsync();
            return Ok(new ApiResponse<List<PtzPreset>>
            {
                Success = true,
                Data = presets
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PTZ presets for camera {CameraId}", id);
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "PTZ_PRESETS_FAILED"
            });
        }
    }

    [HttpPost("{id}/ptz/presets/{presetId}/goto")]
    public async Task<ActionResult<ApiResponse>> GotoPtzPreset(Guid id, int presetId)
    {
        _logger.LogInformation("Going to PTZ preset {PresetId} for camera {CameraId}", presetId, id);
        var monitoring = _cameraService.GetCamera(id);
        if (monitoring == null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera not found"
            });
        }

        var ptzControl = monitoring.Camera.PtzControl;
        if (ptzControl == null || !ptzControl.IsSupported)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera does not support PTZ"
            });
        }

        try
        {
            var result = await ptzControl.GotoPresetAsync(presetId);
            return Ok(new ApiResponse
            {
                Success = result,
                ErrorMessage = result ? null : "Failed to go to preset"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to go to PTZ preset {PresetId} for camera {CameraId}", presetId, id);
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "PTZ_GOTO_PRESET_FAILED"
            });
        }
    }

    [HttpPost("{id}/ptz/presets")]
    public async Task<ActionResult<ApiResponse>> SetPtzPreset(Guid id, [FromBody] PtzPresetRequest request)
    {
        _logger.LogInformation("Setting PTZ preset '{PresetName}' for camera {CameraId}", request.Name, id);
        var monitoring = _cameraService.GetCamera(id);
        if (monitoring == null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera not found"
            });
        }

        var ptzControl = monitoring.Camera.PtzControl;
        if (ptzControl == null || !ptzControl.IsSupported)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera does not support PTZ"
            });
        }

        try
        {
            var result = await ptzControl.SetPresetAsync(request.Name);
            return Ok(new ApiResponse
            {
                Success = result,
                ErrorMessage = result ? null : "Failed to set preset"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set PTZ preset {PresetName} for camera {CameraId}", request.Name, id);
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "PTZ_SET_PRESET_FAILED"
            });
        }
    }

    [HttpDelete("{id}/ptz/presets/{presetId}")]
    public async Task<ActionResult<ApiResponse>> DeletePtzPreset(Guid id, int presetId)
    {
        _logger.LogInformation("Deleting PTZ preset {PresetId} for camera {CameraId}", presetId, id);
        var monitoring = _cameraService.GetCamera(id);
        if (monitoring == null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera not found"
            });
        }

        var ptzControl = monitoring.Camera.PtzControl;
        if (ptzControl == null || !ptzControl.IsSupported)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = "Camera does not support PTZ"
            });
        }

        try
        {
            var result = await ptzControl.RemovePresetAsync(presetId);
            return Ok(new ApiResponse
            {
                Success = result,
                ErrorMessage = result ? null : "Failed to remove preset"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove PTZ preset {PresetId} for camera {CameraId}", presetId, id);
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "PTZ_REMOVE_PRESET_FAILED"
            });
        }
    }

    [HttpGet("service/health")]
    public async Task<ActionResult<ApiResponse<ServiceHealthSummary>>> GetServiceHealth()
    {
        _logger.LogDebug("Getting service health status");
        try
        {
            var health = await _cameraService.GetServiceHealthAsync();
            return Ok(new ApiResponse<ServiceHealthSummary>
            {
                Success = true,
                Data = health
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service health");
            return BadRequest(new ApiResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "SERVICE_HEALTH_CHECK_FAILED"
            });
        }
    }
}