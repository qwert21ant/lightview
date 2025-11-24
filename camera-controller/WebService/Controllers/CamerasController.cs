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
    public ActionResult<ApiResponse<List<CameraStatusResponse>>> GetCameras()
    {
        var managedCameras = _cameraService.GetAllCameras();
        
        var response = managedCameras.Values.Select(monitoring => new CameraStatusResponse
        {
            Id = monitoring.Camera.Id,
            Name = monitoring.Camera.Configuration.Name,
            Url = monitoring.Camera.Configuration.Url,
            Status = monitoring.Camera.Status,
            IsMonitoring = monitoring.IsMonitoring,
            Health = monitoring.LastHealthStatus,
            LastConnectedAt = monitoring.Camera.Configuration.LastConnectedAt,
            DeviceInfo = monitoring.Camera.Configuration.DeviceInfo,
            Capabilities = monitoring.Camera.Capabilities,
            ProfileCount = monitoring.Camera.Profiles.Count
        }).ToList();

        return Ok(new ApiResponse<List<CameraStatusResponse>>
        {
            Success = true,
            Data = response
        });
    }

    [HttpGet("{id}")]
    public ActionResult<ApiResponse<CameraStatusResponse>> GetCamera(Guid id)
    {
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

        var response = new CameraStatusResponse
        {
            Id = monitoring.Camera.Id,
            Name = monitoring.Camera.Configuration.Name,
            Url = monitoring.Camera.Configuration.Url,
            Status = monitoring.Camera.Status,
            IsMonitoring = monitoring.IsMonitoring,
            Health = monitoring.LastHealthStatus,
            LastConnectedAt = monitoring.Camera.Configuration.LastConnectedAt,
            DeviceInfo = monitoring.Camera.Configuration.DeviceInfo,
            Capabilities = monitoring.Camera.Capabilities,
            ProfileCount = monitoring.Camera.Profiles.Count
        };

        return Ok(new ApiResponse<CameraStatusResponse>
        {
            Success = true,
            Data = response
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CameraStatusResponse>>> CreateCamera([FromBody] AddCameraRequest request)
    {
        try
        {
            var camera = new Camera
            {
                Id = Guid.NewGuid(),
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

            var response = new CameraStatusResponse
            {
                Id = monitoring.Camera.Id,
                Name = monitoring.Camera.Configuration.Name,
                Url = monitoring.Camera.Configuration.Url,
                Status = monitoring.Camera.Status,
                IsMonitoring = monitoring.IsMonitoring,
                Health = monitoring.LastHealthStatus,
                Capabilities = monitoring.Camera.Capabilities,
                ProfileCount = monitoring.Camera.Profiles.Count
            };

            return CreatedAtAction(nameof(GetCamera), 
                new { id = camera.Id }, 
                new ApiResponse<CameraStatusResponse>
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

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteCamera(Guid id)
    {
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