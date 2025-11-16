using Microsoft.AspNetCore.Mvc;

namespace WebService.Controllers;

/// <summary>
/// Health check controller for monitoring service status
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                service = "Camera Controller",
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Detailed health check with dependency status
    /// </summary>
    /// <returns>Detailed health status</returns>
    [HttpGet("detailed")]
    public IActionResult GetDetailed()
    {
        try
        {
            var health = new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                service = "Camera Controller",
                version = "1.0.0",
                uptime = Environment.TickCount64,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                dependencies = new
                {
                    ffprobe = CheckFFprobeAvailability(),
                    mediamtx = "Not implemented" // Could add MediaMTX connectivity check
                }
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed");
            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    private string CheckFFprobeAvailability()
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                process.WaitForExit(5000); // 5 second timeout
                return process.ExitCode == 0 ? "Available" : "Error";
            }
            return "Not found";
        }
        catch
        {
            return "Not available";
        }
    }
}