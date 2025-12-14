using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;

namespace CameraManager.Interfaces;

public interface ICameraService
{
    Task<List<Camera>> GetAllCamerasAsync();
    Task<Camera?> GetCameraByIdAsync(Guid id);
    Task<Camera> AddCameraAsync(AddCameraRequest request);
    Task<Camera> UpdateCameraConfigAsync(Guid id, Camera camera);
    Task UpdateCameraMetadataAsync(Guid id, CameraStatus? status = null, 
        CameraCapabilities? capabilities = null, CameraDeviceInfo? deviceInfo = null, DateTime? lastConnectedAt = null);
    Task UpdateCameraProfilesAsync(Guid id, List<CameraProfile> profiles);
    Task<bool> DeleteCameraAsync(Guid id);
    Task<CameraStatusResponse?> GetCameraStatusAsync(Guid id);
    Task<bool> ConnectCameraAsync(Guid id);
    Task<bool> DisconnectCameraAsync(Guid id);
    Task<PtzMoveResponse?> MovePtzAsync(Guid id, PtzMoveRequest request);
    Task<bool> StopPtzAsync(Guid id);
    Task SaveSnapshotAsync(Guid cameraId, byte[] imageData, string? profileToken = null, DateTime? capturedAt = null);
    Task<Persistence.Models.CameraSnapshot?> GetLatestSnapshotAsync(Guid cameraId);
}