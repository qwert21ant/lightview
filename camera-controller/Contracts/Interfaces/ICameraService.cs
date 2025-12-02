using Lightview.Shared.Contracts;
using CameraController.Contracts.Models;
using Lightview.Shared.Contracts.InternalApi;

namespace CameraController.Contracts.Interfaces;

/// <summary>
/// Camera management service interface
/// </summary>
public interface ICameraService
{
    /// <summary>
    /// Get all managed cameras
    /// </summary>
    IReadOnlyDictionary<Guid, ICameraMonitoring> GetAllCameras();
    
    /// <summary>
    /// Get specific camera by ID
    /// </summary>
    ICameraMonitoring? GetCamera(Guid cameraId);
    
    /// <summary>
    /// Add and start monitoring a new camera
    /// </summary>
    Task<ICameraMonitoring> AddCameraAsync(Camera cameraConfig, CameraMonitoringConfig? monitoringConfig = null);
    
    /// <summary>
    /// Remove camera and stop monitoring
    /// </summary>
    Task<bool> RemoveCameraAsync(Guid cameraId);
    
    /// <summary>
    /// Update camera configuration
    /// </summary>
    Task<bool> UpdateCameraAsync(Guid cameraId, Camera updatedConfig);
    
    /// <summary>
    /// Get camera health status
    /// </summary>
    Task<CameraHealthStatus?> GetCameraHealthAsync(Guid cameraId);
    
    /// <summary>
    /// Get service health summary
    /// </summary>
    Task<ServiceHealthSummary> GetServiceHealthAsync();
    
    /// <summary>
    /// Connect to a specific camera
    /// </summary>
    Task<bool> ConnectCameraAsync(Guid cameraId);
    
    /// <summary>
    /// Disconnect from a specific camera
    /// </summary>
    Task<bool> DisconnectCameraAsync(Guid cameraId);
}