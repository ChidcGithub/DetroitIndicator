using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AffectionIndicator.Models;
using AffectionIndicator.Rendering;
using AffectionIndicator.Services;

namespace AffectionIndicator.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly NetworkMonitor _networkMonitor;
    private readonly ControlServer _controlServer;
    private readonly BridgeClientService _bridge;
    private readonly Action<GaugeState> _preview;
    private readonly Func<bool> _isOverlayVisible;
    private readonly System.Windows.Threading.DispatcherTimer _statusTimer;
    private bool _loading = true;
    private GaugeState _lastPreview = GaugeState.Increase;

    public bool AllowClose { get; set; }

    public SettingsWindow(
        SettingsService settingsService,
        NetworkMonitor networkMonitor,
        ControlServer controlServer,
        BridgeClientService bridge,
        Action<GaugeState> preview,
        Func<bool> isOverlayVisible)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _networkMonitor = networkMonitor;
        _controlServer = controlServer;
        _bridge = bridge;
        _preview = preview;
        _isOverlayVisible = isOverlayVisible;

        _networkMonitor.StatusChanged += OnNetworkStatusChanged;
        _bridge.StatusChanged += OnBridgeStatusChanged;
        _settingsService.Changed += OnSettingsChanged;
        Closing += (_, e) =>
        {
            if (!AllowClose)
            {
                e.Cancel = true;
                Hide();
            }
        };

        _statusTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };
        _statusTimer.Tick += (_, _) => SidebarStatus.Text = _isOverlayVisible()
            ? "Overlay visible"
            : "Overlay hidden";
        _statusTimer.Start();

        LoadFromSettings(settingsService.Settings);
        UpdateNetworkStatus(_networkMonitor.IsOnline);
        UpdateServerStatus();
        UpdateBridgeStatus(_bridge.Status);
        NavList.SelectedIndex = 0;
    }

    public void ShowWindow()
    {
        if (!IsVisible)
            Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
    }

    private void LoadFromSettings(AppSettings s)
    {
        _loading = true;
        AlwaysOnTopToggle.IsChecked = s.AlwaysOnTop;
        ClickThroughToggle.IsChecked = s.ClickThrough;
        AutoStartToggle.IsChecked = StartupService.IsEnabled();
        NetworkAutoToggle.IsChecked = s.NetworkAutoSwitch;
        ServerToggle.IsChecked = s.ControlServerEnabled;
        PortBox.Text = s.ControlServerPort.ToString();
        BridgeEnabledToggle.IsChecked = s.BridgeEnabled;
        BridgeUrlBox.Text = s.BridgeUrl;
        BridgeIdText.Text = string.IsNullOrWhiteSpace(s.BridgeDeviceId) ? "—" : s.BridgeDeviceId;
        NameBox.Text = s.Name;
        AdjectiveBox.Text = s.Adjective;
        OffsetXSlider.Value = s.OffsetX;
        OffsetYSlider.Value = s.OffsetY;
        ScaleSlider.Value = s.Scale;
        NameOffsetXSlider.Value = s.NameOffsetX;
        NameOffsetYSlider.Value = s.NameOffsetY;
        SetAnchorSelection(s.Anchor);
        UpdateSliderLabels();
        _loading = false;
    }

    private void OnSettingsChanged(object? sender, AppSettings s)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => OnSettingsChanged(sender, s)));
            return;
        }
        if (_loading)
            return;

        _loading = true;
        try
        {
            if (!AlwaysOnTopToggle.IsKeyboardFocusWithin)
                AlwaysOnTopToggle.IsChecked = s.AlwaysOnTop;
            if (!ClickThroughToggle.IsKeyboardFocusWithin)
                ClickThroughToggle.IsChecked = s.ClickThrough;
            if (!NetworkAutoToggle.IsKeyboardFocusWithin)
                NetworkAutoToggle.IsChecked = s.NetworkAutoSwitch;
            if (!ServerToggle.IsKeyboardFocusWithin)
                ServerToggle.IsChecked = s.ControlServerEnabled;
            if (!PortBox.IsKeyboardFocusWithin)
                PortBox.Text = s.ControlServerPort.ToString();
            if (!BridgeEnabledToggle.IsKeyboardFocusWithin)
                BridgeEnabledToggle.IsChecked = s.BridgeEnabled;
            if (!BridgeUrlBox.IsKeyboardFocusWithin)
                BridgeUrlBox.Text = s.BridgeUrl;
            BridgeIdText.Text = string.IsNullOrWhiteSpace(s.BridgeDeviceId) ? "—" : s.BridgeDeviceId;
            if (!NameBox.IsKeyboardFocusWithin)
                NameBox.Text = s.Name;
            if (!AdjectiveBox.IsKeyboardFocusWithin)
                AdjectiveBox.Text = s.Adjective;
            if (!OffsetXSlider.IsKeyboardFocusWithin)
                OffsetXSlider.Value = s.OffsetX;
            if (!OffsetYSlider.IsKeyboardFocusWithin)
                OffsetYSlider.Value = s.OffsetY;
            if (!ScaleSlider.IsKeyboardFocusWithin)
                ScaleSlider.Value = s.Scale;
            if (!NameOffsetXSlider.IsKeyboardFocusWithin)
                NameOffsetXSlider.Value = s.NameOffsetX;
            if (!NameOffsetYSlider.IsKeyboardFocusWithin)
                NameOffsetYSlider.Value = s.NameOffsetY;
            SetAnchorSelection(s.Anchor);
            UpdateSliderLabels();
        }
        finally
        {
            _loading = false;
        }
    }

    private void SetAnchorSelection(IndicatorAnchor anchor)
    {
        for (var i = 0; i < AnchorCombo.Items.Count; i++)
        {
            if (AnchorCombo.Items[i] is ComboBoxItem item &&
                item.Tag is string tag &&
                Enum.TryParse<IndicatorAnchor>(tag, out var parsed) &&
                parsed == anchor)
            {
                AnchorCombo.SelectedIndex = i;
                return;
            }
        }
        AnchorCombo.SelectedIndex = 0;
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;

        var s = _settingsService.Settings.Clone();
        s.AlwaysOnTop = AlwaysOnTopToggle.IsChecked == true;
        s.ClickThrough = ClickThroughToggle.IsChecked == true;
        s.AutoStart = AutoStartToggle.IsChecked == true;
        s.NetworkAutoSwitch = NetworkAutoToggle.IsChecked == true;
        s.ControlServerEnabled = ServerToggle.IsChecked == true;
        s.BridgeEnabled = BridgeEnabledToggle.IsChecked == true;
        if (int.TryParse(PortBox.Text, out var port))
            s.ControlServerPort = port;
        StartupService.SetEnabled(s.AutoStart);
        _settingsService.Save(s);
        UpdateServerStatus();
    }

    private void Preview_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } && Enum.TryParse<GaugeState>(tag, out var state))
        {
            _lastPreview = state;
            _preview(state);
        }
    }

    private void PreviewText_OnClick(object sender, RoutedEventArgs e)
    {
        var s = _settingsService.Settings.Clone();
        s.Name = NameBox.Text;
        s.Adjective = AdjectiveBox.Text;
        _settingsService.Save(s);
        _preview(_lastPreview);
    }

    private void Port_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;
        var s = _settingsService.Settings.Clone();
        if (int.TryParse(PortBox.Text, out var port))
        {
            s.ControlServerPort = port;
            _settingsService.Save(s);
            UpdateServerStatus();
        }
        PortBox.Text = _settingsService.Settings.ControlServerPort.ToString();
    }

    private void Anchor_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
            return;
        var s = _settingsService.Settings.Clone();
        if (AnchorCombo.SelectedItem is ComboBoxItem { Tag: string tag } &&
            Enum.TryParse<IndicatorAnchor>(tag, out var anchor))
        {
            s.Anchor = anchor;
        }
        _settingsService.Save(s);
    }

    private void Offset_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateSliderLabels();
        if (_loading)
            return;
        var s = _settingsService.Settings.Clone();
        s.OffsetX = OffsetXSlider.Value;
        s.OffsetY = OffsetYSlider.Value;
        if (s.Anchor != IndicatorAnchor.Custom)
            s.Anchor = IndicatorAnchor.Custom;
        _settingsService.Save(s);
    }

    private void Scale_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateSliderLabels();
        if (_loading)
            return;
        var s = _settingsService.Settings.Clone();
        s.Scale = ScaleSlider.Value;
        _settingsService.Save(s);
    }

    private void NameOffset_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateSliderLabels();
        if (_loading)
            return;
        var s = _settingsService.Settings.Clone();
        s.NameOffsetX = NameOffsetXSlider.Value;
        s.NameOffsetY = NameOffsetYSlider.Value;
        _settingsService.Save(s);
    }

    private void UpdateSliderLabels()
    {
        if (OffsetXLabel == null || OffsetYLabel == null || ScaleLabel == null ||
            NameOffsetXLabel == null || NameOffsetYLabel == null)
            return;
        OffsetXLabel.Text = $"{OffsetXSlider.Value:0} px";
        OffsetYLabel.Text = $"{OffsetYSlider.Value:0} px";
        ScaleLabel.Text = $"{ScaleSlider.Value:0.00}×";
        NameOffsetXLabel.Text = $"{NameOffsetXSlider.Value:0} px";
        NameOffsetYLabel.Text = $"{NameOffsetYSlider.Value:0} px";
    }

    private void ResetPosition_OnClick(object sender, RoutedEventArgs e)
    {
        var s = _settingsService.Settings.Clone();
        s.Anchor = IndicatorAnchor.TopRight;
        s.OffsetX = 0;
        s.OffsetY = 0;
        s.Scale = 1;
        _settingsService.Save(s);
        LoadFromSettings(s);
    }

    private void ResetName_OnClick(object sender, RoutedEventArgs e)
    {
        var s = _settingsService.Settings.Clone();
        s.NameOffsetX = 0;
        s.NameOffsetY = 0;
        _settingsService.Save(s);

        _loading = true;
        NameOffsetXSlider.Value = 0;
        NameOffsetYSlider.Value = 0;
        _loading = false;
        UpdateSliderLabels();
    }

    private void PreviewName_OnClick(object sender, RoutedEventArgs e)
    {
        _lastPreview = GaugeState.None;
        _preview(GaugeState.None);
    }

    private void RecheckNetwork_OnClick(object sender, RoutedEventArgs e)
    {
        _networkMonitor.Recheck();
        UpdateNetworkStatus(_networkMonitor.IsOnline);
    }

    private void OnNetworkStatusChanged(object? sender, bool online)
    {
        Dispatcher.Invoke(() => UpdateNetworkStatus(online));
    }

    private void UpdateNetworkStatus(bool online)
    {
        NetworkStatusText.Text = online ? "Online" : "Offline";
        NetworkDot.Fill = online
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x34, 0xC7, 0x59))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x3B, 0x30));
    }

    private void UpdateServerStatus()
    {
        ServerStatusText.Text = _controlServer.IsRunning
            ? $"Listening on 127.0.0.1:{_controlServer.Port}"
            : "Server stopped";
    }

    private void BridgeUrl_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;

        var value = BridgeUrlBox.Text.Trim().TrimEnd('/');
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            BridgeUrlBox.Text = _settingsService.Settings.BridgeUrl;
            return;
        }

        var s = _settingsService.Settings.Clone();
        if (!string.Equals(s.BridgeUrl, value, StringComparison.OrdinalIgnoreCase))
        {
            s.BridgeUrl = value;
            s.BridgeDeviceId = "";
            s.BridgeToken = "";
            _settingsService.Save(s);
            BridgeIdText.Text = "—";
            BridgeStatusText.Text = "Bridge URL changed; requesting a new ID…";
            _bridge.ForceReconnect();
        }

        BridgeUrlBox.Text = _settingsService.Settings.BridgeUrl;
    }

    private void CopyBridgeId_OnClick(object sender, RoutedEventArgs e)
    {
        var id = _settingsService.Settings.BridgeDeviceId;
        if (string.IsNullOrWhiteSpace(id))
        {
            BridgeStatusText.Text = "No device ID yet";
            return;
        }

        try
        {
            Clipboard.SetText(id);
            BridgeStatusText.Text = $"Copied {id}";
        }
        catch
        {
            BridgeStatusText.Text = "Could not copy the device ID";
        }
    }

    private void OpenBridge_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(_settingsService.Settings.BridgeUrl)
            {
                UseShellExecute = true
            });
        }
        catch
        {
            // The URL is validated before it is saved; a failed browser launch is harmless.
        }
    }

    private void ReconnectBridge_OnClick(object sender, RoutedEventArgs e)
    {
        BridgeStatusText.Text = "Reconnecting…";
        _bridge.ForceReconnect();
    }

    private void ResetBridgePairing_OnClick(object sender, RoutedEventArgs e)
    {
        var s = _settingsService.Settings.Clone();
        s.BridgeDeviceId = "";
        s.BridgeToken = "";
        _settingsService.Save(s);
        BridgeIdText.Text = "—";
        BridgeStatusText.Text = "Requesting a device ID…";
        _bridge.ForceReconnect();
    }

    private void OnBridgeStatusChanged(object? sender, BridgeStatus status)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => UpdateBridgeStatus(status)));
            return;
        }

        UpdateBridgeStatus(status);
    }

    private void UpdateBridgeStatus(BridgeStatus status)
    {
        BridgeStatusText.Text = status.Message;
        var color = status.State switch
        {
            BridgeConnectionState.Connected => Color.FromRgb(0x34, 0xC7, 0x59),
            BridgeConnectionState.Registering => Color.FromRgb(0xFF, 0xB3, 0x40),
            BridgeConnectionState.Connecting => Color.FromRgb(0xFF, 0xB3, 0x40),
            BridgeConnectionState.Starting => Color.FromRgb(0xFF, 0xB3, 0x40),
            BridgeConnectionState.Reconnecting => Color.FromRgb(0xFF, 0x45, 0x3A),
            BridgeConnectionState.WaitingForBridge => Color.FromRgb(0xFF, 0x45, 0x3A),
            _ => Color.FromRgb(0x8E, 0x8E, 0x93)
        };
        BridgeStatusDot.Fill = new SolidColorBrush(color);

        if (!string.IsNullOrWhiteSpace(status.DeviceId))
            BridgeIdText.Text = status.DeviceId;
        else if (string.IsNullOrWhiteSpace(_settingsService.Settings.BridgeDeviceId))
            BridgeIdText.Text = "—";
    }

    private void NavList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GeneralPage == null)
            return;

        GeneralPage.Visibility = NavList.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        AppearancePage.Visibility = NavList.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        NetworkPage.Visibility = NavList.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        BridgePage.Visibility = NavList.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
        AboutPage.Visibility = NavList.SelectedIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
    }
}
