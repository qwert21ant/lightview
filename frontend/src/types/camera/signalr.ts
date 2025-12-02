import type { CameraStatus, CameraDeviceInfo } from './camera';
import type { CameraCapabilities } from './capabilities';
import type { CameraProfile } from './stream';
import type { PtzPosition, PtzMoveType } from './ptz';

// Base model for all camera change notifications from SignalR
export interface CameraChangedNotification {
  cameraId: string;
  eventType: string;
  data: any;
  timestamp: string;
}

// Data models for different types of camera change events
export interface CameraStatusChangedData {
  previousStatus: CameraStatus;
  currentStatus: CameraStatus;
  reason?: string;
}

export interface CameraErrorData {
  errorCode: string;
  errorMessage: string;
  severity: ErrorSeverity;
  isRecoverable: boolean;
}

export interface PtzMovedData {
  previousPosition?: PtzPosition;
  currentPosition?: PtzPosition;
  moveType: PtzMoveType;
}

export interface CameraStatisticsData {
  uptime: string; // TimeSpan as ISO duration string
  bytesReceived: number;
  averageFps: number;
  droppedFrames: number;
  averageLatency: string; // TimeSpan as ISO duration string
}

export interface CameraMetadataUpdatedData {
  profiles?: CameraProfile[];
  capabilities?: CameraCapabilities;
  deviceInfo?: CameraDeviceInfo;
  updateType: string;
}

// Supporting types
export interface CameraHealthStatus {
  isHealthy: boolean;
  checkedAt: string;
  issues: string[];
  lastError?: any;
}

export enum ErrorSeverity {
  Info = 0,
  Warning = 1,
  Error = 2,
  Critical = 3
}

// Constants for camera event types
export const CameraEventTypes = {
  StatusChanged: 'StatusChanged',
  Error: 'Error',
  PtzMoved: 'PtzMoved',
  Statistics: 'Statistics',
  MetadataUpdated: 'MetadataUpdated'
} as const;

export type CameraEventType = typeof CameraEventTypes[keyof typeof CameraEventTypes];

// Type guards for strongly-typed event handling
export function isCameraStatusChangedData(data: any): data is CameraStatusChangedData {
  return data && typeof data.previousStatus === 'number' && typeof data.currentStatus === 'number';
}

export function isCameraErrorData(data: any): data is CameraErrorData {
  return data && typeof data.errorCode === 'string' && typeof data.errorMessage === 'string';
}

export function isPtzMovedData(data: any): data is PtzMovedData {
  return data && typeof data.moveType === 'number';
}

export function isCameraStatisticsData(data: any): data is CameraStatisticsData {
  return data && typeof data.bytesReceived === 'number' && typeof data.averageFps === 'number';
}

export function isCameraMetadataUpdatedData(data: any): data is CameraMetadataUpdatedData {
  return data && typeof data.updateType === 'string';
}