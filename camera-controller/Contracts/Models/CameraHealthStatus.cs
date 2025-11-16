namespace CameraController.Contracts.Models;

/// <summary>
/// Represents camera health status
/// </summary>
public class CameraHealthStatus
{
    public bool IsHealthy { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ResponseTime { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int ConsecutiveSuccesses { get; set; }
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public Exception? LastError { get; set; }
}