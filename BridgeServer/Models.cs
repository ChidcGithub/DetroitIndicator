using System.Text.Json.Serialization;

namespace BridgeServer;

public sealed class DeviceRecord
{
    public string DeviceId { get; set; } = "";
    public string SerialHash { get; set; } = "";
    public string Token { get; set; } = "";
    public string MachineName { get; set; } = "";
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSeenUtc { get; set; }
}

public sealed record RegisterRequest(string SerialHash, string? MachineName);

public sealed record RegisterResponse(string DeviceId, string Token, bool IsNew);

public sealed record CommandRequest(string Command);

public sealed record DeviceStatusResponse(
    string DeviceId,
    string MachineName,
    bool Connected,
    DateTimeOffset? LastSeenUtc);

public sealed record BridgeHealthResponse(bool Ok, int Devices, int Connected);
