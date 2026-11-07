using System.Net.WebSockets;
using System.Text.RegularExpressions;
using BridgeServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddSingleton(sp =>
    new DeviceStore(Path.Combine(builder.Environment.ContentRootPath, "data", "devices.json")));
builder.Services.AddSingleton<BridgeHub>();

var app = builder.Build();

app.UseCors();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", (DeviceStore store, BridgeHub hub) =>
    Results.Ok(new BridgeHealthResponse(true, store.Count, hub.ConnectedCount)));

app.MapPost("/api/devices/register",
    (RegisterRequest request, DeviceStore store, ILogger<Program> log) =>
    {
        var serialHash = (request.SerialHash ?? "").Trim();
        if (!IsSha256Hex(serialHash))
            return Results.BadRequest(new { error = "serialHash must be a SHA-256 hex string" });

        var machineName = (request.MachineName ?? "").Trim();
        if (machineName.Length > 80)
            machineName = machineName[..80];

        var device = store.Register(serialHash, machineName, out var isNew);
        log.LogInformation("Register {DeviceId} ({MachineName}) new={IsNew}",
            device.DeviceId, device.MachineName, isNew);
        return Results.Ok(new RegisterResponse(device.DeviceId, device.Token, isNew));
    });

app.MapGet("/api/devices/{deviceId}/status",
    (string deviceId, DeviceStore store, BridgeHub hub) =>
    {
        if (!store.TryGet(deviceId, out var device))
            return Results.NotFound(new { error = "unknown device" });

        return Results.Ok(new DeviceStatusResponse(
            device.DeviceId,
            device.MachineName,
            hub.IsConnected(device.DeviceId),
            device.LastSeenUtc));
    });

app.MapPost("/api/devices/{deviceId}/commands",
    async (string deviceId, CommandRequest request, DeviceStore store, BridgeHub hub,
        ILogger<Program> log) =>
    {
        if (!store.TryGet(deviceId, out var device))
            return Results.NotFound(new { error = "unknown device" });

        var command = (request.Command ?? "").Trim();
        if (!IsAllowedCommand(command))
            return Results.BadRequest(new { error = "unsupported command" });

        if (!hub.IsConnected(device.DeviceId))
            return Results.Conflict(new { error = "desktop is not connected" });

        var sent = await hub.SendAsync(device.DeviceId, command);
        if (!sent)
            return Results.Conflict(new { error = "desktop connection failed" });

        log.LogInformation("Command {Command} -> {DeviceId}", command, device.DeviceId);
        return Results.Accepted($"/api/devices/{device.DeviceId}/status",
            new { ok = true, command });
    });

app.Map("/ws/device", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket required");
        return;
    }

    var deviceId = context.Request.Query["id"].ToString().Trim();
    var token = context.Request.Query["token"].ToString().Trim();
    var store = context.RequestServices.GetRequiredService<DeviceStore>();
    var hub = context.RequestServices.GetRequiredService<BridgeHub>();

    if (string.IsNullOrWhiteSpace(deviceId) || !store.Validate(deviceId, token))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var socket = await context.WebSockets.AcceptWebSocketAsync();
    store.Touch(deviceId);
    app.Logger.LogInformation("Device {DeviceId} connected", deviceId);

    await hub.RunAsync(deviceId, socket, context.RequestAborted);

    store.Touch(deviceId);
    app.Logger.LogInformation("Device {DeviceId} disconnected", deviceId);
});

app.Run();

static bool IsSha256Hex(string value)
{
    if (value.Length != 64)
        return false;
    foreach (var c in value)
    {
        if (!Uri.IsHexDigit(c))
            return false;
    }
    return true;
}

static bool IsAllowedCommand(string command)
{
    var value = command.Trim().ToLowerInvariant();
    if (value is "inc" or "increase" or "dec" or "decrease"
        or "inc+" or "increaseplus" or "biginc"
        or "dec+" or "decreaseplus" or "bigdec"
        or "unlock" or "unlocked" or "lock" or "locked"
        or "neutral" or "none" or "show" or "hide")
    {
        return true;
    }

    return Regex.IsMatch(value, "^state:-?[0-5]$", RegexOptions.CultureInvariant);
}
