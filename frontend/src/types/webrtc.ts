/**
 * WebRTC types and interfaces for video streaming
 */

export interface WebRTCConfiguration {
  iceServers: RTCIceServer[]
  iceCandidatePoolSize?: number
}

export interface WebRTCStreamOptions {
  streamUrl: string
  autoplay?: boolean
  muted?: boolean
  controls?: boolean
  playsInline?: boolean
}

export interface MediaMTXConnectionOptions {
  user?: string
  pass?: string
  token?: string
}

export interface WebRTCStats {
  bytesReceived: number
  packetsReceived: number
  packetsLost: number
  framesReceived: number
  framesDropped: number
  frameWidth: number
  frameHeight: number
  frameRate: number
  bitrate: number
  jitter: number
  roundTripTime: number
  timestamp: number
}

export interface WebRTCConnectionState {
  connectionState: RTCPeerConnectionState
  iceConnectionState: RTCIceConnectionState
  iceGatheringState: RTCIceGatheringState
  signalingState: RTCSignalingState
}

export enum WebRTCPlayerState {
  Idle = 'idle',
  Connecting = 'connecting',
  Connected = 'connected',
  Playing = 'playing',
  Paused = 'paused',
  Buffering = 'buffering',
  Error = 'error',
  Disconnected = 'disconnected'
}

export interface WebRTCPlayerEvents {
  stateChange: (state: WebRTCPlayerState) => void
  connectionStateChange: (state: WebRTCConnectionState) => void
  statsUpdate: (stats: WebRTCStats) => void
  error: (error: Error) => void
  streamReady: (stream: MediaStream) => void
  streamEnded: () => void
}