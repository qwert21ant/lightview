namespace Lightview.Shared.Contracts;

// Stream Management Models
public class CameraProfile
{
    public string Token { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public VideoSettings Video { get; set; } = new();
    public AudioSettings? Audio { get; set; }
    public string? RtspUri { get; set; }
    public string? HttpUri { get; set; }
    public bool IsMainStream { get; set; }
}

public class VideoSettings
{
    public string Codec { get; set; } = string.Empty; // H.264, H.265, MJPEG
    public Resolution Resolution { get; set; } = new();
    public int Framerate { get; set; }
    public int Bitrate { get; set; }
    public BitrateControl BitrateControl { get; set; } = BitrateControl.VBR;
    public int Quality { get; set; } = 5; // 1-10
    public int GovLength { get; set; } = 30;
}

public class AudioSettings
{
    public string Codec { get; set; } = string.Empty; // AAC, G.711, G.726
    public int Bitrate { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; } = 1;
}

public class Resolution
{
    public int Width { get; set; }
    public int Height { get; set; }
    
    public override string ToString() => $"{Width}x{Height}";
}

public enum BitrateControl
{
    CBR, // Constant Bitrate
    VBR  // Variable Bitrate
}

public class StreamConfiguration
{
    public Guid CameraId { get; set; }
    public string StreamPath { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public StreamProtocol Protocol { get; set; } = StreamProtocol.Rtsp;
    public bool IsActive { get; set; }
    public StreamSettings Settings { get; set; } = new();
}

public class StreamSettings
{
    public bool RecordingEnabled { get; set; }
    public int MaxConnections { get; set; } = 100;
    public TimeSpan ReconnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool AllowPlayback { get; set; } = true;
    public bool AllowPublish { get; set; } = false;
}

public enum StreamProtocol
{
    Rtsp,
    Rtmp,
    WebRTC,
    Hls
}

public class StreamStatus
{
    public Guid CameraId { get; set; }
    public string StreamPath { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public int ViewerCount { get; set; }
    public StreamStatistics Statistics { get; set; } = new();
    public DateTime LastActivity { get; set; }
}

public class StreamStatistics
{
    public long BytesReceived { get; set; }
    public long BytesSent { get; set; }
    public double CurrentBitrate { get; set; }
    public double AverageBitrate { get; set; }
    public int DroppedFrames { get; set; }
    public TimeSpan Uptime { get; set; }
}