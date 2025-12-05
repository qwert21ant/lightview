<template>
  <div class="webrtc-video-player" :class="playerClasses">
    <!-- Video Element -->
    <div class="relative w-full h-full bg-black rounded-lg overflow-hidden">
      <video
        ref="videoElement"
        class="w-full h-full object-contain"
        :autoplay="options.autoplay"
        :muted="options.muted"
        :controls="showNativeControls"
        :playsinline="options.playsInline"
        @loadstart="onLoadStart"
        @loadedmetadata="onLoadedMetadata"
        @canplay="onCanPlay"
        @play="onPlay"
        @pause="onPause"
        @ended="onEnded"
        @error="onVideoError"
        @click="togglePlayPause"
      />
      
      <!-- Loading Overlay -->
      <div
        v-if="showLoadingOverlay"
        class="absolute inset-0 flex items-center justify-center bg-black/50"
      >
        <div class="flex flex-col items-center space-y-4 text-white">
          <div class="animate-spin rounded-full h-12 w-12 border-4 border-white border-t-transparent"></div>
          <div class="text-sm font-medium">
            {{ loadingText }}
          </div>
        </div>
      </div>
      
      <!-- Error Overlay -->
      <div
        v-if="showErrorOverlay"
        class="absolute inset-0 flex items-center justify-center bg-black/75"
      >
        <div class="flex flex-col items-center space-y-4 text-white max-w-md mx-4">
          <div class="w-16 h-16 rounded-full bg-red-500/20 flex items-center justify-center">
            <ExclamationTriangleIcon class="w-8 h-8 text-red-400" />
          </div>
          <div class="text-center">
            <h3 class="text-lg font-semibold mb-2">Connection Error</h3>
            <p class="text-sm text-gray-300 mb-4">
              {{ errorMessage }}
            </p>
            <button
              @click="retry"
              class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            >
              Retry Connection
            </button>
          </div>
        </div>
      </div>
      
      <!-- Custom Controls -->
      <div
        v-if="showCustomControls"
        class="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/70 to-transparent p-4"
        :class="{ 'opacity-0 pointer-events-none': !controlsVisible }"
        @mouseenter="showControls"
        @mouseleave="hideControls"
      >
        <div class="flex items-center justify-between text-white">
          <!-- Play/Pause Button -->
          <button
            @click="togglePlayPause"
            class="p-2 hover:bg-white/20 rounded-full transition-colors"
          >
            <PlayIcon v-if="!isVideoPlaying" class="w-5 h-5" />
            <PauseIcon v-else class="w-5 h-5" />
          </button>
          
          <!-- Connection Status -->
          <div class="flex items-center space-x-2">
            <div class="flex items-center space-x-1 text-xs">
              <div 
                class="w-2 h-2 rounded-full"
                :class="connectionStatusColor"
              ></div>
              <span>{{ connectionStatusText }}</span>
            </div>
            
            <!-- Stats Toggle -->
            <button
              v-if="showStatsToggle"
              @click="toggleStats"
              class="p-1 hover:bg-white/20 rounded text-xs"
            >
              Stats
            </button>
          </div>
          
          <!-- Fullscreen Button -->
          <button
            @click="toggleFullscreen"
            class="p-2 hover:bg-white/20 rounded-full transition-colors"
          >
            <ArrowsPointingOutIcon v-if="!isFullscreen" class="w-5 h-5" />
            <ArrowsPointingInIcon v-else class="w-5 h-5" />
          </button>
        </div>
      </div>
    </div>
    
    <!-- Statistics Panel -->
    <div
      v-if="showStats"
      class="absolute top-4 right-4 bg-black/80 text-white p-3 rounded-lg text-xs font-mono max-w-xs"
    >
      <div class="grid grid-cols-2 gap-2">
        <div>Resolution:</div>
        <div>{{ stats.frameWidth }}x{{ stats.frameHeight }}</div>
        
        <div>Frame Rate:</div>
        <div>{{ stats.frameRate?.toFixed(1) || 0 }} fps</div>
        
        <div>Bitrate:</div>
        <div>{{ formatBitrate(stats.bitrate) }}</div>
        
        <div>Packets:</div>
        <div>{{ stats.packetsReceived || 0 }}</div>
        
        <div>Lost:</div>
        <div>{{ stats.packetsLost || 0 }}</div>
        
        <div>RTT:</div>
        <div>{{ ((stats.roundTripTime || 0) * 1000).toFixed(0) }}ms</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import {
  PlayIcon,
  PauseIcon,
  ArrowsPointingOutIcon,
  ArrowsPointingInIcon,
  ExclamationTriangleIcon
} from '@heroicons/vue/24/outline'
import { useWebRTC } from '@/composables/useWebRTC'
import { mediaMTXService } from '@/services/mediaMTXService'
import type { WebRTCStreamOptions, WebRTCPlayerState } from '@/types/webrtc'

interface Props {
  streamUrl: string
  options?: Partial<WebRTCStreamOptions>
  autoConnect?: boolean
  showControls?: boolean
  showStats?: boolean
  retryAttempts?: number
  retryDelay?: number
}

const props = withDefaults(defineProps<Props>(), {
  options: () => ({
    autoplay: true,
    muted: true,
    controls: false,
    playsInline: true
  }),
  autoConnect: true,
  showControls: true,
  showStats: false,
  retryAttempts: 3,
  retryDelay: 2000
})

const emit = defineEmits<{
  'connected': []
  'disconnected': []
  'error': [error: Error]
  'stats': [stats: any]
}>()

// Template refs
const videoElement = ref<HTMLVideoElement | null>(null)

// Local state
const isVideoPlaying = ref(false)
const controlsVisible = ref(true)
const controlsTimer = ref<number | null>(null)
const isFullscreen = ref(false)
const showStatsPanel = ref(props.showStats)
const currentRetryAttempt = ref(0)

// WebRTC composable
const {
  remoteStream,
  playerState,
  connectionState,
  stats,
  isConnecting,
  isConnected,
  hasError,
  lastError,
  connect,
  disconnect,
  getStats,
  on,
  off
} = useWebRTC()

// Merged options
const options = computed(() => ({
  autoplay: true,
  muted: true,
  controls: false,
  playsInline: true,
  ...props.options
}))

// UI state
const showLoadingOverlay = computed(() => 
  playerState.value === 'connecting' || playerState.value === 'buffering'
)

const showErrorOverlay = computed(() => 
  playerState.value === 'error'
)

const showCustomControls = computed(() => 
  props.showControls && !options.value.controls
)

const showNativeControls = computed(() => 
  props.showControls && options.value.controls
)

const showStats = computed(() => 
  showStatsPanel.value && isConnected.value
)

const showStatsToggle = computed(() => 
  props.showStats || showStatsPanel.value
)

// Loading and error messages
const loadingText = computed(() => {
  switch (playerState.value) {
    case 'connecting':
      return currentRetryAttempt.value > 0 
        ? `Reconnecting... (${currentRetryAttempt.value}/${props.retryAttempts})`
        : 'Connecting to MediaMTX stream...'
    case 'buffering':
      return 'Buffering...'
    default:
      return 'Loading...'
  }
})

const errorMessage = computed(() => {
  if (lastError.value) {
    return lastError.value.message
  }
  return 'Failed to connect to MediaMTX stream'
})

// Connection status
const connectionStatusColor = computed(() => {
  switch (playerState.value) {
    case 'playing':
      return 'bg-green-500'
    case 'connecting':
      return 'bg-yellow-500'
    case 'error':
    case 'disconnected':
      return 'bg-red-500'
    default:
      return 'bg-gray-500'
  }
})

const connectionStatusText = computed(() => {
  switch (playerState.value) {
    case 'idle':
      return 'Idle'
    case 'connecting':
      return 'Connecting'
    case 'connected':
      return 'Connected'
    case 'playing':
      return 'Live'
    case 'paused':
      return 'Paused'
    case 'buffering':
      return 'Buffering'
    case 'error':
      return 'Error'
    case 'disconnected':
      return 'Disconnected'
    default:
      return 'Unknown'
  }
})

// Component classes
const playerClasses = computed(() => [
  'relative',
  'w-full',
  'h-full',
  {
    'cursor-pointer': showCustomControls.value,
    'opacity-75': isConnecting.value,
    'opacity-50': hasError.value
  }
])

// Set up WebRTC event listeners
function setupWebRTCEvents() {
  on('stateChange', (state: WebRTCPlayerState) => {
    console.log('MediaMTX player state changed:', state)
    
    if (state === 'connected' || state === 'playing') {
      emit('connected')
    } else if (state === 'disconnected') {
      emit('disconnected')
    }
  })

  on('error', (error: Error) => {
    console.error('WebRTC error:', error)
    emit('error', error)
    
    // Auto-retry on error
    if (currentRetryAttempt.value < props.retryAttempts) {
      setTimeout(() => {
        retry()
      }, props.retryDelay)
    }
  })

  on('statsUpdate', (statsData) => {
    emit('stats', statsData)
  })

  on('streamReady', (stream: MediaStream) => {
    attachStreamToVideo(stream)
  })

  on('streamEnded', () => {
    if (videoElement.value) {
      videoElement.value.srcObject = null
    }
  })
}

// Attach stream to video element
async function attachStreamToVideo(stream: MediaStream) {
  if (!videoElement.value) return

  try {
    videoElement.value.srcObject = stream
    
    if (options.value.autoplay) {
      await nextTick()
      await videoElement.value.play()
    }
  } catch (error) {
    console.error('Error attaching stream to video:', error)
  }
}

// Video event handlers
function onLoadStart() {
  console.log('Video load started')
}

function onLoadedMetadata() {
  console.log('Video metadata loaded')
}

function onCanPlay() {
  console.log('Video can play')
}

function onPlay() {
  isVideoPlaying.value = true
  console.log('Video playing')
}

function onPause() {
  isVideoPlaying.value = false
  console.log('Video paused')
}

function onEnded() {
  isVideoPlaying.value = false
  console.log('Video ended')
}

function onVideoError(event: Event) {
  console.error('Video error:', event)
  const video = event.target as HTMLVideoElement
  if (video.error) {
    const error = new Error(`Video error: ${video.error.message} (code: ${video.error.code})`)
    emit('error', error)
  }
}

// Control methods
function togglePlayPause() {
  if (!videoElement.value) return

  if (isVideoPlaying.value) {
    videoElement.value.pause()
  } else {
    videoElement.value.play().catch(error => {
      console.error('Error playing video:', error)
    })
  }
}

function toggleStats() {
  showStatsPanel.value = !showStatsPanel.value
}

function toggleFullscreen() {
  if (!document.fullscreenElement) {
    videoElement.value?.requestFullscreen().then(() => {
      isFullscreen.value = true
    }).catch(err => {
      console.error('Error entering fullscreen:', err)
    })
  } else {
    document.exitFullscreen().then(() => {
      isFullscreen.value = false
    }).catch(err => {
      console.error('Error exiting fullscreen:', err)
    })
  }
}

function showControls() {
  controlsVisible.value = true
  if (controlsTimer.value) {
    clearTimeout(controlsTimer.value)
  }
}

function hideControls() {
  if (controlsTimer.value) {
    clearTimeout(controlsTimer.value)
  }
  controlsTimer.value = setTimeout(() => {
    controlsVisible.value = false
  }, 3000)
}

// Connection methods
async function connectToStream() {
  if (!props.streamUrl) {
    console.warn('No stream URL provided')
    return
  }

  currentRetryAttempt.value++
  console.log(`Connecting to MediaMTX stream: ${props.streamUrl} (attempt ${currentRetryAttempt.value})`)
  
  // Validate MediaMTX URL format
  if (!mediaMTXService.validateStreamUrl(props.streamUrl)) {
    const error = new Error('Invalid MediaMTX WebRTC URL format')
    emit('error', error)
    return
  }
  
  const success = await connect(props.streamUrl)
  if (success) {
    currentRetryAttempt.value = 0
  }
}

async function retry() {
  if (currentRetryAttempt.value >= props.retryAttempts) {
    console.warn('Max retry attempts reached')
    return
  }

  await disconnect()
  await connectToStream()
}

// Utility methods
function formatBitrate(bitrate: number = 0): string {
  if (bitrate === 0) return '0 bps'
  
  const units = ['bps', 'Kbps', 'Mbps', 'Gbps']
  let value = bitrate
  let unitIndex = 0
  
  while (value >= 1000 && unitIndex < units.length - 1) {
    value /= 1000
    unitIndex++
  }
  
  return `${value.toFixed(1)} ${units[unitIndex]}`
}

// Statistics polling
let statsInterval: number | null = null

function startStatsPolling() {
  if (statsInterval) return
  
  statsInterval = setInterval(() => {
    if (isConnected.value) {
      getStats()
    }
  }, 1000)
}

function stopStatsPolling() {
  if (statsInterval) {
    clearInterval(statsInterval)
    statsInterval = null
  }
}

// Watch for stream URL changes
watch(() => props.streamUrl, async (newUrl, oldUrl) => {
  if (newUrl !== oldUrl) {
    await disconnect()
    if (newUrl && props.autoConnect) {
      currentRetryAttempt.value = 0
      await connectToStream()
    }
  }
})

// Watch for connection state changes
watch(isConnected, (connected) => {
  if (connected) {
    startStatsPolling()
  } else {
    stopStatsPolling()
  }
})

// Watch for remote stream changes
watch(remoteStream, (stream) => {
  if (stream) {
    attachStreamToVideo(stream)
  }
})

// Lifecycle
onMounted(() => {
  setupWebRTCEvents()
  
  if (props.streamUrl && props.autoConnect) {
    connectToStream()
  }
  
  // Hide controls initially if custom controls are shown
  if (showCustomControls.value) {
    hideControls()
  }
  
  // Handle fullscreen changes
  document.addEventListener('fullscreenchange', () => {
    isFullscreen.value = !!document.fullscreenElement
  })
})

onUnmounted(() => {
  stopStatsPolling()
  
  if (controlsTimer.value) {
    clearTimeout(controlsTimer.value)
  }
  
  // Clean up WebRTC events
  off('stateChange')
  off('error')
  off('statsUpdate')
  off('streamReady')
  off('streamEnded')
})

// Expose methods for parent components
defineExpose({
  connect: connectToStream,
  disconnect,
  retry,
  togglePlayPause,
  toggleFullscreen,
  toggleStats,
  getStats
})
</script>

<style scoped>
.webrtc-video-player {
  transition: all 0.3s ease;
}

.webrtc-video-player video {
  background-color: #000;
}

.webrtc-video-player .absolute {
  transition: opacity 0.3s ease;
}
</style>