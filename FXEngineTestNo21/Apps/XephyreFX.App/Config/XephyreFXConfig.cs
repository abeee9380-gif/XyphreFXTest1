namespace XephyreFX.App.Config;

public sealed class TextElementConfig
{
    public string ColorHex { get; set; } = "#FFFFFF";
    public double OffsetX { get; set; } = 0;
    public double OffsetY { get; set; } = 0;
    public double Scale { get; set; } = 1.0;

    /// <summary>Leave blank to use the shared font below; set this to override just this one element's font.</summary>
    public string? FontFamily { get; set; }
}

public sealed class TextElementsConfig
{
    /// <summary>Default for every element that doesn't set its own FontFamily override.</summary>
    public string FontFamily { get; set; } = "Segoe UI";

    public TextElementConfig Time { get; set; } = new() { OffsetY = -72 };
    public TextElementConfig Date { get; set; } = new() { OffsetY = -46, Scale = 0.62 };
    public TextElementConfig Temperature { get; set; } = new() { OffsetY = -4 };
    public TextElementConfig Condition { get; set; } = new() { OffsetY = 40, Scale = 0.7 };
    public TextElementConfig Forecast { get; set; } = new() { OffsetY = 76, Scale = 0.55 };
}

public enum CustomElementType { Line, Image }

/// <summary>
/// A user-added decoration inside the blob -- either a divider line (e.g. to separate the
/// time from the date) or a small custom image. Added/removed from the settings panel,
/// draggable in the scene like the text elements, and saved in config.json.
/// </summary>
public sealed class CustomElementConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public CustomElementType Type { get; set; } = CustomElementType.Line;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public string ColorHex { get; set; } = "#FFFFFF";

    // Line-only:
    public double Length { get; set; } = 80;
    public double Thickness { get; set; } = 2;
    public bool Horizontal { get; set; } = true;

    // Image-only:
    public string? ImagePath { get; set; }
    public double Scale { get; set; } = 1.0;
}

public sealed class CelestialConfig
{
    /// <summary>
    /// Per-period custom image paths, keyed by SkyPeriod name (Morning/Noon/Afternoon/Evening/
    /// Sunset/Night). Missing or blank for a period falls back to the procedural sun/moon for
    /// that period -- so the moon can have a different image than the noon sun, etc.
    /// </summary>
    public Dictionary<string, string?> ImagesByPeriod { get; set; } = new();

    /// <summary>Position relative to the blob's center, in pixels. Drag it in the scene, or set directly here.</summary>
    public double OffsetX { get; set; } = 140;
    public double OffsetY { get; set; } = -140;
    public double Scale { get; set; } = 1.0;
}

/// <summary>Where clouds spawn/cluster, relative to the blob's center. Drag the small handle in the scene to relocate.</summary>
public sealed class PositionConfig
{
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
}

public sealed class BlobConfig
{
    public string BaseColorHex { get; set; } = "#000000";
    public string HighlightColorHex { get; set; } = "#1E1C22";
}

public sealed class CreditsConfig
{
    public string CreatorName { get; set; } = "ThatMemeKidd";
    public string YouTube { get; set; } = "@ThatMemeKidd";
}

/// <summary>Whether the scene should try to attach behind the desktop icons automatically on launch, instead of starting as a normal window every time.</summary>
public sealed class DesktopModeConfig
{
    public bool StartEmbedded { get; set; }
}

public sealed class XephyreFXConfig
{
    public TextElementsConfig Text { get; set; } = new();
    public CelestialConfig Celestial { get; set; } = new();
    public PositionConfig CloudAnchor { get; set; } = new();
    public BlobConfig Blob { get; set; } = new();
    public CreditsConfig Credits { get; set; } = new();
    public DesktopModeConfig DesktopMode { get; set; } = new();
    public List<CustomElementConfig> CustomElements { get; set; } = new();
}
