using System.Text.Json.Serialization;

namespace AffectionIndicator.Models;

public enum IndicatorAnchor
{
    TopRight,
    TopLeft,
    TopCenter,
    Center,
    BottomRight,
    BottomLeft,
    Custom
}

public sealed class AppSettings
{
    public bool AlwaysOnTop { get; set; } = true;
    public bool ClickThrough { get; set; } = true;
    public bool AutoStart { get; set; } = false;
    public bool NetworkAutoSwitch { get; set; } = true;
    public bool ControlServerEnabled { get; set; } = true;
    public int ControlServerPort { get; set; } = 8765;

    public IndicatorAnchor Anchor { get; set; } = IndicatorAnchor.TopRight;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double Scale { get; set; } = 1.0;

    public double NameOffsetX { get; set; }
    public double NameOffsetY { get; set; }

    public string Name { get; set; } = "HANK";
    public string Adjective { get; set; } = "HOSTILE";

    [JsonIgnore]
    public string AnchorLabel => Anchor switch
    {
        IndicatorAnchor.TopRight => "Top right",
        IndicatorAnchor.TopLeft => "Top left",
        IndicatorAnchor.TopCenter => "Top center",
        IndicatorAnchor.Center => "Center",
        IndicatorAnchor.BottomRight => "Bottom right",
        IndicatorAnchor.BottomLeft => "Bottom left",
        _ => "Custom"
    };

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    public void Normalize()
    {
        Scale = Math.Clamp(Scale, 0.7, 1.6);
        ControlServerPort = Math.Clamp(ControlServerPort, 1024, 65535);
        OffsetX = Math.Clamp(OffsetX, -4000, 4000);
        OffsetY = Math.Clamp(OffsetY, -4000, 4000);
        NameOffsetX = Math.Clamp(NameOffsetX, -300, 300);
        NameOffsetY = Math.Clamp(NameOffsetY, -100, 100);
        Name = string.IsNullOrWhiteSpace(Name) ? "HANK" : Name.Trim();
        Adjective = string.IsNullOrWhiteSpace(Adjective) ? "HOSTILE" : Adjective.Trim();
    }
}
