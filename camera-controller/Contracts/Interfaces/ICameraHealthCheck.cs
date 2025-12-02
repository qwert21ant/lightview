using Lightview.Shared.Contracts;

namespace CameraController.Contracts.Interfaces;

/// <summary>
/// Interface for performing camera health checks
/// </summary>
public interface ICameraHealthCheck
{
    /// <summary>
    /// Performs a ping test to check if the camera's host is reachable
    /// </summary>
    Task<HealthCheckResult> PingCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests camera credentials by attempting authentication
    /// </summary>
    Task<HealthCheckResult> CredentialsCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests if the camera's video stream is accessible
    /// </summary>
    Task<HealthCheckResult> StreamCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs all health checks and returns comprehensive results
    /// </summary>
    Task<CameraHealthCheckResults> PerformAllHealthChecksAsync(CancellationToken cancellationToken = default);
}