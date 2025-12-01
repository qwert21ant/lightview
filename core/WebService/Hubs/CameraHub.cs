using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using CameraManager.Interfaces;
using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;

namespace WebService.Hubs;

[Authorize] // Require authentication for all hub methods
public class CameraHub : Hub
{
    private readonly ICameraService _cameraService;
    private readonly ILogger<CameraHub> _logger;

    public CameraHub(ICameraService cameraService, ILogger<CameraHub> logger)
    {
        _cameraService = cameraService;
        _logger = logger;
    }

    // Connection management
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to CameraHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from CameraHub: {ConnectionId}", Context.ConnectionId);
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error");
        }
        await base.OnDisconnectedAsync(exception);
    }

    // CRUD Operations
    public async Task<List<Camera>> GetAllCameras()
    {
        try
        {
            _logger.LogDebug("GetAllCameras called by {ConnectionId}", Context.ConnectionId);
            return await _cameraService.GetAllCamerasAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all cameras");
            throw new HubException("Failed to retrieve cameras");
        }
    }

    public async Task<Camera?> GetCamera(string cameraId)
    {
        try
        {
            if (!Guid.TryParse(cameraId, out var id))
            {
                throw new HubException("Invalid camera ID format");
            }

            _logger.LogDebug("GetCamera called for {CameraId} by {ConnectionId}", cameraId, Context.ConnectionId);
            return await _cameraService.GetCameraByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camera {CameraId}", cameraId);
            throw new HubException($"Failed to retrieve camera {cameraId}");
        }
    }

    public async Task<Camera> AddCamera(AddCameraRequest request)
    {
        try
        {
            _logger.LogInformation("AddCamera called by {ConnectionId}: {CameraName}", Context.ConnectionId, request.Name);
            var camera = await _cameraService.AddCameraAsync(request);
            
            // Notify all clients about new camera
            await Clients.All.SendAsync("CameraAdded", camera);
            return camera;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding camera {CameraName}", request.Name);
            throw new HubException("Failed to add camera");
        }
    }

    public async Task<Camera> UpdateCamera(string cameraId, Camera camera)
    {
        try
        {
            if (!Guid.TryParse(cameraId, out var id))
            {
                throw new HubException("Invalid camera ID format");
            }

            _logger.LogInformation("UpdateCamera called for {CameraId} by {ConnectionId}", cameraId, Context.ConnectionId);
            var updatedCamera = await _cameraService.UpdateCameraAsync(id, camera);
            
            // Notify all clients about camera update
            await Clients.All.SendAsync("CameraUpdated", updatedCamera);
            return updatedCamera;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating camera {CameraId}", cameraId);
            throw new HubException($"Failed to update camera {cameraId}");
        }
    }

    public async Task<bool> DeleteCamera(string cameraId)
    {
        try
        {
            if (!Guid.TryParse(cameraId, out var id))
            {
                throw new HubException("Invalid camera ID format");
            }

            _logger.LogInformation("DeleteCamera called for {CameraId} by {ConnectionId}", cameraId, Context.ConnectionId);
            var result = await _cameraService.DeleteCameraAsync(id);
            
            if (result)
            {
                // Notify all clients about camera deletion
                await Clients.All.SendAsync("CameraDeleted", cameraId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting camera {CameraId}", cameraId);
            throw new HubException($"Failed to delete camera {cameraId}");
        }
    }

    // Camera Control Operations
    public async Task<bool> ConnectCamera(string cameraId)
    {
        try
        {
            if (!Guid.TryParse(cameraId, out var id))
            {
                throw new HubException("Invalid camera ID format");
            }

            _logger.LogInformation("ConnectCamera called for {CameraId} by {ConnectionId}", cameraId, Context.ConnectionId);
            var result = await _cameraService.ConnectCameraAsync(id);
            
            if (result)
            {
                await Clients.All.SendAsync("CameraConnected", cameraId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting camera {CameraId}", cameraId);
            throw new HubException($"Failed to connect camera {cameraId}");
        }
    }

    public async Task<bool> DisconnectCamera(string cameraId)
    {
        try
        {
            if (!Guid.TryParse(cameraId, out var id))
            {
                throw new HubException("Invalid camera ID format");
            }

            _logger.LogInformation("DisconnectCamera called for {CameraId} by {ConnectionId}", cameraId, Context.ConnectionId);
            var result = await _cameraService.DisconnectCameraAsync(id);
            
            if (result)
            {
                await Clients.All.SendAsync("CameraDisconnected", cameraId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting camera {CameraId}", cameraId);
            throw new HubException($"Failed to disconnect camera {cameraId}");
        }
    }

    public async Task<CameraStatusResponse?> GetCameraStatus(string cameraId)
    {
        try
        {
            if (!Guid.TryParse(cameraId, out var id))
            {
                throw new HubException("Invalid camera ID format");
            }

            _logger.LogDebug("GetCameraStatus called for {CameraId} by {ConnectionId}", cameraId, Context.ConnectionId);
            return await _cameraService.GetCameraStatusAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camera status {CameraId}", cameraId);
            throw new HubException($"Failed to get camera status {cameraId}");
        }
    }

    public async Task<StreamUrlResponse?> GetStreamUrl(string cameraId, string? profileToken = null)
    {
        try
        {
            if (!Guid.TryParse(cameraId, out var id))
            {
                throw new HubException("Invalid camera ID format");
            }

            _logger.LogDebug("GetStreamUrl called for {CameraId} by {ConnectionId}", cameraId, Context.ConnectionId);
            return await _cameraService.GetStreamUrlAsync(id, profileToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stream URL for camera {CameraId}", cameraId);
            throw new HubException($"Failed to get stream URL for camera {cameraId}");
        }
    }

    // PTZ Operations
    public async Task<PtzMoveResponse?> MovePtz(string cameraId, PtzMoveRequest request)
    {
        try
        {
            if (!Guid.TryParse(cameraId, out var id))
            {
                throw new HubException("Invalid camera ID format");
            }

            _logger.LogDebug("MovePtz called for {CameraId} by {ConnectionId}: {MoveType}", 
                cameraId, Context.ConnectionId, request.MoveType);
            
            var result = await _cameraService.MovePtzAsync(id, request);
            
            if (result != null && string.IsNullOrEmpty(result.ErrorMessage))
            {
                // Notify all clients about PTZ movement
                await Clients.All.SendAsync("PtzMoved", cameraId, request.MoveType.ToString(), result);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving PTZ for camera {CameraId}", cameraId);
            throw new HubException($"Failed to move PTZ for camera {cameraId}");
        }
    }

    public async Task<bool> StopPtz(string cameraId)
    {
        try
        {
            if (!Guid.TryParse(cameraId, out var id))
            {
                throw new HubException("Invalid camera ID format");
            }

            _logger.LogDebug("StopPtz called for {CameraId} by {ConnectionId}", cameraId, Context.ConnectionId);
            var result = await _cameraService.StopPtzAsync(id);
            
            if (result)
            {
                await Clients.All.SendAsync("PtzStopped", cameraId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping PTZ for camera {CameraId}", cameraId);
            throw new HubException($"Failed to stop PTZ for camera {cameraId}");
        }
    }

    // Convenience PTZ methods for simple directional movement
    public async Task<PtzMoveResponse?> MovePtzUp(string cameraId, float speed = 0.5f)
    {
        var request = new PtzMoveRequest
        {
            MoveType = PtzMoveType.Continuous,
            ContinuousSpeed = new PtzSpeed { Pan = 0, Tilt = speed, Zoom = 0 }
        };
        return await MovePtz(cameraId, request);
    }

    public async Task<PtzMoveResponse?> MovePtzDown(string cameraId, float speed = 0.5f)
    {
        var request = new PtzMoveRequest
        {
            MoveType = PtzMoveType.Continuous,
            ContinuousSpeed = new PtzSpeed { Pan = 0, Tilt = -speed, Zoom = 0 }
        };
        return await MovePtz(cameraId, request);
    }

    public async Task<PtzMoveResponse?> MovePtzLeft(string cameraId, float speed = 0.5f)
    {
        var request = new PtzMoveRequest
        {
            MoveType = PtzMoveType.Continuous,
            ContinuousSpeed = new PtzSpeed { Pan = -speed, Tilt = 0, Zoom = 0 }
        };
        return await MovePtz(cameraId, request);
    }

    public async Task<PtzMoveResponse?> MovePtzRight(string cameraId, float speed = 0.5f)
    {
        var request = new PtzMoveRequest
        {
            MoveType = PtzMoveType.Continuous,
            ContinuousSpeed = new PtzSpeed { Pan = speed, Tilt = 0, Zoom = 0 }
        };
        return await MovePtz(cameraId, request);
    }

    public async Task<PtzMoveResponse?> ZoomIn(string cameraId, float speed = 0.5f)
    {
        var request = new PtzMoveRequest
        {
            MoveType = PtzMoveType.Continuous,
            ContinuousSpeed = new PtzSpeed { Pan = 0, Tilt = 0, Zoom = speed }
        };
        return await MovePtz(cameraId, request);
    }

    public async Task<PtzMoveResponse?> ZoomOut(string cameraId, float speed = 0.5f)
    {
        var request = new PtzMoveRequest
        {
            MoveType = PtzMoveType.Continuous,
            ContinuousSpeed = new PtzSpeed { Pan = 0, Tilt = 0, Zoom = -speed }
        };
        return await MovePtz(cameraId, request);
    }

    // Group operations for multiple clients
    public async Task JoinCameraGroup(string cameraId)
    {
        if (!Guid.TryParse(cameraId, out var _))
        {
            throw new HubException("Invalid camera ID format");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"camera_{cameraId}");
        _logger.LogDebug("Client {ConnectionId} joined camera group {CameraId}", Context.ConnectionId, cameraId);
    }

    public async Task LeaveCameraGroup(string cameraId)
    {
        if (!Guid.TryParse(cameraId, out var _))
        {
            throw new HubException("Invalid camera ID format");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"camera_{cameraId}");
        _logger.LogDebug("Client {ConnectionId} left camera group {CameraId}", Context.ConnectionId, cameraId);
    }

    // Send notifications to specific camera group
    public async Task NotifyCameraGroup(string cameraId, string eventType, object data)
    {
        await Clients.Group($"camera_{cameraId}").SendAsync("CameraEvent", cameraId, eventType, data);
    }
}
