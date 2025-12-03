using Microsoft.AspNetCore.Mvc;
using CameraController.Contracts.Interfaces;
using CameraController.Contracts.Models;

namespace WebService.Controllers;

/// <summary>
/// Controller for managing MediaMTX streaming operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StreamsController : ControllerBase
{
    private readonly IMediaMtxService _mediaMtxService;
    private readonly ILogger<StreamsController> _logger;

    public StreamsController(
        IMediaMtxService mediaMtxService,
        ILogger<StreamsController> logger)
    {
        _mediaMtxService = mediaMtxService;
        _logger = logger;
    }

    /// <summary>
    /// Get WebRTC URL for streaming a camera
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WebRTC URL for client access</returns>
    [HttpGet("{cameraId:guid}/webrtc")]
    public async Task<ActionResult<string>> GetWebRtcUrl(
        Guid cameraId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting WebRTC URL for camera {CameraId}", cameraId);
            
            var url = await _mediaMtxService.GetWebRtcUrlAsync(cameraId, "main", cancellationToken);
            return Ok(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get WebRTC URL for camera {CameraId}", cameraId);
            return StatusCode(500, "Failed to get WebRTC URL");
        }
    }

    /// <summary>
    /// Check if a camera stream is currently active
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream status</returns>
    [HttpGet("{cameraId:guid}/status")]
    public async Task<ActionResult<object>> GetStreamStatus(
        Guid cameraId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking stream status for camera {CameraId}", cameraId);
            
            var isActive = await _mediaMtxService.IsStreamActiveAsync(cameraId, "main", cancellationToken);
            
            return Ok(new { 
                cameraId = cameraId,
                isActive = isActive,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check stream status for camera {CameraId}", cameraId);
            return StatusCode(500, "Failed to check stream status");
        }
    }

    /// <summary>
    /// Get detailed stream statistics for a camera
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream statistics</returns>
    [HttpGet("{cameraId:guid}/statistics")]
    public async Task<ActionResult<MediaMtxStreamStatistics>> GetStreamStatistics(
        Guid cameraId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting stream statistics for camera {CameraId}", cameraId);
            
            var statistics = await _mediaMtxService.GetStreamStatisticsAsync(cameraId, "main", cancellationToken);
            
            if (statistics == null)
            {
                return NotFound($"Stream statistics not found for camera {cameraId}");
            }
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stream statistics for camera {CameraId}", cameraId);
            return StatusCode(500, "Failed to get stream statistics");
        }
    }

    /// <summary>
    /// Get stream URLs for multiple cameras
    /// </summary>
    /// <param name="cameraIds">Array of camera IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of camera IDs to stream URLs</returns>
    [HttpPost("batch-urls")]
    public async Task<ActionResult<Dictionary<Guid, object>>> GetBatchStreamUrls(
        [FromBody] Guid[] cameraIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting batch stream URLs for {Count} cameras", cameraIds.Length);
            
            var result = new Dictionary<Guid, object>();
            
            var tasks = cameraIds.Select(async cameraId =>
            {
                try
                {
                    var webRtcUrl = await _mediaMtxService.GetWebRtcUrlAsync(cameraId, "main", cancellationToken);
                    var isActive = await _mediaMtxService.IsStreamActiveAsync(cameraId, "main", cancellationToken);
                    
                    return new KeyValuePair<Guid, object>(cameraId, new
                    {
                        webRtcUrl = webRtcUrl,
                        isActive = isActive
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get URLs for camera {CameraId}", cameraId);
                    return new KeyValuePair<Guid, object>(cameraId, new
                    {
                        error = "Failed to get stream URLs"
                    });
                }
            });
            
            var results = await Task.WhenAll(tasks);
            
            foreach (var kvp in results)
            {
                result[kvp.Key] = kvp.Value;
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get batch stream URLs");
            return StatusCode(500, "Failed to get batch stream URLs");
        }
    }
}