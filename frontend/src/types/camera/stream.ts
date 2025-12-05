// Stream management models synchronized with backend
export interface CameraProfile {
  token: string;
  name: string;
  video: VideoSettings;
  audio?: AudioSettings;
  originFeedUrl?: string;
  rtspUri?: string;
  webRtcUri?: string;
  isMainStream: boolean;
}

export interface VideoSettings {
  codec: string; // H.264, H.265, MJPEG
  resolution: Resolution;
  framerate: number;
  bitrate: number;
  bitrateControl: BitrateControl;
  quality: number; // 1-10
  govLength: number;
}

export interface AudioSettings {
  codec: string; // AAC, G.711, G.726
  bitrate: number;
  sampleRate: number;
  channels: number;
}

export interface Resolution {
  width: number;
  height: number;
}

export enum BitrateControl {
  CBR = 'CBR', // Constant Bitrate
  VBR = 'VBR'  // Variable Bitrate
}

export interface StreamConfiguration {
  cameraId: string;
  profileToken: string;
  streamType: StreamType;
  protocol: StreamProtocol;
  transportMode: TransportMode;
  multicast: boolean;
}

export enum StreamType {
  Live = 'Live',
  Playback = 'Playback'
}

export enum StreamProtocol {
  RTSP = 'RTSP',
  HTTP = 'HTTP',
  UDP = 'UDP',
  TCP = 'TCP'
}

export enum TransportMode {
  UDP = 'UDP',
  TCP = 'TCP',
  HTTP = 'HTTP'
}

export interface StreamUrlRequest {
  profileToken?: string;
  protocol?: StreamProtocol;
  transportMode?: TransportMode;
}

export interface StreamUrlResponse {
  streamUrl: string;
  profileToken?: string;
}

// Snapshot models
export interface SnapshotRequest {
  profileToken?: string;
  width?: number;
  height?: number;
  quality?: number; // 1-100
}

export interface SnapshotResponse {
  imageData: string; // Base64 encoded image
  contentType: string;
  timestamp: string;
}