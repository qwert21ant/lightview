// Camera capabilities models synchronized with backend
export interface CameraCapabilities {
  supportsPtz: boolean;
  supportsAudio: boolean;
  supportsMotionDetection: boolean;
  supportsIrCut: boolean;
  supportsPresets: boolean;
  supportsSnapshot: boolean;
  supportsZoom: boolean;
  supportsFocus: boolean;
  supportsIris: boolean;
  supportedProfiles: string[];
  ptzCapabilities?: PtzCapabilities;
  imageCapabilities?: ImageCapabilities;
}

export interface PtzCapabilities {
  panRange: PtzRange;
  tiltRange: PtzRange;
  zoomRange: PtzRange;
  defaultSpeed: PtzSpeed;
  maxSpeed: PtzSpeed;
  maxPresets: number;
}

export interface PtzRange {
  min: number;
  max: number;
}

export interface PtzSpeed {
  pan: number;
  tilt: number;
  zoom: number;
}

export interface ImageCapabilities {
  brightnessRange: Range;
  contrastRange: Range;
  saturationRange: Range;
  sharpnessRange: Range;
  supportedFocusModes: FocusMode[];
  supportedIrModes: IrMode[];
}

export interface Range {
  min: number;
  max: number;
  step: number;
}

export enum FocusMode {
  Auto = 'Auto',
  Manual = 'Manual',
  SemiAuto = 'SemiAuto'
}

export enum IrMode {
  Auto = 'Auto',
  On = 'On',
  Off = 'Off'
}