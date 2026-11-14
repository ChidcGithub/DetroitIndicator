# Affection Indicator — Cloudflare Bridge

A Cloudflare Workers deployment of the Affection Indicator bridge. It exposes
the same API and WebSocket protocol as `BridgeServer`, serves the same control
website, and stores pairing data in Durable Objects.

## Architecture

```
Worker (src/index.ts)
  ├─ Registry Durable Object (singleton)
  │    serialHash -> device ID / token
  │    idempotent registration, token validation
  ├─ DeviceHub Durable Object (one per device)
  │    hibernating WebSocket, status, command relay
  └─ Static assets in public/
       index.html, styles.css, app.js (the control website)
```

- **Registry** is a single SQLite-backed Durable Object. Registration is
  serialized, so a serial hash maps to exactly one device ID.
- **DeviceHub** accepts the desktop WebSocket, keeps it alive with the
  hibernation API, and forwards commands from the Worker.
- **Static assets** are served by Cloudflare for `/`, `/styles.css`, and
  `/app.js`. The Worker runs first for `/api/*` and `/ws/*`.
- No D1 or KV namespace setup is required. The first deploy applies the
  Durable Object migration in `wrangler.jsonc`.

## Local development

```powershell
cd CloudflareBridge
npm install
npx wrangler dev --port 8787
```

Open <http://localhost:8787>. The Worker runs locally with local Durable
Objects, so this does not require a Cloudflare login.

For a local end-to-end test, set the desktop app's **Settings → Bridge → Bridge
URL** to `http://localhost:8787`. The desktop will register and receive a new
ID, which you can enter in the watch or website.

## Deploy

```powershell
cd CloudflareBridge
npx wrangler login
npx wrangler deploy
```

`wrangler deploy` prints the deployed Worker URL, for example:

```
https://affection-bridge.<your-subdomain>.workers.dev
```

Then:

1. Set the desktop **Bridge URL** to that HTTPS URL and let it register.
2. Open the Worker URL as the bridge website.
3. Put the same URL and the new device ID in the Wear OS app.

The Worker name is set in `wrangler.jsonc` as `affection-bridge`; change it if
that name is already used in your account.

> Durable Objects are required for the WebSocket and pairing store. Make sure
> your Workers plan supports the Durable Objects classes used here.

## API

The endpoints match the local bridge:

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/api/devices/register` | Register a SHA-256 serial hash; returns `deviceId`, `token`, `isNew` |
| `GET` | `/api/devices/{id}/status` | Returns `connected`, `machineName`, `lastSeenUtc` |
| `POST` | `/api/devices/{id}/commands` | Send a command; `202`, `404`, or `409` |
| `GET` | `/ws/device?id=&token=` | Desktop WebSocket channel |
| `GET` | `/api/health` | Health and device count |

The command body supports the same optional `name` and `adjective` fields:

```json
{
  "command": "inc",
  "name": "TARA",
  "adjective": "HAPPY"
}
```

The Worker sends `name:...`, `adj:...`, then the state over the WebSocket, just
like the local bridge.

## Verified locally

The local Worker (`wrangler dev`) was tested with a Node WebSocket client:

- Registration returned a device ID and token.
- Registering the same serial hash returned the same ID and `isNew: false`.
- WebSocket connected and status returned `connected: true`.
- A command arrived in order as `name:TARA`, `adj:HAPPY`, `inc`.
- Commands returned `409` when no desktop socket was connected.
- `/api/health` returned the device count.
- The static website returned `200`.
- Unknown IDs returned `404`; an invalid token was rejected.

## Security

The deployed Worker is public on the internet. The watch/website use the random
10-character device ID, and the desktop authenticates the WebSocket with a
private token. There is no user account system, so treat the ID as a secret and
use the deployed URL only with people you trust. Cloudflare provides HTTPS/WSS;
do not disable it.

The control website in `public/` is copied from `BridgeServer/wwwroot` so both
deployments serve the same UI. Keep them in sync if you change the website.
