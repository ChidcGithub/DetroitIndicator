using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AffectionIndicator.Models;
using AffectionIndicator.Rendering;
using AffectionIndicator.Services;

namespace AffectionIndicator;

public partial class MainWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;

    private readonly SettingsService _settingsService;
    private readonly Stopwatch _clock = new();
    private bool _renderingHooked;
    private bool _isDragging;
    private GaugeState _state = GaugeState.None;
    private string _name = "HANK";
    private string _adjective = "HOSTILE";

    public MainWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        SourceInitialized += (_, _) => ApplyWindowStyles();
        ApplySettings(settingsService.Settings);
    }

    public void ApplySettings(AppSettings settings)
    {
        Width = GaugeRenderer.ViewWidth * settings.Scale;
        Height = GaugeRenderer.ViewHeight * settings.Scale;
        Gauge.Scale = settings.Scale;
        Gauge.NameOffsetX = settings.NameOffsetX;
        Gauge.NameOffsetY = settings.NameOffsetY;
        Topmost = settings.AlwaysOnTop;
        PositionFromSettings(settings);
        ApplyWindowStyles();
        if (IsVisible)
            Gauge.InvalidateVisual();
    }

    public void Trigger(GaugeState state, string? name = null, string? adjective = null)
    {
        ApplySettings(_settingsService.Settings);
        _state = state;
        _name = name ?? _settingsService.Settings.Name;
        _adjective = adjective ?? _settingsService.Settings.Adjective;
        Gauge.State = _state;
        Gauge.GaugeName = _name;
        Gauge.Adjective = _adjective;
        Gauge.Time = 0;
        Gauge.InvalidateVisual();

        _clock.Restart();
        if (!IsVisible)
            Show();

        ApplyWindowStyles();
        if (_settingsService.Settings.AlwaysOnTop)
            Topmost = true;

        if (!_renderingHooked)
        {
            CompositionTarget.Rendering += OnRendering;
            _renderingHooked = true;
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var t = _clock.Elapsed.TotalSeconds;
        Gauge.Time = t;
        Gauge.InvalidateVisual();

        if (t >= 2.4 && !_isDragging)
            HideOverlay();
    }

    public void HideOverlay()
    {
        if (_renderingHooked)
        {
            CompositionTarget.Rendering -= OnRendering;
            _renderingHooked = false;
        }
        _clock.Stop();
        Hide();
    }

    public void ToggleVisible()
    {
        if (IsVisible)
            HideOverlay();
        else
            Trigger(_state == GaugeState.None ? GaugeState.Increase : _state);
    }

    private void PositionFromSettings(AppSettings settings)
    {
        var work = SystemParameters.WorkArea;
        var width = GaugeRenderer.ViewWidth * settings.Scale;
        var height = GaugeRenderer.ViewHeight * settings.Scale;
        const double margin = 16;

        double left;
        double top;
        switch (settings.Anchor)
        {
            case IndicatorAnchor.TopLeft:
                left = work.Left + margin;
                top = work.Top + 8;
                break;
            case IndicatorAnchor.TopCenter:
                left = work.Left + (work.Width - width) / 2;
                top = work.Top + 8;
                break;
            case IndicatorAnchor.Center:
                left = work.Left + (work.Width - width) / 2;
                top = work.Top + (work.Height - height) / 2;
                break;
            case IndicatorAnchor.BottomRight:
                left = work.Right - width - margin;
                top = work.Bottom - height - margin;
                break;
            case IndicatorAnchor.BottomLeft:
                left = work.Left + margin;
                top = work.Bottom - height - margin;
                break;
            case IndicatorAnchor.Custom:
                left = work.Left + settings.OffsetX;
                top = work.Top + settings.OffsetY;
                break;
            default:
                left = work.Right - width - margin;
                top = work.Top + 8;
                break;
        }

        if (settings.Anchor != IndicatorAnchor.Custom)
        {
            left += settings.OffsetX;
            top += settings.OffsetY;
        }

        Left = Math.Round(left);
        Top = Math.Round(top);
    }

    private void ApplyWindowStyles()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        var ex = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        ex |= WsExToolWindow | WsExNoActivate;
        if (_settingsService.Settings.ClickThrough)
            ex |= WsExTransparent;
        else
            ex &= ~WsExTransparent;
        SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(ex));
    }

    private void Root_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_settingsService.Settings.ClickThrough)
            return;

        _isDragging = true;
        try
        {
            DragMove();
        }
        catch
        {
            return;
        }
        finally
        {
            _isDragging = false;
        }

        var work = SystemParameters.WorkArea;
        var s = _settingsService.Settings.Clone();
        s.Anchor = IndicatorAnchor.Custom;
        s.OffsetX = Left - work.Left;
        s.OffsetY = Top - work.Top;
        _settingsService.Save(s);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}
