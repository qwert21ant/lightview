namespace CameraController.Contracts.Models;

/// <summary>
/// Service health summary
/// </summary>
public class ServiceHealthSummary
{
    public int TotalCameras { get; set; }
    public int HealthyCameras { get; set; }
    public int UnhealthyCameras { get; set; }
    public int OfflineCameras { get; set; }
    public int ConnectingCameras { get; set; }
    public int ErrorCameras { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<Guid> ProblematicCameras { get; set; } = new();
}