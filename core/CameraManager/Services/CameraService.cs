using CameraManager.Interfaces;
using CameraManager.Mappers;
using CameraControllerConnector.Interfaces;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;
using SharedCamera = Lightview.Shared.Contracts.Camera;
using System.Text.Json;

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
        var persistenceCameras = await _dbContext.Cameras
            .AsNoTracking()
            .Include(c => c.Metadata)
            .Include(c => c.Profiles)
            .ToListAsync();
        return persistenceCameras.Select(c => c.ToSharedCamera()).ToList();
    }

    public async Task<SharedCamera?> GetCameraByIdAsync(Guid id)
    {
        var persistenceCamera = await _dbContext.Cameras
            .AsNoTracking()
            .Include(c => c.Metadata)
            .Include(c => c.Profiles)
            .FirstOrDefaultAsync(c => c.Id == id);
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

    public async Task<SharedCamera> UpdateCameraConfigAsync(Guid id, SharedCamera updatedCamera)
    {
        var persistenceCamera = await _dbContext.Cameras
            .Include(c => c.Metadata)
            .Include(c => c.Profiles)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (persistenceCamera == null)
            throw new ArgumentException($"Camera with ID {id} not found");

        // Update only main camera table properties
        persistenceCamera.Name = updatedCamera.Name;
        persistenceCamera.Url = updatedCamera.Url.ToString();
        persistenceCamera.Username = updatedCamera.Username;
        persistenceCamera.Password = updatedCamera.Password;
        persistenceCamera.Protocol = (int)updatedCamera.Protocol;
        
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
            _logger.LogInformation("Camera {CameraId} config updated in camera-controller", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update camera {CameraId} config in camera-controller", id);
        }

        return persistenceCamera.ToSharedCamera();
    }

    /// <summary>
    /// Update camera metadata only (status, capabilities, device info)
    /// Used by event handlers to avoid circular updates
    /// </summary>
    public async Task UpdateCameraMetadataAsync(Guid id, CameraStatus? status = null, 
        CameraCapabilities? capabilities = null, CameraDeviceInfo? deviceInfo = null, DateTime? lastConnectedAt = null)
    {
        var camera = await _dbContext.Cameras
            .Include(c => c.Metadata)
            .FirstOrDefaultAsync(c => c.Id == id);
        
        if (camera == null)
        {
            _logger.LogWarning("Camera {CameraId} not found for metadata update", id);
            return;
        }

        // Create metadata if it doesn't exist
        if (camera.Metadata == null)
        {
            camera.Metadata = new Persistence.Models.CameraMetadata
            {
                CameraId = id,
                Status = (int)CameraStatus.Offline,
                LastConnectedAt = DateTime.MinValue
            };
        }

        // Update provided fields
        if (status.HasValue)
            camera.Metadata.Status = (int)status.Value;
        
        if (lastConnectedAt.HasValue)
            camera.Metadata.LastConnectedAt = lastConnectedAt.Value;
            
        if (capabilities != null)
            camera.Metadata.CapabilitiesJson = JsonSerializer.Serialize(capabilities);
            
        if (deviceInfo != null)
            camera.Metadata.DeviceInfoJson = JsonSerializer.Serialize(deviceInfo);

        await _dbContext.SaveChangesAsync();
        _logger.LogDebug("Camera {CameraId} metadata updated", id);
    }

    /// <summary>
    /// Update camera profiles (replace existing profiles with new ones)
    /// Used by event handlers when profile information is updated
    /// </summary>
    public async Task UpdateCameraProfilesAsync(Guid id, List<CameraProfile> profiles)
    {
        var camera = await _dbContext.Cameras
            .Include(c => c.Profiles)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (camera == null)
        {
            _logger.LogWarning("Camera {CameraId} not found for profiles update", id);
            return;
        }

        // Remove existing profiles from database context
        _dbContext.CameraProfiles.RemoveRange(camera.Profiles);

        // Add new profiles
        foreach (var profile in profiles)
        {
            _dbContext.CameraProfiles.Add(profile.ToPersistenceProfile(id));
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogDebug("Camera {CameraId} profiles updated with {ProfileCount} profiles", id, profiles.Count);
    }

    public async Task<bool> DeleteCameraAsync(Guid id)
    {
        var camera = await _dbContext.Cameras
            .Include(c => c.Metadata)
            .Include(c => c.Profiles)
            .FirstOrDefaultAsync(c => c.Id == id);
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
                var camera = await _dbContext.Cameras
                    .Include(c => c.Metadata)
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (camera != null)
                {
                    if (camera.Metadata == null)
                    {
                        camera.Metadata = new Persistence.Models.CameraMetadata
                        {
                            CameraId = camera.Id,
                            Status = (int)CameraStatus.Offline
                        };
                    }
                    camera.Metadata.LastConnectedAt = DateTime.UtcNow;
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

    public async Task SaveSnapshotAsync(Guid cameraId, byte[] imageData, string? profileToken = null, DateTime? capturedAt = null)
    {
        try
        {
            // Verify camera exists
            var cameraExists = await _dbContext.Cameras.AnyAsync(c => c.Id == cameraId);
            if (!cameraExists)
            {
                _logger.LogWarning("Cannot save snapshot for non-existent camera {CameraId}", cameraId);
                return;
            }

            // Remove existing snapshots for this camera to keep only the latest
            var existingSnapshots = await _dbContext.CameraSnapshots
                .Where(s => s.CameraId == cameraId)
                .ToListAsync();
            
            if (existingSnapshots.Any())
            {
                _dbContext.CameraSnapshots.RemoveRange(existingSnapshots);
                _logger.LogDebug("Removed {Count} existing snapshots for camera {CameraId}", existingSnapshots.Count, cameraId);
            }

            var snapshot = new Persistence.Models.CameraSnapshot
            {
                Id = Guid.NewGuid(),
                CameraId = cameraId,
                ImageData = imageData,
                ProfileToken = profileToken,
                CapturedAt = capturedAt?.ToUniversalTime() ?? DateTime.UtcNow,
                FileSize = imageData.Length
            };

            _dbContext.CameraSnapshots.Add(snapshot);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Saved latest snapshot for camera {CameraId}: {FileSize} bytes", cameraId, imageData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save snapshot for camera {CameraId}", cameraId);
            throw;
        }
    }

    public async Task<Persistence.Models.CameraSnapshot?> GetLatestSnapshotAsync(Guid cameraId)
    {
        try
        {
            return await _dbContext.CameraSnapshots
                .AsNoTracking()
                .Where(s => s.CameraId == cameraId)
                .OrderByDescending(s => s.CapturedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest snapshot for camera {CameraId}", cameraId);
            throw;
        }
    }
}