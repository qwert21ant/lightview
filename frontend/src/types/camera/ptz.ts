import type { PtzSpeed, FocusMode, IrMode } from './capabilities';

// PTZ control models synchronized with backend
export interface PtzPosition {
  pan: number;
  tilt: number;
  zoom: number;
  timestamp: string;
}

export interface PtzMoveRequest {
  moveType: PtzMoveType;
  absolutePosition?: PtzPosition;
  relativeMovement?: PtzPosition;
  continuousSpeed?: PtzSpeed;
  speed?: PtzSpeed;
  duration?: number; // Duration in milliseconds for continuous movement
}

export enum PtzMoveType {
  Absolute = 'Absolute',
  Relative = 'Relative',
  Continuous = 'Continuous',
  Stop = 'Stop'
}

export interface PtzMoveResponse {
  newPosition: PtzPosition;
  isMoving: boolean;
  errorMessage?: string;
}

export interface PtzPreset {
  id: number;
  name: string;
  position: PtzPosition;
  createdAt: string;
  lastUsedAt?: string;
}

export interface PtzPresetRequest {
  name: string;
  position?: PtzPosition; // If null, uses current position
}

// Image Settings Models
export interface ImageSettings {
  brightness: number;
  contrast: number;
  saturation: number;
  sharpness: number;
  colorHue: number;
  whiteBalance: WhiteBalanceMode;
  exposureMode: ExposureMode;
  exposureTime?: number;
  exposureGain?: number;
  focusMode: FocusMode;
  focusDistance?: number;
  irCutFilter: IrMode;
}

export enum WhiteBalanceMode {
  Auto = 'Auto',
  Manual = 'Manual',
  Daylight = 'Daylight',
  Fluorescent = 'Fluorescent',
  Incandescent = 'Incandescent'
}

export enum ExposureMode {
  Auto = 'Auto',
  Manual = 'Manual',
  Shutter = 'Shutter',
  Iris = 'Iris'
}
