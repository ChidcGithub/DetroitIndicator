using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace BridgeServer;

public sealed class BridgeHub
{
    private sealed class Connection
    {
        public required WebSocket Socket { get; init; }
        public SemaphoreSlim SendLock { get; } = new(1, 1);
    }

    private readonly ConcurrentDictionary<string, Connection> _connections =
        new(StringComparer.OrdinalIgnoreCase);

    public bool IsConnected(string deviceId)
    {
        return _connections.TryGetValue(deviceId, out var connection) &&
               connection.Socket.State == WebSocketState.Open;
    }

    public int ConnectedCount => _connections.Count;

    public async Task RunAsync(string deviceId, WebSocket socket, CancellationToken cancellationToken)
    {
        var connection = new Connection { Socket = socket };

        if (_connections.TryRemove(deviceId, out var previous))
        {
            try { previous.Socket.Abort(); } catch { }
        }
        _connections[deviceId] = connection;

        var buffer = new byte[8 * 1024];
        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();
                    if (string.Equals(text, "ping", StringComparison.OrdinalIgnoreCase))
                        await SendToConnectionAsync(connection, "pong", cancellationToken);
                }
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
            if (_connections.TryGetValue(deviceId, out var current) &&
                ReferenceEquals(current, connection))
            {
                _connections.TryRemove(deviceId, out _);
            }

            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye",
                        CancellationToken.None);
                }
            }
            catch
            {
            }
        }
    }

    public async Task<bool> SendAsync(string deviceId, string text,
        CancellationToken cancellationToken = default)
    {
        if (!_connections.TryGetValue(deviceId, out var connection))
            return false;

        return await SendToConnectionAsync(connection, text, cancellationToken);
    }

    private static async Task<bool> SendToConnectionAsync(Connection connection, string text,
        CancellationToken cancellationToken)
    {
        var lockTaken = false;
        try
        {
            await connection.SendLock.WaitAsync(cancellationToken);
            lockTaken = true;
            if (connection.Socket.State != WebSocketState.Open)
                return false;

            var bytes = Encoding.UTF8.GetBytes(text);
            await connection.Socket.SendAsync(bytes, WebSocketMessageType.Text, true,
                cancellationToken);
            return true;
        }
        catch
        {
            try { connection.Socket.Abort(); } catch { }
            return false;
        }
        finally
        {
            if (lockTaken)
                connection.SendLock.Release();
        }
    }
}
