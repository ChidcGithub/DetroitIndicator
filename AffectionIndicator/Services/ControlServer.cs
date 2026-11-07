using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AffectionIndicator.Services;

public sealed class ControlServer : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;

    public bool IsRunning => _listener != null;
    public int Port { get; private set; }

    public event EventHandler<string>? CommandReceived;

    public void Start(int port)
    {
        Stop();
        try
        {
            Port = port;
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            _acceptTask = AcceptLoopAsync(_cts.Token);
        }
        catch
        {
            Stop();
        }
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        _listener = null;
        _cts?.Dispose();
        _cts = null;
        _acceptTask = null;
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener is { } listener)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(token);
                _ = HandleClientAsync(client, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                if (token.IsCancellationRequested)
                    break;
                await Task.Delay(250, token).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 5000;
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };

                await writer.WriteLineAsync("AFFECTION INDICATOR READY");
                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(token);
                    if (line == null)
                        break;

                    line = line.Trim();
                    if (line.Length == 0)
                        continue;

                    if (line.Equals("quit", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("BYE");
                        break;
                    }

                    CommandReceived?.Invoke(this, line);
                    await writer.WriteLineAsync("OK " + line);
                }
            }
            catch
            {
                // A dropped client is normal for a tiny control channel.
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
