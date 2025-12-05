import { ref, onUnmounted, computed, reactive, readonly } from 'vue'
import type {
  WebRTCConfiguration,
  WebRTCStreamOptions,
  WebRTCStats,
  WebRTCConnectionState,
  WebRTCPlayerState,
  WebRTCPlayerEvents
} from '@/types/webrtc'
import { MediaMTXWebRTCReader, type MediaMTXReaderConfig } from '@/mediamtx/MediaMTXWebRTCReader'

export function useWebRTC() {
  // Reactive state
  const mediaMtxReader = ref<MediaMTXWebRTCReader | null>(null)
  const remoteStream = ref<MediaStream | null>(null)
  const playerState = ref<WebRTCPlayerState>('idle' as WebRTCPlayerState)
  const isConnecting = ref(false)
  const isConnected = ref(false)
  const lastError = ref<Error | null>(null)
  
  // Connection state tracking
  const connectionState = reactive<WebRTCConnectionState>({
    connectionState: 'new' as RTCPeerConnectionState,
    iceConnectionState: 'new' as RTCIceConnectionState,
    iceGatheringState: 'new' as RTCIceGatheringState,
    signalingState: 'stable' as RTCSignalingState
  })

  // Statistics tracking
  const stats = reactive<Partial<WebRTCStats>>({
    bytesReceived: 0,
    packetsReceived: 0,
    packetsLost: 0,
    framesReceived: 0,
    framesDropped: 0,
    frameWidth: 0,
    frameHeight: 0,
    frameRate: 0,
    bitrate: 0,
    jitter: 0,
    roundTripTime: 0,
    timestamp: 0
  })

  // Event callbacks
  const eventCallbacks: Partial<WebRTCPlayerEvents> = {}

  // Computed properties
  const isIdle = computed(() => playerState.value === 'idle')
  const isPlaying = computed(() => playerState.value === 'playing')
  const hasError = computed(() => playerState.value === 'error')
  const canPlay = computed(() => remoteStream.value !== null)

  /**
   * Parse MediaMTX WebRTC URL and extract authentication info
   */
  function parseWebRTCUrl(url: string) {
    try {
      const urlObj = new URL(url)
      const config: any = {
        url: url
      }
      
      // Extract authentication from URL
      if (urlObj.username) {
        config.user = decodeURIComponent(urlObj.username)
      }
      if (urlObj.password) {
        config.pass = decodeURIComponent(urlObj.password)
      }
      
      // Check for token in query params
      const token = urlObj.searchParams.get('token')
      if (token) {
        config.token = token
      }
      
      return config
    } catch (error) {
      throw new Error(`Invalid WebRTC URL: ${error}`)
    }
  }

  /**
   * Handle MediaMTX reader errors
   */
  function handleMediaMTXError(errorMessage: string) {
    const error = new Error(errorMessage)
    lastError.value = error
    playerState.value = 'error' as WebRTCPlayerState
    isConnecting.value = false
    isConnected.value = false
    eventCallbacks.error?.(error)
    eventCallbacks.stateChange?.(playerState.value)
  }

  /**
   * Handle MediaMTX track events
   */
  function handleMediaMTXTrack(event: RTCTrackEvent) {
    const [stream] = event.streams
    if (stream) {
      console.log('MediaMTX track received:', event.track.kind)
      
      // Create or update remote stream
      if (!remoteStream.value) {
        remoteStream.value = new MediaStream()
      }
      
      // Add track to stream if not already present
      const existingTrack = remoteStream.value.getTracks()
        .find(t => t.kind === event.track.kind)
      
      if (!existingTrack) {
        remoteStream.value.addTrack(event.track)
      }
      
      // Update state
      playerState.value = 'playing' as WebRTCPlayerState
      isConnecting.value = false
      isConnected.value = true
      
      eventCallbacks.streamReady?.(remoteStream.value)
      eventCallbacks.stateChange?.(playerState.value)
      
      // Set up track event listeners
      event.track.addEventListener('ended', () => {
        if (remoteStream.value) {
          remoteStream.value.removeTrack(event.track)
          
          // If no more tracks, consider stream ended
          if (remoteStream.value.getTracks().length === 0) {
            remoteStream.value = null
            playerState.value = 'disconnected' as WebRTCPlayerState
            isConnected.value = false
            eventCallbacks.streamEnded?.()
            eventCallbacks.stateChange?.(playerState.value)
          }
        }
      })
    }
  }

  /**
   * Connect to MediaMTX WebRTC stream
   */
  async function connect(streamUrl: string, options?: { user?: string, pass?: string, token?: string }) {
    try {
      // Disconnect any existing connection
      await disconnect()

      // Parse URL and prepare configuration
      const urlConfig = parseWebRTCUrl(streamUrl)
      
      // Override with provided options
      if (options) {
        if (options.user) urlConfig.user = options.user
        if (options.pass) urlConfig.pass = options.pass
        if (options.token) urlConfig.token = options.token
      }

      // Set up MediaMTX reader configuration
      const readerConfig: MediaMTXReaderConfig = {
        url: urlConfig.url,
        user: urlConfig.user,
        pass: urlConfig.pass,
        token: urlConfig.token,
        onError: handleMediaMTXError,
        onTrack: handleMediaMTXTrack
      }

      console.log('Connecting to MediaMTX WebRTC stream:', streamUrl)
      
      // Update state
      playerState.value = 'connecting' as WebRTCPlayerState
      isConnecting.value = true
      isConnected.value = false
      lastError.value = null
      eventCallbacks.stateChange?.(playerState.value)

      // Create MediaMTX reader
      mediaMtxReader.value = new MediaMTXWebRTCReader(readerConfig)
      
      return true
    } catch (error) {
      const err = error instanceof Error ? error : new Error('Failed to connect to MediaMTX stream')
      handleMediaMTXError(err.message)
      return false
    }
  }

  /**
   * Disconnect from MediaMTX WebRTC stream
   */
  async function disconnect() {
    try {
      // Close MediaMTX reader
      if (mediaMtxReader.value) {
        mediaMtxReader.value.close()
        mediaMtxReader.value = null
      }

      // Clean up remote stream
      if (remoteStream.value) {
        remoteStream.value.getTracks().forEach(track => track.stop())
        remoteStream.value = null
      }

      // Reset state
      playerState.value = 'idle' as WebRTCPlayerState
      isConnecting.value = false
      isConnected.value = false
      lastError.value = null
      
      eventCallbacks.stateChange?.(playerState.value)
    } catch (error) {
      console.error('Error during MediaMTX disconnect:', error)
    }
  }

  /**
   * Initialize MediaMTX reader (for compatibility)
   */
  async function initializePeerConnection() {
    // This method is kept for API compatibility but not used with MediaMTX
    console.warn('initializePeerConnection is not used with MediaMTX WebRTC Reader')
    return null
  }

  /**
   * Get connection statistics from MediaMTX
   */
  async function getStats() {
    if (!mediaMtxReader.value || !mediaMtxReader.value.pc) return null

    try {
      const pc = mediaMtxReader.value.pc
      const statsReport = await pc.getStats() 
      const newStats: Partial<WebRTCStats> = { timestamp: Date.now() }

      statsReport.forEach((report: any) => {
        if (report.type === 'inbound-rtp' && report.mediaType === 'video') {
          newStats.bytesReceived = report.bytesReceived || 0
          newStats.packetsReceived = report.packetsReceived || 0
          newStats.packetsLost = report.packetsLost || 0
          newStats.framesReceived = report.framesReceived || 0
          newStats.framesDropped = report.framesDropped || 0
          newStats.frameWidth = report.frameWidth || 0
          newStats.frameHeight = report.frameHeight || 0
          newStats.frameRate = report.framesPerSecond || 0
          newStats.jitter = report.jitter || 0
        }

        if (report.type === 'candidate-pair' && report.state === 'succeeded') {
          newStats.roundTripTime = report.currentRoundTripTime || 0
        }
      })

      // Calculate bitrate
      if (stats.bytesReceived && stats.timestamp) {
        const timeDiff = (newStats.timestamp! - stats.timestamp) / 1000
        const bytesDiff = (newStats.bytesReceived || 0) - stats.bytesReceived
        newStats.bitrate = timeDiff > 0 ? Math.round((bytesDiff * 8) / timeDiff) : 0
      }

      Object.assign(stats, newStats)
      eventCallbacks.statsUpdate?.(stats as WebRTCStats)

      return stats as WebRTCStats
    } catch (error) {
      console.error('Error getting MediaMTX WebRTC stats:', error)
      return null
    }
  }

  /**
   * Register event callbacks
   */
  function on<K extends keyof WebRTCPlayerEvents>(event: K, callback: WebRTCPlayerEvents[K]) {
    eventCallbacks[event] = callback as any
  }

  /**
   * Unregister event callbacks
   */
  function off<K extends keyof WebRTCPlayerEvents>(event: K) {
    delete eventCallbacks[event]
  }

  // Cleanup on unmount
  onUnmounted(() => {
    disconnect()
  })

  return {
    // State
    mediaMtxReader: readonly(mediaMtxReader),
    remoteStream: readonly(remoteStream),
    playerState: readonly(playerState),
    connectionState: readonly(connectionState),
    stats: readonly(stats),
    isConnecting: readonly(isConnecting),
    isConnected: readonly(isConnected),
    lastError: readonly(lastError),
    
    // Computed
    isIdle,
    isPlaying,
    hasError,
    canPlay,

    // Methods
    initializePeerConnection,
    connect,
    disconnect,
    getStats,
    on,
    off
  }
}