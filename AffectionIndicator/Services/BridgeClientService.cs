using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Windows;
using AffectionIndicator.Models;

namespace AffectionIndicator.Services;

public enum BridgeConnectionState
{
    Disabled,
    Starting,
    WaitingForBridge,
    Registering,
    Connecting,
    Connected,
    Reconnecting
}

public sealed record BridgeStatus(
    BridgeConnectionState State,
    string DeviceId,
    string Message);

public sealed class BridgeClientService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;
    private ClientWebSocket? _socket;
    private string? _serialHash;
    private bool _lastEnabled;
    private string _lastUrl = "";
    private BridgeStatus _status = new(BridgeConnectionState.Disabled, "", "Bridge disabled");

    public event EventHandler<BridgeStatus>? StatusChanged;
    public event EventHandler<string>? CommandReceived;

    public BridgeStatus Status => _status;

    public BridgeClientService(SettingsService settings)
    {
        _settings = settings;
        _lastEnabled = settings.Settings.BridgeEnabled;
        _lastUrl = settings.Settings.BridgeUrl;
    }

    public void Start()
    {
        _loop ??= Task.Run(() => RunAsync(_cts.Token));
    }

    public void ApplySettings(AppSettings settings)
    {
        var restart = settings.BridgeEnabled != _lastEnabled ||
                      !string.Equals(settings.BridgeUrl, _lastUrl, StringComparison.OrdinalIgnoreCase);
        _lastEnabled = settings.BridgeEnabled;
        _lastUrl = settings.BridgeUrl;

        if (restart)
        {
            try { _socket?.Abort(); } catch { }
        }
    }

    public void ForceReconnect()
    {
        try { _socket?.Abort(); } catch { }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        SetStatus(BridgeConnectionState.Starting, "Starting bridge client");

        while (!cancellationToken.IsCancellationRequested)
        {
            var s = _settings.Settings;

            if (!s.BridgeEnabled)
            {
                SetStatus(BridgeConnectionState.Disabled, "Bridge disabled", s.BridgeDeviceId);
                await DelayAsync(1200, cancellationToken);
                continue;
            }

            if (!TryGetBaseUri(s.BridgeUrl, out var baseUri))
            {
                SetStatus(BridgeConnectionState.WaitingForBridge,
                    "Bridge URL is not a valid http/https address", s.BridgeDeviceId);
                await DelayAsync(5000, cancellationToken);
                continue;
            }

            if (string.IsNullOrWhiteSpace(s.BridgeDeviceId) ||
                string.IsNullOrWhiteSpace(s.BridgeToken))
            {
                SetStatus(BridgeConnectionState.Registering,
                    "Requesting a device ID from the bridge", "");
                if (!await RegisterAsync(baseUri, cancellationToken))
                {
                    SetStatus(BridgeConnectionState.WaitingForBridge,
                        "Waiting for the bridge server", "");
                    await DelayAsync(8000, cancellationToken);
                }
                continue;
            }

            SetStatus(BridgeConnectionState.Connecting, "Connecting to the bridge",
                s.BridgeDeviceId);
            if (!await ConnectAsync(baseUri, s.BridgeDeviceId, s.BridgeToken, cancellationToken))
            {
                SetStatus(BridgeConnectionState.Reconnecting,
                    "Bridge unavailable; refreshing pairing", s.BridgeDeviceId);
                await DelayAsync(3000, cancellationToken);
                await RegisterAsync(baseUri, cancellationToken);
                await DelayAsync(3000, cancellationToken);
                continue;
            }

            SetStatus(BridgeConnectionState.Connected, "Connected to the bridge",
                s.BridgeDeviceId);
            await ReceiveLoopAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                break;

            SetStatus(BridgeConnectionState.Reconnecting, "Bridge connection lost",
                s.BridgeDeviceId);
            await DelayAsync(3000, cancellationToken);
        }
    }

    private async Task<bool> RegisterAsync(Uri baseUri, CancellationToken cancellationToken)
    {
        try
        {
            _serialHash ??= MachineIdentity.GetSerialHash();
            var request = new RegisterRequest(_serialHash, Environment.MachineName);
            using var response = await _http.PostAsJsonAsync(
                new Uri(baseUri, "/api/devices/register"), request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>(
                cancellationToken: cancellationToken);
            if (result == null || string.IsNullOrWhiteSpace(result.DeviceId) ||
                string.IsNullOrWhiteSpace(result.Token))
            {
                return false;
            }

            SaveDevice(result.DeviceId, result.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ConnectAsync(
        Uri baseUri,
        string deviceId,
        string token,
        CancellationToken cancellationToken)
    {
        var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

        try
        {
            var scheme = baseUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                ? "wss"
                : "ws";
            var builder = new UriBuilder(baseUri)
            {
                Scheme = scheme,
                Path = "/ws/device",
                Query = $"id={Uri.EscapeDataString(deviceId)}&token={Uri.EscapeDataString(token)}"
            };

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            await socket.ConnectAsync(builder.Uri, timeout.Token);
            _socket = socket;
            return true;
        }
        catch
        {
            socket.Dispose();
            return false;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var socket = _socket;
        if (socket == null)
            return;

        var buffer = new byte[8 * 1024];
        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();
                if (message.Length == 0 ||
                    message.Equals("pong", StringComparison.OrdinalIgnoreCase) ||
                    message.Equals("hello", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                CommandReceived?.Invoke(this, message);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            try { socket.Dispose(); } catch { }
            if (ReferenceEquals(_socket, socket))
                _socket = null;
        }
    }

    private void SaveDevice(string deviceId, string token)
    {
        void Apply()
        {
            var s = _settings.Settings.Clone();
            if (s.BridgeDeviceId == deviceId && s.BridgeToken == token)
                return;

            s.BridgeDeviceId = deviceId;
            s.BridgeToken = token;
            _settings.Save(s);
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
            dispatcher.Invoke(Apply);
        else
            Apply();
    }

    private void SetStatus(BridgeConnectionState state, string message, string? deviceId = null)
    {
        var status = new BridgeStatus(state, deviceId ?? _settings.Settings.BridgeDeviceId, message);
        _status = status;
        StatusChanged?.Invoke(this, status);
    }

    private static bool TryGetBaseUri(string value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
        {
            uri = parsed;
            return true;
        }

        uri = null!;
        return false;
    }

    private static async Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
    {
        try { await Task.Delay(milliseconds, cancellationToken); }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _socket?.Abort(); } catch { }
        _http.Dispose();
        _cts.Dispose();
    }

    private sealed record RegisterRequest(string SerialHash, string MachineName);

    private sealed record RegisterResponse(string DeviceId, string Token, bool IsNew);
}
