// Event handler types for SignalR camera events
import type { Camera } from './camera';
import type { PtzMoveResponse } from './ptz';

export interface CameraEventHandlers {
  onCameraAdded?: (camera: Camera) => void;
  onCameraUpdated?: (camera: Camera) => void;
  onCameraDeleted?: (cameraId: string) => void;
  onCameraConnected?: (cameraId: string) => void;
  onCameraDisconnected?: (cameraId: string) => void;
  onCameraStatusChanged?: (cameraId: string, status: string) => void;
  onPtzMoved?: (cameraId: string, moveType: string, response: PtzMoveResponse) => void;
  onPtzStopped?: (cameraId: string) => void;
  onCameraEvent?: (cameraId: string, eventType: string, data: any) => void;
  onStreamStarted?: (cameraId: string, streamUrl: string) => void;
  onStreamStopped?: (cameraId: string) => void;
  onMotionDetected?: (cameraId: string, timestamp: string, metadata: any) => void;
  onRecordingStarted?: (cameraId: string, recordingId: string) => void;
  onRecordingStopped?: (cameraId: string, recordingId: string) => void;
}

// Hub group management
export interface CameraGroupOperations {
  joinGroup: (groupName: string) => Promise<void>;
  leaveGroup: (groupName: string) => Promise<void>;
  sendToGroup: (groupName: string, method: string, ...args: any[]) => Promise<void>;
}

// Common event data structures
export interface CameraEvent {
  cameraId: string;
  eventType: string;
  timestamp: string;
  data?: any;
}

export interface MotionDetectionEvent {
  cameraId: string;
  timestamp: string;
  confidence: number;
  boundingBoxes: BoundingBox[];
  snapshot?: string; // Base64 encoded image
}

export interface BoundingBox {
  x: number;
  y: number;
  width: number;
  height: number;
  confidence: number;
  label?: string;
}

export interface RecordingEvent {
  cameraId: string;
  recordingId: string;
  timestamp: string;
  duration?: number; // in seconds
  fileSize?: number; // in bytes
  filePath?: string;
}