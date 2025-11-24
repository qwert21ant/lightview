namespace CameraControllerConnector.Models;

/// <summary>
/// Configuration for camera controller service connection
/// </summary>
public class CameraControllerConfiguration
{
    /// <summary>
    /// Base URL of the camera controller API (e.g., "http://camera-controller:5001")
    /// </summary>
    public required string BaseUrl { get; set; }

    /// <summary>
    /// API key or authentication token (if required)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// HTTP client timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable retry policy for failed requests
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}
