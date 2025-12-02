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
  Offline = 0,      // Camera is not connected
  Connecting = 1,   // Camera is in the process of connecting
  Online = 2,       // Camera is connected and healthy
  Degraded = 3,     // Camera is connected but experiencing health issues
  Error = 4         // Camera has encountered an error
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