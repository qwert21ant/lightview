namespace Lightview.Shared.Contracts;

// PTZ Control Models
public class PtzPosition
{
    public float Pan { get; set; }
    public float Tilt { get; set; }
    public float Zoom { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PtzMoveRequest
{
    public PtzMoveType MoveType { get; set; }
    public PtzPosition? AbsolutePosition { get; set; }
    public PtzPosition? RelativeMovement { get; set; }
    public PtzSpeed? ContinuousSpeed { get; set; }
    public PtzSpeed? Speed { get; set; }
    public TimeSpan? Duration { get; set; } // For continuous movement
}

public enum PtzMoveType
{
    Absolute,
    Relative,
    Continuous,
    Stop
}

public class PtzPreset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PtzPosition Position { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class PtzPresetRequest
{
    public string Name { get; set; } = string.Empty;
    public PtzPosition? Position { get; set; } // If null, uses current position
}

// Image Settings Models
public class ImageSettings
{
    public float Brightness { get; set; }
    public float Contrast { get; set; }
    public float Saturation { get; set; }
    public float Sharpness { get; set; }
    public FocusSettings Focus { get; set; } = new();
    public IrSettings IrCut { get; set; } = new();
    public WhiteBalanceSettings WhiteBalance { get; set; } = new();
}

public class FocusSettings
{
    public FocusMode Mode { get; set; } = FocusMode.Auto;
    public float? ManualPosition { get; set; } // Used when Mode is Manual
    public float? NearLimit { get; set; }
    public float? FarLimit { get; set; }
    public float Speed { get; set; } = 1.0f;
}

public class IrSettings
{
    public IrMode Mode { get; set; } = IrMode.Auto;
    public bool IsActive { get; set; }
    public float? SwitchThreshold { get; set; } // Lux threshold for auto mode
}

public class WhiteBalanceSettings
{
    public WhiteBalanceMode Mode { get; set; } = WhiteBalanceMode.Auto;
    public float? RedGain { get; set; }
    public float? BlueGain { get; set; }
}

public enum WhiteBalanceMode
{
    Auto,
    Manual,
    Indoor,
    Outdoor,
    Fluorescent,
    Incandescent
}