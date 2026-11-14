import { DurableObject } from "cloudflare:workers";
import type { CommandPayload, Env } from "./env";
import { json } from "./http";

export class DeviceHub extends DurableObject<Env> {
  async fetch(request: Request): Promise<Response> {
    const url = new URL(request.url);

    if (url.pathname === "/connect") {
      return this.acceptConnection();
    }

    if (request.method === "POST" && url.pathname === "/command") {
      return this.command(request);
    }

    if (request.method === "GET" && url.pathname === "/status") {
      return this.status();
    }

    return json({ error: "not found" }, 404);
  }

  private async acceptConnection(): Promise<Response> {
    for (const existing of this.ctx.getWebSockets()) {
      try {
        existing.close(1000, "replaced");
      } catch {
        // The previous socket may already be closed.
      }
    }

    const pair = new WebSocketPair();
    this.ctx.acceptWebSocket(pair[1]);
    await this.ctx.storage.put("lastSeen", Date.now());

    return new Response(null, {
      status: 101,
      webSocket: pair[0]
    });
  }

  private async command(request: Request): Promise<Response> {
    let payload: CommandPayload;
    try {
      payload = (await request.json()) as CommandPayload;
    } catch {
      return json({ sent: 0, connected: false });
    }

    const messages = Array.isArray(payload.messages)
      ? payload.messages.filter((message): message is string => typeof message === "string")
      : [];

    const sockets = this.ctx.getWebSockets();
    let sent = 0;

    for (const socket of sockets) {
      if (socket.readyState !== 1) {
        continue;
      }

      for (const message of messages) {
        socket.send(message);
      }
      sent += 1;
    }

    if (sent > 0) {
      await this.ctx.storage.put("lastSeen", Date.now());
    }

    return json({ sent, connected: sent > 0 });
  }

  private async status(): Promise<Response> {
    const sockets = this.ctx.getWebSockets();
    const connected = sockets.some((socket) => socket.readyState === 1);
    const lastSeen = await this.ctx.storage.get<number>("lastSeen");

    return json({
      connected,
      lastSeenUtc: typeof lastSeen === "number" ? new Date(lastSeen).toISOString() : null
    });
  }

  async webSocketMessage(socket: WebSocket, message: string | ArrayBuffer): Promise<void> {
    if (typeof message === "string" && message.trim().toLowerCase() === "ping") {
      socket.send("pong");
    }
    await this.ctx.storage.put("lastSeen", Date.now());
  }

  async webSocketClose(
    _socket: WebSocket,
    _code: number,
    _reason: string,
    _wasClean: boolean
  ): Promise<void> {
    await this.ctx.storage.put("lastSeen", Date.now());
  }

  async webSocketError(_socket: WebSocket, _error: unknown): Promise<void> {
    await this.ctx.storage.put("lastSeen", Date.now());
  }
}
