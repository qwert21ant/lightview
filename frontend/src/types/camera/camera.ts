import type { CameraCapabilities } from './capabilities';
import type { CameraProfile } from './stream';

// Base camera model synchronized with backend
export interface Camera {
  id: string;
  name: string;
  url: string;
  username: string;
  password: string;
  protocol: CameraProtocol;
  status: CameraStatus;
  capabilities?: CameraCapabilities;
  profiles: CameraProfile[];
  createdAt: string;
  lastConnectedAt: string;
  deviceInfo?: CameraDeviceInfo;
}

export interface CameraDeviceInfo {
  manufacturer?: string;
  model?: string;
  firmwareVersion?: string;
  serialNumber?: string;
}

export interface CameraCredentials {
  username: string;
  password: string;
}

export enum CameraStatus {
  Offline = 0,
  Online = 1,
  Connecting = 2,
  Error = 3,
  Maintenance = 4
}

export enum CameraProtocol {
  Onvif = 0,
  Rtsp = 1
}

// Request models
export interface AddCameraRequest {
  name: string;
  url: string;
  username: string;
  password: string;
  protocol: CameraProtocol;
}

export interface UpdateCameraRequest {
  id: string;
  name: string;
  url: string;
  username: string;
  password: string;
  protocol: CameraProtocol;
}

// Response models
export interface CameraStatusResponse {
  isOnline: boolean;
  status: string;
  lastSeen?: string;
  errorMessage?: string;
}