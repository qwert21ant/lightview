<template>
  <div class="webrtc-video-player" :class="playerClasses">
    <!-- Video Element -->
    <div class="relative w-full h-full bg-black rounded-lg overflow-hidden">
      <video
        ref="videoElement"
        class="w-full h-full object-contain"
        :autoplay="options.autoplay"
        :muted="options.muted"
        :controls="false"
        :playsinline="options.playsInline"
        @loadstart="onLoadStart"
        @loadedmetadata="onLoadedMetadata"
        @canplay="onCanPlay"
        @play="onPlay"
        @pause="onPause"
        @ended="onEnded"
        @error="onVideoError"
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
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import {
  ExclamationTriangleIcon
} from '@heroicons/vue/24/outline'
import { useWebRTC } from '@/composables/useWebRTC'
import { mediaMTXService } from '@/services/mediaMTXService'
import type { WebRTCStreamOptions, WebRTCPlayerState } from '@/types/webrtc'

interface Props {
  streamUrl: string
  options?: Partial<WebRTCStreamOptions>
  autoConnect?: boolean
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

// Component classes
const playerClasses = computed(() => [
  'relative',
  'w-full',
  'h-full',
  {
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
  console.log('Video playing')
}

function onPause() {
  console.log('Video paused')
}

function onEnded() {
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
})

onUnmounted(() => {
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
  retry
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