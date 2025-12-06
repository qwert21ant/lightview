<template>
  <div class="camera-stream-viewer" :class="viewerClasses">
    <!-- Camera Header -->
    <div v-if="showHeader" class="camera-header">
      <div class="flex items-center justify-between p-3 bg-gray-900 text-white">
        <div class="flex items-center space-x-3">
          <div class="flex items-center space-x-2">
            <div 
              class="w-3 h-3 rounded-full"
              :class="statusColor"
            ></div>
            <h3 class="font-medium">{{ camera?.name || 'Camera Stream' }}</h3>
          </div>
          
          <div v-if="camera?.deviceInfo" class="text-xs text-gray-400">
            {{ camera.deviceInfo.manufacturer }} {{ camera.deviceInfo.model }}
          </div>
        </div>
        
        <div class="flex items-center space-x-2">
          <!-- Stream Quality Selector -->
          <select
            v-if="availableStreams.length > 1"
            v-model="selectedStreamUrl"
            class="bg-gray-800 text-white text-sm px-2 py-1 rounded border border-gray-600 focus:border-blue-500"
          >
            <option
              v-for="stream in availableStreams"
              :key="stream.url"
              :value="stream.url"
            >
              {{ stream.label }}
            </option>
          </select>
          
          <!-- Actions Menu -->
          <div class="relative" v-if="showActions">
            <button
              @click="toggleActionsMenu"
              class="p-1 hover:bg-gray-800 rounded"
            >
              <EllipsisVerticalIcon class="w-4 h-4" />
            </button>
            
            <!-- Actions Dropdown -->
            <div
              v-if="showActionsMenu"
              class="absolute right-0 mt-1 bg-gray-800 border border-gray-600 rounded-lg shadow-lg z-50 min-w-[160px]"
              @click.stop
            >
              <button
                @click="takeSnapshot"
                class="w-full text-left px-3 py-2 hover:bg-gray-700 text-sm"
              >
                Take Snapshot
              </button>
              <button
                @click="toggleRecording"
                class="w-full text-left px-3 py-2 hover:bg-gray-700 text-sm"
              >
                {{ isRecording ? 'Stop Recording' : 'Start Recording' }}
              </button>
              <hr class="border-gray-600">
              <button
                @click="reconnect"
                class="w-full text-left px-3 py-2 hover:bg-gray-700 text-sm"
              >
                Reconnect
              </button>
              <button
                @click="$emit('configure')"
                class="w-full text-left px-3 py-2 hover:bg-gray-700 text-sm"
              >
                Configure
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <!-- WebRTC Video Player -->
    <div class="video-container" :style="containerStyle">
      <WebRTCVideoPlayer
        ref="videoPlayer"
        :stream-url="selectedStreamUrl"
        :options="videoOptions"
        :auto-connect="autoConnect"
        :show-controls="showVideoControls"
        :show-stats="showStats"
        :retry-attempts="retryAttempts"
        :retry-delay="retryDelay"
        @connected="onConnected"
        @disconnected="onDisconnected"
        @error="onError"
        @stats="onStats"
      />
    </div>
    
    <!-- Camera Info Overlay -->
    <div
      v-if="showInfo && camera"
      class="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/80 to-transparent p-4 text-white text-sm"
    >
      <div class="flex items-center justify-between">
        <div>
          <div class="font-medium">{{ camera.name }}</div>
          <div class="text-xs text-gray-300">
            {{ formatLastSeen(camera.lastConnectedAt) }}
          </div>
        </div>
        
        <div v-if="connectionStats" class="text-right text-xs">
          <div>{{ connectionStats.frameWidth }}x{{ connectionStats.frameHeight }}</div>
          <div>{{ connectionStats.frameRate?.toFixed(1) }} fps</div>
        </div>
      </div>
    </div>
    
    <!-- Recording Indicator -->
    <div
      v-if="isRecording"
      class="absolute top-4 left-4 flex items-center space-x-2 bg-red-600 text-white px-3 py-1 rounded-full text-sm font-medium"
    >
      <div class="w-2 h-2 bg-white rounded-full animate-pulse"></div>
      <span>REC</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { EllipsisVerticalIcon } from '@heroicons/vue/24/outline'
import WebRTCVideoPlayer from './WebRTCVideoPlayer.vue'
import { CameraStatus, type Camera } from '@/types/camera'
import type { WebRTCStreamOptions, WebRTCStats } from '@/types/webrtc'
import { mediaMTXService } from '@/services/mediaMTXService'

interface StreamOption {
  url: string
  label: string
  quality: 'high' | 'medium' | 'low'
  resolution?: string
}

interface Props {
  camera?: Camera
  streamUrls?: string | string[] | StreamOption[]
  aspectRatio?: string
  autoConnect?: boolean
  showHeader?: boolean
  showInfo?: boolean
  showActions?: boolean
  showVideoControls?: boolean
  showStats?: boolean
  retryAttempts?: number
  retryDelay?: number
  videoOptions?: Partial<WebRTCStreamOptions>
}

const props = withDefaults(defineProps<Props>(), {
  aspectRatio: '16/9',
  autoConnect: true,
  showHeader: true,
  showInfo: false,
  showActions: true,
  showVideoControls: true,
  showStats: false,
  retryAttempts: 3,
  retryDelay: 2000,
  videoOptions: () => ({
    autoplay: true,
    muted: true,
    playsInline: true
  })
})

const emit = defineEmits<{
  'connected': []
  'disconnected': []
  'error': [error: Error]
  'stats': [stats: WebRTCStats]
  'configure': []
  'snapshot': [blob: Blob]
  'recording-started': []
  'recording-stopped': [blob: Blob]
}>()

// Template refs
const videoPlayer = ref<InstanceType<typeof WebRTCVideoPlayer> | null>(null)

// Local state
const selectedStreamUrl = ref('')
const connectionStats = ref<WebRTCStats | null>(null)
const isRecording = ref(false)
const showActionsMenu = ref(false)
const actionsMenuRef = ref<HTMLElement | null>(null)

// Computed properties
const viewerClasses = computed(() => [
  'relative',
  'bg-black',
  'rounded-lg',
  'overflow-hidden',
  'shadow-lg'
])

const containerStyle = computed(() => {
  const baseStyle = {
    width: '100%',
    minHeight: '300px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center'
  }
  
  if (props.aspectRatio === 'unset') {
    return {
      ...baseStyle,
      height: '100%'
    }
  }
  
  if (props.aspectRatio === 'responsive') {
    return {
      ...baseStyle,
      height: 'calc(100vh - 8rem)',
      maxHeight: 'calc(100vh - 8rem)',
      flex: '1 1 auto'
    }
  }
  
  return {
    ...baseStyle,
    aspectRatio: props.aspectRatio,
    maxHeight: 'calc(100vh - 12rem)'
  }
})

const availableStreams = computed((): StreamOption[] => {
  if (!props.streamUrls) {
    // Use camera profiles if available
    if (props.camera?.profiles) {
      return props.camera.profiles.map((profile, index) => {
        let streamUrl = ''
        
        // Prefer WebRTC URI, fallback to RTSP
        if (profile.webRtcUri) {
          // Convert to proxied URL if it's a direct MediaMTX URL
          streamUrl = mediaMTXService.convertToProxiedUrl(profile.webRtcUri)
        } else if (profile.rtspUri) {
          // Convert RTSP to MediaMTX WebRTC URL using proxy
          try {
            const rtspUrl = new URL(profile.rtspUri)
            const path = rtspUrl.pathname.replace(/^\//, '')
            
            streamUrl = mediaMTXService.createStreamUrl(
              mediaMTXService.getWebRTCBaseUrl(),
              path
            )
          } catch (error) {
            console.warn('Failed to convert RTSP URL to WebRTC:', error)
            streamUrl = profile.rtspUri
          }
        }
        
        const resolution = `${profile.video.resolution.width}x${profile.video.resolution.height}`
        return {
          url: streamUrl,
          label: `${profile.name || `Profile ${index + 1}`} (${resolution})`,
          quality: index === 0 ? 'high' : index === 1 ? 'medium' : 'low' as 'high' | 'medium' | 'low',
          resolution
        }
      }).filter(stream => stream.url)
    }
    return []
  }

  if (typeof props.streamUrls === 'string') {
    return [{
      url: props.streamUrls,
      label: 'Default',
      quality: 'high'
    }]
  }

  if (Array.isArray(props.streamUrls)) {
    return props.streamUrls.map((url, index) => {
      if (typeof url === 'string') {
        return {
          url,
          label: `Stream ${index + 1}`,
          quality: index === 0 ? 'high' : index === 1 ? 'medium' : 'low'
        }
      }
      return url
    }) as StreamOption[]
  }

  return []
})

const statusColor = computed(() => {
  if (!props.camera) return 'bg-gray-500'
  
  switch (props.camera.status) {
    case CameraStatus.Online:
      return 'bg-green-500'
    case CameraStatus.Connecting:
      return 'bg-yellow-500 animate-pulse'
    case CameraStatus.Degraded:
      return 'bg-orange-500'
    case CameraStatus.Error:
    case CameraStatus.Offline:
      return 'bg-red-500'
    default:
      return 'bg-gray-500'
  }
})

// Set up click outside for actions menu (simplified implementation)
// Note: You can install @vueuse/core for a more robust onClickOutside implementation

// Initialize selected stream
function initializeStream() {
  if (availableStreams.value.length > 0) {
    // Select the highest quality stream by default
    const highQualityStream = availableStreams.value.find(s => s.quality === 'high') || availableStreams.value[0]!
    selectedStreamUrl.value = highQualityStream.url
  }
}

// Event handlers
function onConnected() {
  console.log('MediaMTX camera stream connected:', props.camera?.name || 'Unknown')
  emit('connected')
}

function onDisconnected() {
  console.log('MediaMTX camera stream disconnected:', props.camera?.name || 'Unknown')
  emit('disconnected')
}

function onError(error: Error) {
  console.error('MediaMTX camera stream error:', error, {
    camera: props.camera?.name,
    streamUrl: selectedStreamUrl.value
  })
  emit('error', error)
}

function onStats(stats: WebRTCStats) {
  connectionStats.value = stats
  emit('stats', stats)
}

// Actions
function toggleActionsMenu() {
  showActionsMenu.value = !showActionsMenu.value
}

async function takeSnapshot() {
  try {
    if (!videoPlayer.value) return
    
    // Get the video element from the WebRTC player
    const videoElement = videoPlayer.value.$refs?.videoElement as HTMLVideoElement
    if (!videoElement) return
    
    // Create canvas and draw current frame
    const canvas = document.createElement('canvas')
    canvas.width = videoElement.videoWidth
    canvas.height = videoElement.videoHeight
    
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    
    ctx.drawImage(videoElement, 0, 0)
    
    // Convert to blob
    canvas.toBlob((blob) => {
      if (blob) {
        emit('snapshot', blob)
        
        // Optional: Download the snapshot
        const url = URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = url
        a.download = `camera-${props.camera?.name || 'snapshot'}-${new Date().toISOString()}.png`
        a.click()
        URL.revokeObjectURL(url)
      }
    }, 'image/png')
    
    showActionsMenu.value = false
  } catch (error) {
    console.error('Error taking snapshot:', error)
  }
}

async function toggleRecording() {
  if (isRecording.value) {
    stopRecording()
  } else {
    startRecording()
  }
  showActionsMenu.value = false
}

let mediaRecorder: MediaRecorder | null = null
let recordedChunks: Blob[] = []

function startRecording() {
  try {
    if (!videoPlayer.value) return
    
    const videoElement = videoPlayer.value.$refs?.videoElement as HTMLVideoElement
    if (!videoElement || !videoElement.srcObject) return
    
    const stream = videoElement.srcObject as MediaStream
    mediaRecorder = new MediaRecorder(stream)
    recordedChunks = []
    
    mediaRecorder.ondataavailable = (event) => {
      if (event.data.size > 0) {
        recordedChunks.push(event.data)
      }
    }
    
    mediaRecorder.onstop = () => {
      const blob = new Blob(recordedChunks, { type: 'video/webm' })
      emit('recording-stopped', blob)
      
      // Optional: Download the recording
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `camera-${props.camera?.name || 'recording'}-${new Date().toISOString()}.webm`
      a.click()
      URL.revokeObjectURL(url)
      
      isRecording.value = false
    }
    
    mediaRecorder.start()
    isRecording.value = true
    emit('recording-started')
  } catch (error) {
    console.error('Error starting recording:', error)
  }
}

function stopRecording() {
  if (mediaRecorder && mediaRecorder.state === 'recording') {
    mediaRecorder.stop()
  }
}

async function reconnect() {
  if (videoPlayer.value) {
    await videoPlayer.value.retry()
  }
  showActionsMenu.value = false
}

// Utility functions
function formatLastSeen(lastSeen: string): string {
  if (!lastSeen) return 'Never connected'
  
  const date = new Date(lastSeen)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  
  if (diffMs < 60000) {
    return 'Connected now'
  } else if (diffMs < 3600000) {
    const minutes = Math.floor(diffMs / 60000)
    return `Connected ${minutes}m ago`
  } else if (diffMs < 86400000) {
    const hours = Math.floor(diffMs / 3600000)
    return `Connected ${hours}h ago`
  } else {
    return `Connected ${date.toLocaleDateString()}`
  }
}

// Watch for camera changes
watch(() => props.camera, () => {
  initializeStream()
}, { immediate: true })

// Watch for stream URLs changes
watch(() => props.streamUrls, () => {
  initializeStream()
}, { immediate: true })

// Lifecycle
onMounted(() => {
  initializeStream()
})

// Expose methods for parent components
defineExpose({
  reconnect,
  takeSnapshot,
  startRecording,
  stopRecording,
  toggleRecording
})
</script>

<style scoped>
.camera-stream-viewer {
  transition: all 0.2s ease;
}

.camera-header {
  user-select: none;
}

.video-container {
  position: relative;
  background-color: #000;
  overflow: hidden;
  min-height: 300px;
}

.video-container:empty {
  height: 100%;
  min-height: 400px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.video-container:empty::before {
  content: 'Loading video...';
  color: #9ca3af;
  font-size: 1rem;
}

.video-container :deep(video) {
  width: 100%;
  height: 100%;
  object-fit: contain;
  display: block;
  min-height: 300px;
}

.actions-menu {
  min-width: 160px;
}
</style>