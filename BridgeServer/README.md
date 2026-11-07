# Affection Indicator Bridge

A small ASP.NET Core bridge that connects the desktop overlay to the bridge
website and the Wear OS controller.

## Run

```powershell
dotnet run --project BridgeServer --urls http://0.0.0.0:5210
```

Then open <http://localhost:5210>.

The desktop app defaults to `http://localhost:5210`. The bridge must bind to
`0.0.0.0` (not just `localhost`) so a watch on the LAN can reach it.

## How pairing works

1. On first launch the desktop reads the motherboard serial (`Win32_BaseBoard`),
   hashes it with SHA-256, and sends the hash to
   `POST /api/devices/register`.
2. The bridge returns a 10-character **device ID** and a private **token**.
   The desktop stores both in
   `%APPDATA%\AffectionIndicator\settings.json`.
3. The desktop opens a WebSocket to `/ws/device?id=...&token=...` and waits.
4. The website or Wear OS app posts commands to
   `/api/devices/{id}/commands`; the bridge forwards them to the desktop over
   the WebSocket.

On later launches the desktop reuses the stored ID/token and skips
registration. If the connection fails it re-registers the same serial hash,
which is idempotent on the bridge.

## API

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/api/devices/register` | Register a serial hash; returns `deviceId`, `token`, `isNew` |
| `GET` | `/api/devices/{id}/status` | Returns `connected`, `machineName`, `lastSeenUtc` |
| `POST` | `/api/devices/{id}/commands` | Queue a command (`202`), offline (`409`), unknown ID (`404`) |
| `GET` | `/ws/device?id=&token=` | Desktop WebSocket channel |
| `GET` | `/api/health` | Bridge health and device counts |

Allowed commands: `inc`, `increase`, `dec`, `decrease`, `inc+`, `increaseplus`,
`biginc`, `dec+`, `decreaseplus`, `bigdec`, `unlock`, `unlocked`, `lock`,
`locked`, `neutral`, `none`, `show`, `hide`, `state:-1..5`.

The command request can also include optional `name` and `adjective` fields
(up to 32 characters each). The bridge sends `name:...` and `adj:...` to the
desktop before the state, so the desktop's displayed name/adjective update and
persist for later states.

### Register with PowerShell

```powershell
$body = @{
  serialHash  = ('a' * 64)
  machineName = 'test-pc'
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5210/api/devices/register `
  -ContentType application/json -Body $body
```

### Send a command

```powershell
Invoke-RestMethod -Method Post `
  -Uri http://localhost:5210/api/devices/<DEVICE_ID>/commands `
  -ContentType application/json -Body '{"command":"inc+"}'
```

Send a state with custom name/adjective (used by the Wear custom button):

```powershell
Invoke-RestMethod -Method Post `
  -Uri http://localhost:5210/api/devices/<DEVICE_ID>/commands `
  -ContentType application/json `
  -Body '{"command":"inc","name":"TARA","adjective":"HAPPY"}'
```

## Data

Registrations are stored in `BridgeServer/data/devices.json`; the folder is
gitignored because it contains tokens. The store is intentionally simple
(JSON file, in-memory connections) and is suitable for a small LAN bridge.

## Network and firewall

On the PC running the bridge, allow inbound TCP 5210 on the private network:

```powershell
New-NetFirewallRule -DisplayName "Affection Bridge" -Direction Inbound `
  -Protocol TCP -LocalPort 5210 -Action Allow -Profile Private
```

Find the PC's LAN address with `ipconfig` and enter it on the watch, for
example `http://192.168.1.20:5210`. The Android emulator uses
`http://10.0.2.2:5210` to reach the host.

## Security

This is a basic LAN bridge. The watch/website authenticate with the random
10-character device ID; the desktop uses the ID plus a private token. There is
no TLS, user account, or command authentication beyond the ID. Do not expose
port 5210 to the public internet. Use a trusted network, or put the service
behind a reverse proxy with HTTPS and access control.
