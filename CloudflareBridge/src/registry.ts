import { DurableObject } from "cloudflare:workers";
import type {
  DeviceRecord,
  Env,
  ExistsResponse,
  RegisterRequest,
  RegisterResponse,
  ValidationResponse
} from "./env";
import { json } from "./http";

const ID_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

export class Registry extends DurableObject<Env> {
  async fetch(request: Request): Promise<Response> {
    const url = new URL(request.url);

    if (request.method === "POST" && url.pathname === "/register") {
      return this.register(request);
    }

    if (request.method === "POST" && url.pathname === "/validate") {
      return this.validate(request);
    }

    if (request.method === "GET" && url.pathname === "/exists") {
      return this.exists(url);
    }

    if (request.method === "GET" && url.pathname === "/count") {
      const devices = await this.ctx.storage.list({ prefix: "device:" });
      return json({ devices: devices.size });
    }

    return json({ error: "not found" }, 404);
  }

  private async register(request: Request): Promise<Response> {
    let body: RegisterRequest;
    try {
      body = (await request.json()) as RegisterRequest;
    } catch {
      return json({ error: "invalid JSON" }, 400);
    }

    const serialHash = String(body.serialHash ?? "").trim().toLowerCase();
    if (!/^[0-9a-f]{64}$/.test(serialHash)) {
      return json({ error: "serialHash must be a SHA-256 hex string" }, 400);
    }

    const machineName = cleanMachineName(body.machineName);
    const serialKey = `serial:${serialHash}`;
    const existingId = await this.ctx.storage.get<string>(serialKey);

    if (existingId) {
      const existing = await this.ctx.storage.get<DeviceRecord>(`device:${existingId}`);
      if (existing) {
        if (machineName && existing.machineName !== machineName) {
          await this.ctx.storage.put(`device:${existingId}`, {
            ...existing,
            machineName
          } satisfies DeviceRecord);
        }

        const response: RegisterResponse = {
          deviceId: existing.deviceId,
          token: existing.token,
          isNew: false
        };
        return json(response);
      }

      // Recover from a partially written registry record.
      await this.ctx.storage.delete(serialKey);
    }

    let deviceId = "";
    for (let attempt = 0; attempt < 12; attempt++) {
      deviceId = generateId();
      const collision = await this.ctx.storage.get(`device:${deviceId}`);
      if (!collision) {
        break;
      }
      deviceId = "";
    }

    if (!deviceId) {
      return json({ error: "could not allocate a device id" }, 503);
    }

    const record: DeviceRecord = {
      deviceId,
      serialHash,
      token: generateToken(),
      machineName: machineName || "Windows PC",
      createdAt: new Date().toISOString()
    };

    await this.ctx.storage.put(serialKey, deviceId);
    await this.ctx.storage.put(`device:${deviceId}`, record);

    const response: RegisterResponse = {
      deviceId: record.deviceId,
      token: record.token,
      isNew: true
    };
    return json(response);
  }

  private async validate(request: Request): Promise<Response> {
    let body: { deviceId?: string; token?: string };
    try {
      body = (await request.json()) as { deviceId?: string; token?: string };
    } catch {
      return json({ valid: false } satisfies ValidationResponse);
    }

    const deviceId = String(body.deviceId ?? "").trim().toUpperCase();
    const token = String(body.token ?? "").trim();
    if (!deviceId || !token) {
      return json({ valid: false } satisfies ValidationResponse);
    }

    const record = await this.ctx.storage.get<DeviceRecord>(`device:${deviceId}`);
    const valid = Boolean(record && safeEqual(record.token, token));
    const response: ValidationResponse = {
      valid,
      deviceId: record?.deviceId,
      machineName: record?.machineName
    };
    return json(response);
  }

  private async exists(url: URL): Promise<Response> {
    const deviceId = String(url.searchParams.get("id") ?? "").trim().toUpperCase();
    if (!deviceId) {
      return json({ exists: false } satisfies ExistsResponse);
    }

    const record = await this.ctx.storage.get<DeviceRecord>(`device:${deviceId}`);
    const response: ExistsResponse = {
      exists: Boolean(record),
      deviceId: record?.deviceId,
      machineName: record?.machineName
    };
    return json(response);
  }
}

export function cleanMachineName(value: unknown): string {
  if (typeof value !== "string") {
    return "";
  }

  const text = [...value.trim()].filter((char) => char >= " ").join("").trim();
  return text.slice(0, 80);
}

function generateId(): string {
  const bytes = new Uint8Array(10);
  crypto.getRandomValues(bytes);
  return Array.from(bytes, (byte) => ID_ALPHABET[byte % ID_ALPHABET.length]).join("");
}

function generateToken(): string {
  return crypto.randomUUID().replace(/-/g, "");
}

function safeEqual(a: string, b: string): boolean {
  if (a.length !== b.length) {
    return false;
  }

  let diff = 0;
  for (let i = 0; i < a.length; i++) {
    diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  }
  return diff === 0;
}
