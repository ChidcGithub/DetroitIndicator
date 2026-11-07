using System.Windows;
using System.Windows.Media;

namespace AffectionIndicator.Rendering;

public sealed class GaugeView : FrameworkElement
{
    private readonly GaugeRenderer _renderer = new();

    public GaugeState State { get; set; } = GaugeState.None;
    public string GaugeName { get; set; } = "HANK";
    public string Adjective { get; set; } = "HOSTILE";
    public double Time { get; set; }
    public double Scale { get; set; } = 1;
    public double NameOffsetX { get; set; }
    public double NameOffsetY { get; set; }
    public double ArrowOffsetX { get; set; }
    public double ArrowOffsetY { get; set; }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        _renderer.Draw(drawingContext, Scale, State, GaugeName, Adjective, Time, dpi,
            NameOffsetX, NameOffsetY, ArrowOffsetX, ArrowOffsetY);
    }
}
