using Lightview.Shared.Contracts;
using Lightview.Shared.Contracts.InternalApi;

namespace CameraManager.Interfaces;

public interface ICameraService
{
    Task<List<Camera>> GetAllCamerasAsync();
    Task<Camera?> GetCameraByIdAsync(Guid id);
    Task<Camera> AddCameraAsync(AddCameraRequest request);
    Task<Camera> UpdateCameraAsync(Guid id, Camera camera);
    Task<bool> DeleteCameraAsync(Guid id);
    Task<CameraStatusResponse?> GetCameraStatusAsync(Guid id);
    Task<bool> ConnectCameraAsync(Guid id);
    Task<bool> DisconnectCameraAsync(Guid id);
    Task<StreamUrlResponse?> GetStreamUrlAsync(Guid id, string? profileToken = null);
    Task<PtzMoveResponse?> MovePtzAsync(Guid id, PtzMoveRequest request);
    Task<bool> StopPtzAsync(Guid id);
}