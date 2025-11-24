using Lightview.Shared.Contracts.InternalApi;

namespace Lightview.Shared.Contracts.InternalApi;

/// <summary>
/// Extended camera status response that includes credentials for camera-controller initialization
/// </summary>
public class CameraInitializationResponse : CameraStatusResponse
{
    /// <summary>
    /// Camera username for authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Camera password for authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Camera protocol (ONVIF, RTSP, etc.)
    /// </summary>
    public CameraProtocol Protocol { get; set; } = CameraProtocol.Onvif;
}