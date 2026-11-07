using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using AffectionIndicator.Models;
using AffectionIndicator.Rendering;
using AffectionIndicator.Services;
using AffectionIndicator.Views;
using Application = System.Windows.Application;

namespace AffectionIndicator;

public partial class App : Application
{
    private const uint MbOk = 0x00000000;
    private const uint MbIconInformation = 0x00000040;
    private const uint MbTopmost = 0x00040000;
    private const uint MbSetForeground = 0x00010000;

    private Mutex? _singleInstance;
    private SettingsService? _settingsService;
    private NetworkMonitor? _networkMonitor;
    private ControlServer? _controlServer;
    private BridgeClientService? _bridge;
    private TrayService? _tray;
    private MainWindow? _overlay;
    private SettingsWindow? _settingsWindow;
    private bool _appliedServerEnabled;
    private int _appliedServerPort;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstance = new Mutex(true, "AffectionIndicator.SingleInstance", out var created);
        if (!created)
        {
            // The dialog is owned by this second process, so force it above the
            // already-running settings window instead of letting Windows place
            // it behind that window.
            MessageBoxW(IntPtr.Zero,
                "Affection Indicator is already running. Use its tray icon to open settings.",
                "Affection Indicator",
                MbOk | MbIconInformation | MbTopmost | MbSetForeground);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "AffectionIndicator-error.txt"),
                    args.Exception.ToString());
            }
            catch { }
            MessageBox.Show(args.Exception.Message, "Affection Indicator error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        _settingsService = new SettingsService();
        _networkMonitor = new NetworkMonitor();
        _controlServer = new ControlServer();
        _overlay = new MainWindow(_settingsService);
        _settingsService.Changed += OnSettingsChanged;
        _networkMonitor.StatusChanged += OnNetworkStatusChanged;
        _controlServer.CommandReceived += OnControlCommand;

        _bridge = new BridgeClientService(_settingsService);
        _bridge.CommandReceived += (_, command) => DispatchCommand(command);
        _bridge.Start();

        _appliedServerEnabled = _settingsService.Settings.ControlServerEnabled;
        _appliedServerPort = _settingsService.Settings.ControlServerPort;
        if (_appliedServerEnabled)
            _controlServer.Start(_appliedServerPort);

        _tray = new TrayService(
            _settingsService,
            _networkMonitor,
            () => _overlay?.IsVisible == true,
            ShowSettings,
            ToggleOverlay,
            Trigger,
            ExitApplication);

        if (!e.Args.Any(a => a.Equals("--autostart", StringComparison.OrdinalIgnoreCase)))
            ShowSettings();
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        _overlay?.ApplySettings(settings);
        _bridge?.ApplySettings(settings);
        if (_controlServer == null)
            return;

        if (settings.ControlServerEnabled != _appliedServerEnabled ||
            settings.ControlServerPort != _appliedServerPort)
        {
            _appliedServerEnabled = settings.ControlServerEnabled;
            _appliedServerPort = settings.ControlServerPort;
            if (_appliedServerEnabled)
                _controlServer.Start(_appliedServerPort);
            else
                _controlServer.Stop();
        }
    }

    private void OnNetworkStatusChanged(object? sender, bool online)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_settingsService?.Settings.NetworkAutoSwitch != true || _overlay == null)
                return;

            _overlay.Trigger(online ? GaugeState.Increase : GaugeState.Decrease);
        }));
    }

    private void OnControlCommand(object? sender, string command) => DispatchCommand(command);

    private void DispatchCommand(string command)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => DispatchCommand(command)));
            return;
        }

        var text = command.Trim();
        switch (text.ToLowerInvariant())
        {
                case "inc":
                case "increase":
                    Trigger(GaugeState.Increase);
                    break;
                case "inc+":
                case "increaseplus":
                case "biginc":
                    Trigger(GaugeState.IncreasePlus);
                    break;
                case "dec":
                case "decrease":
                    Trigger(GaugeState.Decrease);
                    break;
                case "dec+":
                case "decreaseplus":
                case "bigdec":
                    Trigger(GaugeState.DecreasePlus);
                    break;
                case "unlock":
                case "unlocked":
                    Trigger(GaugeState.Unlocked);
                    break;
                case "lock":
                case "locked":
                    Trigger(GaugeState.Locked);
                    break;
                case "neutral":
                case "none":
                    Trigger(GaugeState.None);
                    break;
                case "hide":
                    _overlay?.HideOverlay();
                    break;
                case "show":
                    Trigger(GaugeState.Increase);
                    break;
                default:
                    if (text.StartsWith("state:", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(text[6..], out var stateValue) &&
                        Enum.IsDefined(typeof(GaugeState), stateValue))
                    {
                        Trigger((GaugeState)stateValue);
                    }
                    else if (text.StartsWith("name:", StringComparison.OrdinalIgnoreCase) &&
                             _settingsService != null)
                    {
                        var s = _settingsService.Settings.Clone();
                        s.Name = text[5..];
                        _settingsService.Save(s);
                    }
                    else if (text.StartsWith("adj:", StringComparison.OrdinalIgnoreCase) &&
                             _settingsService != null)
                    {
                        var s = _settingsService.Settings.Clone();
                        s.Adjective = text[4..];
                        _settingsService.Save(s);
                    }
                    break;
            }
    }

    private void Trigger(GaugeState state)
    {
        _overlay?.Trigger(state);
    }

    private void ToggleOverlay()
    {
        if (_overlay == null)
            return;
        if (_overlay.IsVisible)
            _overlay.HideOverlay();
        else
            Trigger(GaugeState.Increase);
    }

    private void ShowSettings()
    {
        if (_settingsService == null || _networkMonitor == null ||
            _controlServer == null || _bridge == null)
        {
            return;
        }

        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow(
                _settingsService,
                _networkMonitor,
                _controlServer,
                _bridge,
                Trigger,
                () => _overlay?.IsVisible == true);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.ShowWindow();
    }

    private void ExitApplication()
    {
        try
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.AllowClose = true;
                _settingsWindow.Close();
            }
            _overlay?.Close();
            _tray?.Dispose();
            _controlServer?.Dispose();
            _networkMonitor?.Dispose();
            _bridge?.Dispose();
            _singleInstance?.ReleaseMutex();
        }
        catch
        {
            // Exit must always complete.
        }
        finally
        {
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _controlServer?.Dispose();
        _networkMonitor?.Dispose();
        _bridge?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
