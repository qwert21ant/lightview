using CameraController.Contracts.Interfaces;
using Lightview.Shared.Contracts;

namespace WebService.Factories;

/// <summary>
/// Factory for creating camera instances - placeholder for future implementation
/// </summary>
public interface ICameraFactory
{
    ICamera CreateCamera(Camera configuration);
}