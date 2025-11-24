using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WebService.Hubs;

public class CameraHub : Hub
{
    // Example: Called by frontend to move a camera
    public async Task MoveCamera(string cameraId, string direction)
    {
        // Logic to move camera (call service, etc.)
        await Clients.All.SendAsync("CameraMoved", cameraId, direction);
    }

    // Add more methods for camera manipulation as needed
}
