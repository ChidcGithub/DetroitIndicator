export interface Env {
  REGISTRY: DurableObjectNamespace;
  DEVICE_HUB: DurableObjectNamespace;
  ASSETS: Fetcher;
}

export interface DeviceRecord {
  deviceId: string;
  serialHash: string;
  token: string;
  machineName: string;
  createdAt: string;
}

export interface RegisterRequest {
  serialHash?: string;
  machineName?: string;
}

export interface RegisterResponse {
  deviceId: string;
  token: string;
  isNew: boolean;
}

export interface ValidationResponse {
  valid: boolean;
  deviceId?: string;
  machineName?: string;
}

export interface ExistsResponse {
  exists: boolean;
  deviceId?: string;
  machineName?: string;
}

export interface CommandRequest {
  command?: string;
  name?: string;
  adjective?: string;
}

export interface CommandPayload {
  messages: string[];
}
