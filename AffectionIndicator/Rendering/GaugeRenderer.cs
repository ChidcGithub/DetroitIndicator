using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace AffectionIndicator.Rendering;

public sealed class GaugeRenderer
{
    public const double ViewWidth = 520;
    public const double ViewHeight = 236;
    public const double ViewOriginX = 1446;
    public const double ViewOriginY = -66;

    private const double StageWidth = 1920;
    private const double StageHeight = 1080;
    private const double SafeY = -42;

    private const double BgX = 1470;
    private const double BgY = SafeY;
    private const double TriangleWidth = 173;
    private const double TriangleHeight = 150;
    private static readonly double[] TriangleAlphas = { 0.3, 0.5, 0.6, 0.3 };

    private const double NameCenterX = 1732;
    private const double NameCenterY = 26;
    private const double NameSize = 60;
    private const double NameTracking = 10;

    private const double AdjectiveX = 1862;
    private const double AdjectiveOriginY = 56;
    private const double AdjectivePlateX = 16;
    private const double AdjectivePlateY = 14;
    private const double AdjectiveHeight = 32;
    private const double AdjectiveMinWidth = 212;
    private const double AdjectiveCenterX = 1772;
    private const double AdjectiveTextCenterY = 86;
    private const double AdjectiveTracking = 10;

    private const double BendingX = 1902;
    private const double BendingY = 34;

    private const double PathPlateY = 74;
    private const double PathUnlockX = -16;
    private const double PathUnlockY = 90;
    private const double PathScale = 0.35;
    private const double PathShiftX = -40;
    private const double PathCenterX = 1772;
    private const double PathTextCenterY = 124;

    private const double BigArrowLift = -20;

    private static readonly Color Blue = Color.FromRgb(0x80, 0xEE, 0xFF);
    private static readonly Color Red = Color.FromRgb(0xFF, 0x30, 0x00);
    private static readonly Color White = Colors.White;
    private static readonly Color Black = Colors.Black;

    private readonly BitmapSource _lockPart1;
    private readonly BitmapSource _lockPart2;

    public GaugeRenderer()
    {
        _lockPart1 = LoadBitmap("pack://application:,,,/Assets/Lock_part1.png");
        _lockPart2 = LoadBitmap("pack://application:,,,/Assets/Lock_part2.png");
    }

    private static BitmapSource LoadBitmap(string uri)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(uri, UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public void Draw(
        DrawingContext dc,
        double scale,
        GaugeState state,
        string name,
        string adjective,
        double time,
        double pixelsPerDip,
        double nameOffsetX = 0,
        double nameOffsetY = 0,
        double arrowOffsetX = 0,
        double arrowOffsetY = 0)
    {
        if (time < 0)
            return;

        var frame = (int)Math.Floor(time * 30 + 1e-6);
        var hide = Clamp01((time - 1.9) / 0.5);
        var fade = 1 - hide;

        dc.PushTransform(new ScaleTransform(scale, scale));
        dc.PushTransform(new TranslateTransform(-ViewOriginX, -ViewOriginY));

        DrawNameStrip(dc, frame, fade);
        DrawName(dc, name, frame, fade, pixelsPerDip, nameOffsetX, nameOffsetY);
        DrawAdjective(dc, adjective, frame, fade, pixelsPerDip);

        switch (state)
        {
            case GaugeState.Increase:
            case GaugeState.Decrease:
                DrawSmallChevron(dc, time, state, fade, arrowOffsetX, arrowOffsetY);
                break;
            case GaugeState.IncreasePlus:
            case GaugeState.DecreasePlus:
                DrawBigArrow(dc, time, state, fade, pixelsPerDip, arrowOffsetX, arrowOffsetY);
                break;
            case GaugeState.Unlocked:
            case GaugeState.Locked:
                DrawPathPrompt(dc, time, state, fade, pixelsPerDip);
                break;
        }

        dc.Pop();
        dc.Pop();
    }

    private static void DrawNameStrip(DrawingContext dc, int frame, double fade)
    {
        dc.PushTransform(new TranslateTransform(BgX, BgY));
        for (var i = 0; i < 4; i++)
        {
            var alpha = TriangleAlpha(i, frame) * fade;
            if (alpha <= 0.001)
                continue;

            var brush = Brush(Black, alpha);
            if (i % 2 == 0)
            {
                var x0 = i * TriangleWidth * 0.5;
                DrawTriangle(dc, brush,
                    new Point(x0, 0),
                    new Point(x0 + TriangleWidth * 0.5, TriangleHeight),
                    new Point(x0 + TriangleWidth, 0));
            }
            else
            {
                var x0 = (i + 1) * TriangleWidth * 0.5;
                DrawTriangle(dc, brush,
                    new Point(x0, 0),
                    new Point(x0 - TriangleWidth * 0.5, TriangleHeight),
                    new Point(x0 + TriangleWidth * 0.5, TriangleHeight));
            }
        }
        dc.Pop();
    }

    private static double TriangleAlpha(int index, int frame)
    {
        if (frame < index * 2)
            return 0;
        if (frame >= index * 2 + 6)
            return TriangleAlphas[index];
        return frame % 2 == 0 ? 1 : 0;
    }

    private static void DrawName(
        DrawingContext dc,
        string name,
        int frame,
        double fade,
        double dpi,
        double offsetX,
        double offsetY)
    {
        var alpha = frame is 0 or 2 or 4 || frame >= 6 ? 1.0 : 0.0;
        if (alpha <= 0)
            return;

        var size = FitTextSize(name, NameSize, 340, 28, FontWeights.Light,
            NameTracking, dpi);
        DrawTrackedText(dc, name, NameCenterX + offsetX, NameCenterY + offsetY, size,
            FontWeights.Light, White, NameTracking, TextAlign.Center, alpha * fade, dpi);
    }

    private static void DrawAdjective(DrawingContext dc, string text, int frame, double fade, double dpi)
    {
        var local = frame - 4;
        if (local < 0)
            return;

        var textAlpha = local is 0 or 2 ? 0.0 : 1.0;
        var plateAlpha = local >= 3 ? 1.0 : 0.0;
        var adjFade = 1 - EaseOutQuint(1 - fade);
        var size = FitTextSize(text, 24, 320, 10, FontWeights.Bold, AdjectiveTracking, dpi);
        var width = MeasureText(text, size, FontWeights.Bold, AdjectiveTracking, dpi);
        var plateWidth = Math.Max(AdjectiveMinWidth, Math.Floor(width + 44));
        var textColor = local is 3 or 4 or 6 ? White : Black;

        if (plateAlpha > 0)
        {
            DrawPlate(dc, plateWidth, AdjectiveHeight, AdjectiveX + AdjectivePlateX,
                AdjectiveOriginY + AdjectivePlateY, -1, White, 1, plateAlpha * adjFade);
        }

        if (textAlpha > 0)
        {
            DrawTrackedText(dc, text, AdjectiveCenterX, AdjectiveTextCenterY, size,
                FontWeights.Bold, textColor, AdjectiveTracking, TextAlign.Center,
                textAlpha * adjFade, dpi);
        }
    }

    private static void DrawPlate(
        DrawingContext dc,
        double desiredWidth,
        double height,
        double x,
        double y,
        double scaleX,
        Color color,
        double bodyAlpha,
        double alpha)
    {
        if (alpha <= 0.001)
            return;

        var tw = height * 1.155;
        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new ScaleTransform(scaleX, 1));

        // First end group: 2 triangles, alpha 0.3 / 0.4.
        dc.PushTransform(new TranslateTransform(-tw, 0));
        DrawTriangle(dc, Brush(color, alpha * 0.3),
            new Point(0, 0), new Point(tw, 0), new Point(tw * 0.5, height));
        DrawTriangle(dc, Brush(color, alpha * 0.4),
            new Point(-tw * 0.5, height), new Point(tw * 0.5, height), new Point(0, 0));
        dc.Pop();

        // Last end group: 2 triangles, alpha 0.5 / 0.4.
        var lx = desiredWidth - tw + tw * 0.5;
        dc.PushTransform(new TranslateTransform(lx, 0));
        DrawTriangle(dc, Brush(color, alpha * 0.5),
            new Point(0, height), new Point(tw * 0.5, 0), new Point(tw, height));
        DrawTriangle(dc, Brush(color, alpha * 0.4),
            new Point(tw * 0.5, 0), new Point(tw, height), new Point(tw * 1.5, 0));
        dc.Pop();

        // Accordion body.
        DrawPolygon(dc, Brush(color, alpha * bodyAlpha), new[]
        {
            new Point(0, 0),
            new Point(desiredWidth, 0),
            new Point(desiredWidth - tw * 0.5, height),
            new Point(-tw * 0.5, height)
        });

        dc.Pop();
        dc.Pop();
    }

    private static void DrawSmallChevron(
        DrawingContext dc,
        double t,
        GaugeState state,
        double fade,
        double offsetX,
        double offsetY)
    {
        var increase = state == GaugeState.Increase;
        double y;
        double alpha;
        if (t < 1)
        {
            var e = EaseOutQuint(t);
            y = increase ? Lerp(0, -40, e) : Lerp(-28, 16, e);
            alpha = 1;
        }
        else
        {
            y = increase ? -40 : 16;
            alpha = Math.Max(0, 1 - (t - 1) / 0.3);
        }

        DrawChevron(dc, BendingX + 8 + offsetX, BendingY + y + offsetY, increase ? 1 : -1,
            increase ? Blue : Red, alpha * fade);
    }

    private static void DrawBigArrow(
        DrawingContext dc,
        double t,
        GaugeState state,
        double fade,
        double dpi,
        double offsetX,
        double offsetY)
    {
        var increase = state == GaugeState.IncreasePlus;
        var tm = Math.Min(t, 1.9);
        var p0 = Clamp01(tm);
        double maskY = increase
            ? Lerp(120, 56, EaseOutQuint(p0))
            : Lerp(-136, -40, EaseOutQuint(p0));

        if (tm >= 1)
        {
            var p1 = Clamp01(tm - 1);
            maskY = increase
                ? Lerp(56, -8, EaseOutQuint(p1))
                : Lerp(-40, 32, EaseOutQuint(p1));
        }

        maskY += BigArrowLift;
        var arrowY = (increase ? 56 : -40) + BigArrowLift;
        var sy = increase ? -1 : 1;

        double arrowAlpha;
        if (tm < 0.45) arrowAlpha = 0.8;
        else if (tm < 0.95) arrowAlpha = Lerp(0.8, 0.2, (tm - 0.45) / 0.5);
        else if (tm < 1) arrowAlpha = 0.2;
        else arrowAlpha = 0.5;
        arrowAlpha *= fade;

        if (arrowAlpha > 0.001)
        {
            var mask = ArrowGeometry(BendingX + 8 + offsetX, BendingY + maskY + offsetY, sy);
            dc.PushClip(mask);
            dc.PushOpacity(arrowAlpha);
            dc.DrawGeometry(Brush(increase ? Blue : Red, 1), null,
                ArrowGeometry(BendingX + 8 + offsetX, BendingY + arrowY + offsetY, sy));
            dc.Pop();
            dc.Pop();
        }

        if (tm >= 1)
        {
            var p1 = Clamp01(tm - 1);
            var y = increase ? Lerp(24, -40, EaseOutQuint(p1)) : Lerp(-6, 64, EaseOutQuint(p1));
            DrawChevron(dc, BendingX + 8 + offsetX, BendingY + y + BigArrowLift + offsetY,
                increase ? 1 : -1, increase ? Blue : Red, fade);
        }
    }

    private void DrawPathPrompt(DrawingContext dc, double t, GaugeState state, double fade, double dpi)
    {
        if (state is not (GaugeState.Unlocked or GaugeState.Locked) || t < 0.5)
            return;

        var localFrame = (int)Math.Floor((t - 0.5) * 30 + 1e-6);
        var plateAlpha = localFrame is 1 or 3 || localFrame >= 5 ? 1.0 : 0.0;
        var textAlpha = localFrame is 16 or 18 || localFrame >= 20 ? 1.0 : 0.0;
        var color = state == GaugeState.Unlocked ? Blue : Red;

        const string text = "PATH UNLOCKED";
        var size = 18.0;
        var width = MeasureText(text, size, FontWeights.Normal, 0, dpi);
        var plateWidth = Math.Floor(width + 58);
        var plateX = BendingX - 4 + PathShiftX;

        if (plateAlpha > 0)
        {
            DrawPlate(dc, plateWidth, 32, plateX, BendingY + PathPlateY,
                -1, color, 1, plateAlpha * fade);
        }

        DrawPadlock(dc, t, fade);

        if (textAlpha > 0)
        {
            var textCenter = plateX - plateWidth / 2;
            DrawTrackedText(dc, text, textCenter, PathTextCenterY, size,
                FontWeights.Normal, Black, 0, TextAlign.Center, textAlpha * fade, dpi);
        }
    }

    private void DrawPadlock(DrawingContext dc, double t, double fade)
    {
        if (t < 0.5)
            return;

        var tm = Math.Min(t, 1.9);
        var alpha = t >= 1.9
            ? 0.2 * fade
            : EaseInQuint(Clamp01((tm - 0.5) / 0.5));
        var slide = EaseOutQuint(Clamp01(tm - 1));
        var dx = 10 * slide;

        var cx = BendingX + PathUnlockX + PathShiftX;
        var cy = BendingY + PathUnlockY;

        dc.PushOpacity(alpha);
        dc.PushTransform(new TranslateTransform(cx, cy));
        dc.PushTransform(new ScaleTransform(PathScale, PathScale));
        dc.DrawImage(_lockPart1, new Rect(-64 + dx, -64, 128, 128));
        dc.DrawImage(_lockPart2, new Rect(-64 - dx, -64, 128, 128));
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    private static void DrawChevron(DrawingContext dc, double cx, double cy, double flipY, Color color, double alpha)
    {
        if (alpha <= 0.001)
            return;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(-32, 32), isFilled: false, isClosed: false);
            g.LineTo(new Point(0, 0), isStroked: true, isSmoothJoin: false);
            g.LineTo(new Point(32, 32), isStroked: true, isSmoothJoin: false);
        }
        geometry.Freeze();

        var pen = new Pen(Brush(color, alpha), 3)
        {
            StartLineCap = PenLineCap.Flat,
            EndLineCap = PenLineCap.Flat,
            LineJoin = PenLineJoin.Miter
        };

        dc.PushTransform(new TranslateTransform(cx, cy));
        dc.PushTransform(new ScaleTransform(1, flipY));
        dc.DrawGeometry(null, pen, geometry);
        dc.Pop();
        dc.Pop();
    }

    private static StreamGeometry ArrowGeometry(double cx, double cy, double sy)
    {
        var pts = new[]
        {
            new Point(0, 32), new Point(-32, 0), new Point(-32, 64),
            new Point(0, 96), new Point(32, 64), new Point(32, 0)
        };

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(cx, cy + sy * pts[0].Y), isFilled: true, isClosed: true);
            for (var i = 1; i < pts.Length; i++)
                g.LineTo(new Point(cx + pts[i].X, cy + sy * pts[i].Y), isStroked: true, isSmoothJoin: true);
        }
        geometry.Freeze();
        return geometry;
    }

    private enum TextAlign
    {
        Left,
        Center,
        Right
    }

    private static void DrawTrackedText(
        DrawingContext dc,
        string text,
        double centerOrRight,
        double centerY,
        double size,
        FontWeight weight,
        Color color,
        double tracking,
        TextAlign align,
        double alpha,
        double dpi)
    {
        if (alpha <= 0.001 || string.IsNullOrEmpty(text))
            return;

        var typeface = TypefaceFor(weight);
        var brush = Brush(color, alpha);
        var total = MeasureText(text, size, weight, tracking, dpi);
        var cursor = align switch
        {
            TextAlign.Right => centerOrRight - total,
            TextAlign.Center => centerOrRight - total / 2 - InkCenterOffset(text, size, weight, tracking, dpi),
            _ => centerOrRight
        };

        var sample = MakeText("Hg", size, typeface, brush, dpi);
        var baseline = centerY - sample.Height / 2 + sample.Baseline;

        foreach (var ch in text)
        {
            var ft = MakeText(ch.ToString(), size, typeface, brush, dpi);
            var top = baseline - ft.Baseline;
            dc.DrawText(ft, new Point(cursor, top));
            cursor += ft.WidthIncludingTrailingWhitespace + tracking;
        }
    }

    private static double MeasureText(string text, double size, FontWeight weight, double tracking, double dpi)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var typeface = TypefaceFor(weight);
        var brush = Brushes.White;
        var width = 0.0;
        foreach (var ch in text)
        {
            var ft = MakeText(ch.ToString(), size, typeface, brush, dpi);
            width += ft.WidthIncludingTrailingWhitespace + tracking;
        }
        return Math.Max(0, width - tracking);
    }

    private static double FitTextSize(string text, double baseSize, double maxWidth, double minSize,
        FontWeight weight, double tracking, double dpi)
    {
        var size = baseSize;
        while (size > minSize)
        {
            if (MeasureText(text, size, weight, tracking, dpi) <= maxWidth)
                break;
            size -= 1;
        }
        return size;
    }

    private static double InkCenterOffset(string text, double size, FontWeight weight, double tracking, double dpi)
    {
        var typeface = TypefaceFor(weight);
        var brush = Brushes.White;
        var group = new GeometryGroup();
        var cursor = 0.0;
        foreach (var ch in text)
        {
            var ft = MakeText(ch.ToString(), size, typeface, brush, dpi);
            if (!string.IsNullOrWhiteSpace(ch.ToString()))
            {
                var geometry = ft.BuildGeometry(new Point(cursor, 0));
                if (geometry != null && !geometry.IsEmpty())
                    group.Children.Add(geometry);
            }
            cursor += ft.WidthIncludingTrailingWhitespace + tracking;
        }

        var total = Math.Max(0, cursor - tracking);
        if (group.Children.Count == 0)
            return 0;

        var bounds = group.Bounds;
        return (bounds.Left + bounds.Right) / 2 - total / 2;
    }

    private static FormattedText MakeText(string text, double size, Typeface typeface, Brush brush, double dpi)
    {
        return new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            brush,
            dpi);
    }

    private static Typeface TypefaceFor(FontWeight weight)
    {
        return new Typeface(new FontFamily("Bahnschrift"), FontStyles.Normal,
            weight, FontStretches.Condensed);
    }

    private static void DrawTriangle(DrawingContext dc, Brush brush, Point a, Point b, Point c)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(a, isFilled: true, isClosed: true);
            g.LineTo(b, isStroked: true, isSmoothJoin: false);
            g.LineTo(c, isStroked: true, isSmoothJoin: false);
        }
        geometry.Freeze();
        dc.DrawGeometry(brush, null, geometry);
    }

    private static void DrawPolygon(DrawingContext dc, Brush brush, IReadOnlyList<Point> points)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0], isFilled: true, isClosed: true);
            for (var i = 1; i < points.Count; i++)
                g.LineTo(points[i], isStroked: true, isSmoothJoin: false);
        }
        geometry.Freeze();
        dc.DrawGeometry(brush, null, geometry);
    }

    private static SolidColorBrush Brush(Color color, double alpha)
    {
        var brush = new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(alpha * 255, 0, 255), color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);
    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    private static double EaseOutQuint(double t) => 1 - Math.Pow(1 - Clamp01(t), 5);
    private static double EaseInQuint(double t) => Math.Pow(Clamp01(t), 5);
}
