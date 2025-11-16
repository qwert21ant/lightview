namespace Lightview.Shared.Contracts;

public class CameraCapabilities
{
    public bool SupportsPtz { get; set; }
    public bool SupportsAudio { get; set; }
    public bool SupportsMotionDetection { get; set; }
    public bool SupportsIrCut { get; set; }
    public bool SupportsPresets { get; set; }
    public bool SupportsSnapshot { get; set; }
    public bool SupportsZoom { get; set; }
    public bool SupportsFocus { get; set; }
    public bool SupportsIris { get; set; }
    public List<string> SupportedProfiles { get; set; } = new();
    public PtzCapabilities? PtzCapabilities { get; set; }
    public ImageCapabilities? ImageCapabilities { get; set; }
}

public class PtzCapabilities
{
    public PtzRange PanRange { get; set; } = new();
    public PtzRange TiltRange { get; set; } = new();
    public PtzRange ZoomRange { get; set; } = new();
    public PtzSpeed DefaultSpeed { get; set; } = new();
    public PtzSpeed MaxSpeed { get; set; } = new();
    public int MaxPresets { get; set; }
}

public class PtzRange
{
    public float Min { get; set; }
    public float Max { get; set; }
}

public class PtzSpeed
{
    public float Pan { get; set; }
    public float Tilt { get; set; }
    public float Zoom { get; set; }
}

public class ImageCapabilities
{
    public Range BrightnessRange { get; set; } = new();
    public Range ContrastRange { get; set; } = new();
    public Range SaturationRange { get; set; } = new();
    public Range SharpnessRange { get; set; } = new();
    public List<FocusMode> SupportedFocusModes { get; set; } = new();
    public List<IrMode> SupportedIrModes { get; set; } = new();
}

public class Range
{
    public float Min { get; set; }
    public float Max { get; set; }
    public float Step { get; set; }
}

public enum FocusMode
{
    Auto,
    Manual,
    SemiAuto
}

public enum IrMode
{
    Auto,
    On,
    Off
}