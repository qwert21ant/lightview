using CameraManager.Interfaces;
using CameraManager.Mappers;
using CameraControllerConnector.Interfaces;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;
using SharedCamera = Lightview.Shared.Contracts.Camera;

namespace CameraManager.Services;

public class CameraService : ICameraService
{
    private readonly AppDbContext _dbContext;
    private readonly ICameraControllerClient _cameraControllerClient;
    private readonly ILogger<CameraService> _logger;

    public CameraService(
        AppDbContext dbContext,
        ICameraControllerClient cameraControllerClient,
        ILogger<CameraService> logger)
    {
        _dbContext = dbContext;
        _cameraControllerClient = cameraControllerClient;
        _logger = logger;
    }

    public async Task<List<SharedCamera>> GetAllCamerasAsync()
    {
        var persistenceCameras = await _dbContext.Cameras.ToListAsync();
        return persistenceCameras.Select(c => c.ToSharedCamera()).ToList();
    }

    public async Task<SharedCamera?> GetCameraByIdAsync(Guid id)
    {
        var persistenceCamera = await _dbContext.Cameras.FindAsync(id);
        return persistenceCamera?.ToSharedCamera();
    }

    public async Task<SharedCamera> AddCameraAsync(AddCameraRequest request)
    {
        var sharedCamera = new SharedCamera
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Url = request.Url,
            Username = request.Username,
            Password = request.Password,
            Protocol = request.Protocol,
            CreatedAt = DateTime.UtcNow,
            LastConnectedAt = DateTime.MinValue
        };

        var persistenceCamera = sharedCamera.ToPersistenceCamera();
        _dbContext.Cameras.Add(persistenceCamera);
        await _dbContext.SaveChangesAsync();

        // Add camera to camera-controller
        try
        {
            await _cameraControllerClient.AddCameraAsync(sharedCamera.Id, request);
            _logger.LogInformation("Camera {CameraId} added to camera-controller", sharedCamera.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add camera {CameraId} to camera-controller", sharedCamera.Id);
        }

        return sharedCamera;
    }

    public async Task<SharedCamera> UpdateCameraAsync(Guid id, SharedCamera updatedCamera)
    {
        var persistenceCamera = await _dbContext.Cameras.FindAsync(id);
        if (persistenceCamera == null)
            throw new ArgumentException($"Camera with ID {id} not found");

        persistenceCamera.UpdatePersistenceCamera(updatedCamera);
        await _dbContext.SaveChangesAsync();

        // Update camera in camera-controller (create UpdateCameraRequest from SharedCamera)
        try
        {
            var updateRequest = new UpdateCameraRequest
            {
                Name = updatedCamera.Name,
                Url = updatedCamera.Url,
                Username = updatedCamera.Username,
                Password = updatedCamera.Password,
                Protocol = updatedCamera.Protocol
            };
            await _cameraControllerClient.UpdateCameraAsync(id, updateRequest);
            _logger.LogInformation("Camera {CameraId} updated in camera-controller", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update camera {CameraId} in camera-controller", id);
        }

        return persistenceCamera.ToSharedCamera();
    }

    /// <summary>
    /// Update camera in persistence only, without calling camera-controller
    /// Used by event handlers to avoid circular updates
    /// </summary>
    public async Task UpdateCameraPersistenceOnlyAsync(Guid id, SharedCamera updatedCamera)
    {
        var persistenceCamera = await _dbContext.Cameras.FindAsync(id);
        if (persistenceCamera == null)
        {
            _logger.LogWarning("Camera {CameraId} not found in persistence for update", id);
            return;
        }

        persistenceCamera.UpdatePersistenceCamera(updatedCamera);
        await _dbContext.SaveChangesAsync();
        _logger.LogDebug("Camera {CameraId} updated in persistence only", id);
    }

    public async Task<bool> DeleteCameraAsync(Guid id)
    {
        var camera = await _dbContext.Cameras.FindAsync(id);
        if (camera == null)
            return false;

        _dbContext.Cameras.Remove(camera);
        await _dbContext.SaveChangesAsync();

        // Remove camera from camera-controller
        try
        {
            await _cameraControllerClient.RemoveCameraAsync(id);
            _logger.LogInformation("Camera {CameraId} removed from camera-controller", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove camera {CameraId} from camera-controller", id);
        }

        return true;
    }

    public async Task<CameraStatusResponse?> GetCameraStatusAsync(Guid id)
    {
        try
        {
            return await _cameraControllerClient.GetCameraAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get camera {CameraId} status from camera-controller", id);
            return null;
        }
    }

    public async Task<bool> ConnectCameraAsync(Guid id)
    {
        try
        {
            var result = await _cameraControllerClient.ConnectCameraAsync(id);
            
            // Update LastConnectedAt if successful
            if (result)
            {
                var camera = await _dbContext.Cameras.FindAsync(id);
                if (camera != null)
                {
                    camera.LastConnectedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect camera {CameraId}", id);
            return false;
        }
    }

    public async Task<bool> DisconnectCameraAsync(Guid id)
    {
        try
        {
            return await _cameraControllerClient.DisconnectCameraAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect camera {CameraId}", id);
            return false;
        }
    }

    public async Task<StreamUrlResponse?> GetStreamUrlAsync(Guid id, string? profileToken = null)
    {
        try
        {
            var streamUrl = await _cameraControllerClient.GetWebRtcStreamUrlAsync(id);
            return streamUrl != null ? new StreamUrlResponse { Url = streamUrl, Protocol = "WebRTC" } : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stream URL for camera {CameraId}", id);
            return null;
        }
    }

    public async Task<PtzMoveResponse?> MovePtzAsync(Guid id, PtzMoveRequest request)
    {
        try
        {
            return await _cameraControllerClient.MovePtzAsync(id, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move PTZ for camera {CameraId}", id);
            return null;
        }
    }

    public async Task<bool> StopPtzAsync(Guid id)
    {
        try
        {
            return await _cameraControllerClient.StopPtzAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop PTZ for camera {CameraId}", id);
            return false;
        }
    }
}