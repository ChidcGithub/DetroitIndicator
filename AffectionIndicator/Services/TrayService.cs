using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AffectionIndicator.Models;
using AffectionIndicator.Rendering;
using AffectionIndicator.Services;

namespace AffectionIndicator.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SettingsService _settingsService;
    private readonly NetworkMonitor _networkMonitor;
    private readonly Func<bool> _isOverlayVisible;
    private readonly Action _showSettings;
    private readonly Action _toggleOverlay;
    private readonly Action<GaugeState> _preview;
    private readonly Action _exit;
    private readonly ToolStripMenuItem _alwaysOnTopItem;
    private readonly ToolStripMenuItem _autoSwitchItem;
    private readonly ToolStripMenuItem _networkItem;

    public TrayService(
        SettingsService settingsService,
        NetworkMonitor networkMonitor,
        Func<bool> isOverlayVisible,
        Action showSettings,
        Action toggleOverlay,
        Action<GaugeState> preview,
        Action exitAction)
    {
        _settingsService = settingsService;
        _networkMonitor = networkMonitor;
        _isOverlayVisible = isOverlayVisible;
        _showSettings = showSettings;
        _toggleOverlay = toggleOverlay;
        _preview = preview;
        _exit = exitAction;

        var menu = new ContextMenuStrip { ShowCheckMargin = true, ShowImageMargin = false };
        var header = new ToolStripMenuItem("Affection Indicator") { Enabled = false };
        menu.Items.Add(header);
        menu.Items.Add(new ToolStripSeparator());

        var toggle = new ToolStripMenuItem("Show indicator");
        toggle.Click += (_, _) => _toggleOverlay();
        menu.Items.Add(toggle);

        var previewMenu = new ToolStripMenuItem("Preview state");
        previewMenu.DropDownItems.Add(StateItem("Neutral", GaugeState.None));
        previewMenu.DropDownItems.Add(StateItem("Small increase", GaugeState.Increase));
        previewMenu.DropDownItems.Add(StateItem("Small decrease", GaugeState.Decrease));
        previewMenu.DropDownItems.Add(StateItem("Big increase", GaugeState.IncreasePlus));
        previewMenu.DropDownItems.Add(StateItem("Big decrease", GaugeState.DecreasePlus));
        previewMenu.DropDownItems.Add(StateItem("Path unlocked", GaugeState.Unlocked));
        previewMenu.DropDownItems.Add(StateItem("Path locked", GaugeState.Locked));
        menu.Items.Add(previewMenu);

        var settings = new ToolStripMenuItem("Settings...");
        settings.Click += (_, _) => _showSettings();
        menu.Items.Add(settings);
        menu.Items.Add(new ToolStripSeparator());

        _alwaysOnTopItem = new ToolStripMenuItem("Always on top") { CheckOnClick = true };
        _alwaysOnTopItem.Click += (_, _) =>
        {
            var s = _settingsService.Settings.Clone();
            s.AlwaysOnTop = _alwaysOnTopItem.Checked;
            _settingsService.Save(s);
        };
        menu.Items.Add(_alwaysOnTopItem);

        _autoSwitchItem = new ToolStripMenuItem("Switch on network change") { CheckOnClick = true };
        _autoSwitchItem.Click += (_, _) =>
        {
            var s = _settingsService.Settings.Clone();
            s.NetworkAutoSwitch = _autoSwitchItem.Checked;
            _settingsService.Save(s);
        };
        menu.Items.Add(_autoSwitchItem);

        _networkItem = new ToolStripMenuItem("Network: offline") { Enabled = false };
        menu.Items.Add(_networkItem);
        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => _exit();
        menu.Items.Add(exitItem);

        menu.Opening += (_, _) => RefreshMenu(toggle);
        _notifyIcon = new NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "Affection Indicator",
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => _showSettings();
        _networkMonitor.StatusChanged += OnNetworkChanged;
        UpdateNetworkItem();
    }

    private ToolStripMenuItem StateItem(string text, GaugeState state)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) => _preview(state);
        return item;
    }

    private void RefreshMenu(ToolStripMenuItem toggle)
    {
        toggle.Text = _isOverlayVisible() ? "Hide indicator" : "Show indicator";
        _alwaysOnTopItem.Checked = _settingsService.Settings.AlwaysOnTop;
        _autoSwitchItem.Checked = _settingsService.Settings.NetworkAutoSwitch;
        UpdateNetworkItem();
    }

    private void OnNetworkChanged(object? sender, bool online)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;
        if (dispatcher.CheckAccess())
            UpdateNetworkItem();
        else
            dispatcher.BeginInvoke(UpdateNetworkItem);
    }

    private void UpdateNetworkItem()
    {
        _networkItem.Text = _networkMonitor.IsOnline ? "Network: online" : "Network: offline";
    }

    private static Icon CreateIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var background = new SolidBrush(Color.FromArgb(28, 30, 36));
            g.FillEllipse(background, 1, 1, 30, 30);
            using var pen = new Pen(Color.FromArgb(128, 238, 255), 3.2f)
            {
                StartCap = LineCap.Flat,
                EndCap = LineCap.Flat,
                LineJoin = LineJoin.Miter
            };
            g.DrawLines(pen, new[]
            {
                new PointF(8, 20),
                new PointF(16, 11),
                new PointF(24, 20)
            });
            using var smallPen = new Pen(Color.FromArgb(128, 238, 255), 2.2f);
            g.DrawLines(smallPen, new[]
            {
                new PointF(10, 25),
                new PointF(16, 18.5f),
                new PointF(22, 25)
            });
        }

        var handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public void Dispose()
    {
        _networkMonitor.StatusChanged -= OnNetworkChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
