using CameraManager.Interfaces;
using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;
using Microsoft.AspNetCore.Mvc;
using WebService.Authentication;

namespace WebService.Controllers;

[Host("*:5001")]  // Bind this controller to the camera-controller port
[ApiController]
[Route("api/[controller]")]
[ApiKeyAuthorization]  // Require API key authentication for camera controller endpoints
[EndpointGroupName("internal")]
public class CameraControllerController : ControllerBase
{
    private readonly ICameraService _cameraService;
    private readonly ILogger<CameraControllerController> _logger;

    public CameraControllerController(ICameraService cameraService, ILogger<CameraControllerController> logger)
    {
        _cameraService = cameraService;
        _logger = logger;
    }

    /// <summary>
    /// Get all cameras from persistence with credentials for camera-controller initialization
    /// </summary>
    [HttpGet("cameras")]
    public async Task<ActionResult<ApiResponse<List<CameraInitializationResponse>>>> GetAllCameras()
    {
        try
        {
            _logger.LogDebug("Fetching all cameras from persistence for camera-controller");
            
            var cameras = await _cameraService.GetAllCamerasAsync();
            
            var cameraInitializationList = cameras.Select(camera => new CameraInitializationResponse
            {
                Id = camera.Id,
                Name = camera.Name,
                Url = camera.Url,
                Username = camera.Username,
                Password = camera.Password,
                Protocol = camera.Protocol,
                Status = camera.Status,
                IsMonitoring = false, // Will be determined by camera-controller
                Health = null, // Not available from persistence
                LastConnectedAt = camera.LastConnectedAt != default ? camera.LastConnectedAt : null,
                DeviceInfo = camera.DeviceInfo,
                Capabilities = camera.Capabilities,
                ProfileCount = camera.Profiles?.Count ?? 0
            }).ToList();

            _logger.LogInformation("Retrieved {Count} cameras from persistence for camera-controller", cameraInitializationList.Count);

            return Ok(new ApiResponse<List<CameraInitializationResponse>>
            {
                Success = true,
                Data = cameraInitializationList,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve cameras from persistence");
            return StatusCode(500, new ApiResponse<List<CameraInitializationResponse>>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve cameras from persistence",
                ErrorCode = "PERSISTENCE_ERROR",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
