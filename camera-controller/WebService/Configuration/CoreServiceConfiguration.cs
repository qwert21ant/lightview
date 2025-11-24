namespace WebService.Configuration;

/// <summary>
/// Configuration for connecting to the core service
/// </summary>
public class CoreServiceConfiguration
{
    public required string BaseUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryAttempts { get; set; } = 5;
    public int RetryDelaySeconds { get; set; } = 5;
}
