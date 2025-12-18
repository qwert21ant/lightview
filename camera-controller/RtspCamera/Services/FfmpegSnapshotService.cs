using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace RtspCamera.Services;

/// <summary>
/// FFmpeg-based snapshot capture service for RTSP streams
/// </summary>
public class FfmpegSnapshotService
{
    private readonly ILogger<FfmpegSnapshotService> _logger;

    public FfmpegSnapshotService(ILogger<FfmpegSnapshotService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Captures a snapshot from an RTSP stream
    /// </summary>
    /// <param name="rtspUrl">The RTSP stream URL</param>
    /// <param name="username">Optional username for authentication</param>
    /// <param name="password">Optional password for authentication</param>
    /// <param name="timeoutSeconds">Timeout in seconds for the capture operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JPEG image bytes or null if capture failed</returns>
    public async Task<byte[]?> CaptureSnapshotAsync(
        string rtspUrl, 
        string? username = null, 
        string? password = null,
        int timeoutSeconds = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(rtspUrl))
        {
            _logger.LogWarning("RTSP URL is null or empty");
            return null;
        }

        try
        {
            _logger.LogDebug("Capturing snapshot from RTSP stream: {RtspUrl}", rtspUrl);

            // Build the authenticated URL if credentials are provided
            var authenticatedUrl = BuildAuthenticatedUrl(rtspUrl, username, password);

            // Create temporary file path for the snapshot
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"snapshot_{Guid.NewGuid()}.jpg");

            try
            {
                var success = await CaptureWithFfmpegAsync(authenticatedUrl, tempFilePath, timeoutSeconds, cancellationToken);
                
                if (!success)
                {
                    _logger.LogWarning("FFmpeg snapshot capture failed for URL: {RtspUrl}", rtspUrl);
                    return null;
                }

                // Read the captured image file
                if (File.Exists(tempFilePath))
                {
                    var imageBytes = await File.ReadAllBytesAsync(tempFilePath, cancellationToken);
                    _logger.LogDebug("Successfully captured snapshot: {ByteCount} bytes", imageBytes.Length);
                    return imageBytes;
                }
                else
                {
                    _logger.LogWarning("Snapshot file was not created by FFmpeg");
                    return null;
                }
            }
            finally
            {
                // Clean up temporary file
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to delete temporary snapshot file: {TempFilePath}", tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing snapshot from RTSP stream: {RtspUrl}", rtspUrl);
            return null;
        }
    }

    private static string BuildAuthenticatedUrl(string rtspUrl, string? username, string? password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return rtspUrl;
        }

        try
        {
            var uri = new Uri(rtspUrl);
            var authenticatedUri = new UriBuilder(uri)
            {
                UserName = username,
                Password = password
            };
            return authenticatedUri.ToString();
        }
        catch (Exception)
        {
            // If URL parsing fails, return original URL
            return rtspUrl;
        }
    }

    private async Task<bool> CaptureWithFfmpegAsync(
        string rtspUrl, 
        string outputPath, 
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        try
        {
            var arguments = BuildFfmpegArguments(rtspUrl, outputPath);
            
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogDebug("Starting FFmpeg with arguments: {Arguments}", arguments);

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            // Wait for the process to complete with timeout
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            await process.WaitForExitAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            
            var completed = process.HasExited;
            
            if (!completed)
            {
                _logger.LogWarning("FFmpeg snapshot capture timed out after {TimeoutSeconds} seconds", timeoutSeconds);
                process.Kill();
                return false;
            }

            if (process.ExitCode == 0)
            {
                _logger.LogDebug("FFmpeg snapshot capture completed successfully");
                return true;
            }
            else
            {
                var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogWarning("FFmpeg snapshot capture failed with exit code {ExitCode}. Error: {Error}", 
                    process.ExitCode, stderr);
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("FFmpeg snapshot capture was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running FFmpeg for snapshot capture");
            return false;
        }
    }

    private static string BuildFfmpegArguments(string rtspUrl, string outputPath)
    {
        // FFmpeg arguments for capturing a single frame from RTSP stream
        // -y: overwrite output file
        // -i: input URL
        // -vframes 1: capture only 1 video frame
        // -q:v 2: high quality (scale 1-31, lower is better)
        // -f image2: force format to image
        // -update 1: update the same file (useful for single frame)
        return $"-rtsp_transport tcp -y -i \"{rtspUrl}\" -vframes 1 -q:v 2 -f image2 -update 1 \"{outputPath}\"";
    }
}