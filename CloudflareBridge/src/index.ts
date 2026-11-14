import type {
  CommandRequest,
  Env,
  ExistsResponse,
  RegisterRequest,
  ValidationResponse
} from "./env";
import { apiJson, CORS_HEADERS } from "./http";

export { Registry } from "./registry";
export { DeviceHub } from "./deviceHub";

const REGISTRY_NAME = "registry";

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    if (request.method === "OPTIONS" && url.pathname.startsWith("/api/")) {
      return new Response(null, { status: 204, headers: CORS_HEADERS });
    }

    if (request.method === "GET" && url.pathname === "/api/health") {
      const registry = registryStub(env);
      const countResponse = await registry.fetch("https://registry.internal/count");
      const count = (await countResponse.json()) as { devices: number };
      return apiJson({
        ok: true,
        service: "affection-bridge",
        runtime: "cloudflare",
        devices: count.devices
      });
    }

    if (request.method === "POST" && url.pathname === "/api/devices/register") {
      let body: RegisterRequest;
      try {
        body = (await request.json()) as RegisterRequest;
      } catch {
        return apiJson({ error: "invalid JSON" }, 400);
      }

      const serialHash = String(body.serialHash ?? "").trim().toLowerCase();
      if (!/^[0-9a-f]{64}$/.test(serialHash)) {
        return apiJson({ error: "serialHash must be a SHA-256 hex string" }, 400);
      }

      const registry = registryStub(env);
      const response = await registry.fetch("https://registry.internal/register", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({
          serialHash,
          machineName: body.machineName
        })
      });

      return proxyJson(response);
    }

    const statusMatch = /^\/api\/devices\/([^/]+)\/status$/.exec(url.pathname);
    if (request.method === "GET" && statusMatch) {
      const deviceId = normalizeDeviceId(statusMatch[1]);
      if (!deviceId) {
        return apiJson({ error: "invalid device id" }, 400);
      }

      const device = await findDevice(env, deviceId);
      if (!device) {
        return apiJson({ error: "unknown device" }, 404);
      }

      const hub = hubStub(env, deviceId);
      const hubResponse = await hub.fetch("https://device-hub.internal/status");
      const hubData = (await hubResponse.json()) as {
        connected: boolean;
        lastSeenUtc: string | null;
      };

      return apiJson({
        deviceId,
        machineName: device.machineName ?? "",
        connected: hubData.connected,
        lastSeenUtc: hubData.lastSeenUtc
      });
    }

    const commandMatch = /^\/api\/devices\/([^/]+)\/commands$/.exec(url.pathname);
    if (request.method === "POST" && commandMatch) {
      const deviceId = normalizeDeviceId(commandMatch[1]);
      if (!deviceId) {
        return apiJson({ error: "invalid device id" }, 400);
      }

      let body: CommandRequest;
      try {
        body = (await request.json()) as CommandRequest;
      } catch {
        return apiJson({ error: "invalid JSON" }, 400);
      }

      const command = String(body.command ?? "").trim();
      if (!isAllowedCommand(command)) {
        return apiJson({ error: "unsupported command" }, 400);
      }

      const device = await findDevice(env, deviceId);
      if (!device) {
        return apiJson({ error: "unknown device" }, 404);
      }

      const name = cleanText(body.name);
      const adjective = cleanText(body.adjective);
      const messages: string[] = [];

      if (name) {
        messages.push(`name:${name}`);
      }
      if (adjective) {
        messages.push(`adj:${adjective}`);
      }
      messages.push(command);

      const hub = hubStub(env, deviceId);
      const hubResponse = await hub.fetch("https://device-hub.internal/command", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ messages })
      });
      const hubData = (await hubResponse.json()) as {
        sent: number;
        connected: boolean;
      };

      if (!hubData.connected || hubData.sent < 1) {
        return apiJson({ error: "desktop is not connected" }, 409);
      }

      return apiJson({ ok: true, command, name, adjective }, 202);
    }

    if (url.pathname === "/ws/device") {
      if (
        request.method !== "GET" ||
        (request.headers.get("Upgrade") ?? "").toLowerCase() !== "websocket"
      ) {
        return new Response("WebSocket required", { status: 426 });
      }

      const deviceId = normalizeDeviceId(url.searchParams.get("id") ?? "");
      const token = (url.searchParams.get("token") ?? "").trim();
      if (!deviceId || !token) {
        return new Response("Unauthorized", { status: 401 });
      }

      if (!(await validateToken(env, deviceId, token))) {
        return new Response("Unauthorized", { status: 401 });
      }

      const hub = hubStub(env, deviceId);
      return hub.fetch(new Request("https://device-hub.internal/connect", request));
    }

    if (url.pathname.startsWith("/api/")) {
      return apiJson({ error: "not found" }, 404);
    }

    return env.ASSETS.fetch(request);
  }
} satisfies ExportedHandler<Env>;

function registryStub(env: Env): DurableObjectStub {
  return env.REGISTRY.get(env.REGISTRY.idFromName(REGISTRY_NAME));
}

function hubStub(env: Env, deviceId: string): DurableObjectStub {
  return env.DEVICE_HUB.get(env.DEVICE_HUB.idFromName(deviceId));
}

async function findDevice(env: Env, deviceId: string): Promise<ExistsResponse | null> {
  const registry = registryStub(env);
  const response = await registry.fetch(
    `https://registry.internal/exists?id=${encodeURIComponent(deviceId)}`
  );
  if (!response.ok) {
    return null;
  }

  const data = (await response.json()) as ExistsResponse;
  return data.exists ? data : null;
}

async function validateToken(env: Env, deviceId: string, token: string): Promise<boolean> {
  const registry = registryStub(env);
  const response = await registry.fetch("https://registry.internal/validate", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ deviceId, token })
  });
  if (!response.ok) {
    return false;
  }

  const data = (await response.json()) as ValidationResponse;
  return data.valid;
}

async function proxyJson(response: Response): Promise<Response> {
  const body = await response.text();
  return new Response(body, {
    status: response.status,
    headers: {
      ...CORS_HEADERS,
      "content-type": "application/json; charset=utf-8",
      "cache-control": "no-store"
    }
  });
}

function normalizeDeviceId(value: string): string {
  try {
    return decodeURIComponent(value).trim().toUpperCase();
  } catch {
    return "";
  }
}

function cleanText(value: unknown): string | null {
  if (typeof value !== "string") {
    return null;
  }

  const text = [...value.trim()].filter((char) => char >= " ").join("").trim();
  if (!text) {
    return null;
  }
  return text.slice(0, 32);
}

function isAllowedCommand(command: string): boolean {
  const value = command.trim().toLowerCase();
  if (
    [
      "inc",
      "increase",
      "dec",
      "decrease",
      "inc+",
      "increaseplus",
      "biginc",
      "dec+",
      "decreaseplus",
      "bigdec",
      "unlock",
      "unlocked",
      "lock",
      "locked",
      "neutral",
      "none",
      "show",
      "hide"
    ].includes(value)
  ) {
    return true;
  }

  return /^state:-?[0-5]$/.test(value);
}
