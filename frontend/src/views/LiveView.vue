<template>
  <div class="live-view" :class="{ 'fullscreen-mode': isFullscreen }">
    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center h-[calc(100vh-8rem)] bg-gray-900 rounded-lg">
      <div class="text-center">
        <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto mb-4"></div>
        <p class="text-gray-400">Loading camera...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="flex items-center justify-center h-[calc(100vh-8rem)] bg-gray-900 rounded-lg">
      <div class="text-center">
        <div class="text-red-500 text-6xl mb-4">⚠️</div>
        <h2 class="text-xl font-semibold text-white mb-2">Camera Not Found</h2>
        <p class="text-gray-400 mb-4">{{ error }}</p>
        <button
          @click="$router.push('/cameras')"
          class="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-md transition-colors"
        >
          Back to Cameras
        </button>
      </div>
    </div>

    <!-- Camera Stream -->
    <div v-else-if="camera" class="bg-black rounded-lg overflow-hidden" :class="streamContainerClasses">
      <!-- Stream Header -->
      <div class="absolute top-0 left-0 right-0 z-10 bg-gradient-to-b from-black/70 to-transparent">
        <div class="flex items-center justify-between p-4">
          <!-- Camera Info -->
          <div class="flex items-center space-x-4">
            <button
              @click="handleBackNavigation"
              class="text-white hover:text-gray-300 transition-colors"
              title="Go Back"
            >
              <ArrowLeftIcon class="w-6 h-6" />
            </button>
            
            <div class="flex items-center space-x-3">
              <div 
                class="w-3 h-3 rounded-full"
                :class="statusColor"
              ></div>
              <div>
                <h1 class="text-xl font-semibold text-white">{{ camera.name }}</h1>
                <p class="text-sm text-gray-300">
                  {{ camera.deviceInfo?.manufacturer }} {{ camera.deviceInfo?.model }}
                </p>
              </div>
            </div>
          </div>

          <!-- Actions -->
          <div class="flex items-center space-x-2">
            <!-- Stream Quality Selector -->
            <select
              v-if="availableStreams.length > 1"
              v-model="selectedStreamProfile"
              class="bg-black/50 text-white text-sm px-3 py-2 rounded border border-gray-600 focus:border-blue-500 backdrop-blur-sm"
            >
              <option
                v-for="profile in availableStreams"
                :key="profile.token"
                :value="profile"
              >
                {{ getStreamLabel(profile) }}
              </option>
            </select>

            <!-- Fullscreen Toggle -->
            <button
              @click="toggleFullscreen"
              class="text-white hover:text-gray-300 p-2 rounded transition-colors"
              title="Toggle Fullscreen"
            >
              <component :is="isFullscreen ? ArrowsPointingInIcon : ArrowsPointingOutIcon" class="w-5 h-5" />
            </button>

            <!-- Settings -->
            <button
              @click="showSettings = !showSettings"
              class="text-white hover:text-gray-300 p-2 rounded transition-colors"
              title="Settings"
            >
              <CogIcon class="w-5 h-5" />
            </button>
          </div>
        </div>
      </div>

      <!-- Camera Stream Viewer -->
      <CameraStreamViewer
        :camera="camera"
        :selected-stream="selectedStreamProfile"
        :show-header="false"
        :show-actions="false"
        :show-video-controls="true"
        :show-stats="showStats"
        :auto-connect="true"
        :aspect-ratio="isFullscreen ? 'unset' : 'responsive'"
        class="h-full"
        @connected="onStreamConnected"
        @disconnected="onStreamDisconnected"
        @error="onStreamError"
      />

      <!-- Settings Panel -->
      <div
        v-if="showSettings"
        class="absolute top-0 right-0 w-80 bg-black/90 backdrop-blur-sm border-l border-gray-700 z-20 overflow-y-auto"
        :class="isFullscreen ? 'h-full' : 'max-h-[calc(100vh-8rem)]'"
      >
        <div class="p-4">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-semibold text-white">Settings</h3>
            <button
              @click="showSettings = false"
              class="text-gray-400 hover:text-white"
            >
              <XMarkIcon class="w-5 h-5" />
            </button>
          </div>

          <!-- Stream Settings -->
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-300 mb-2">
                Show Statistics
              </label>
              <label class="flex items-center">
                <input
                  v-model="showStats"
                  type="checkbox"
                  class="sr-only"
                >
                <div class="relative">
                  <div class="block bg-gray-600 w-14 h-8 rounded-full"></div>
                  <div
                    class="dot absolute left-1 top-1 bg-white w-6 h-6 rounded-full transition"
                    :class="{ 'translate-x-6': showStats }"
                  ></div>
                </div>
                <span class="ml-3 text-sm text-gray-300">
                  Display stream statistics
                </span>
              </label>
            </div>

            <!-- Camera Information -->
            <div>
              <h4 class="text-sm font-medium text-gray-300 mb-2">Camera Information</h4>
              <div class="space-y-2 text-sm text-gray-400">
                <div class="flex justify-between">
                  <span>Status:</span>
                  <span class="text-white">{{ getCameraStatusText(camera.status) }}</span>
                </div>
                <div class="flex justify-between">
                  <span>Protocol:</span>
                  <span class="text-white">{{ getCameraProtocolText(camera.protocol) }}</span>
                </div>
                <div class="flex justify-between">
                  <span>Profiles:</span>
                  <span class="text-white">{{ camera.profiles.length }}</span>
                </div>
                <div v-if="camera.deviceInfo?.serialNumber" class="flex justify-between">
                  <span>Serial:</span>
                  <span class="text-white font-mono">{{ camera.deviceInfo.serialNumber }}</span>
                </div>
                <div v-if="camera.deviceInfo?.firmwareVersion" class="flex justify-between">
                  <span>Firmware:</span>
                  <span class="text-white">{{ camera.deviceInfo.firmwareVersion }}</span>
                </div>
              </div>
            </div>

            <!-- Stream Information -->
            <div v-if="selectedStreamProfile">
              <h4 class="text-sm font-medium text-gray-300 mb-2">Stream Information</h4>
              <div class="space-y-2 text-sm text-gray-400">
                <div class="flex justify-between">
                  <span>Codec:</span>
                  <span class="text-white">{{ selectedStreamProfile.video.codec }}</span>
                </div>
                <div class="flex justify-between">
                  <span>Resolution:</span>
                  <span class="text-white">
                    {{ selectedStreamProfile.video.resolution.width }}x{{ selectedStreamProfile.video.resolution.height }}
                  </span>
                </div>
                <div class="flex justify-between">
                  <span>Framerate:</span>
                  <span class="text-white">{{ selectedStreamProfile.video.framerate }} fps</span>
                </div>
                <div class="flex justify-between">
                  <span>Bitrate:</span>
                  <span class="text-white">{{ Math.round(selectedStreamProfile.video.bitrate / 1000) }} kbps</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { 
  ArrowLeftIcon, 
  ArrowsPointingOutIcon,
  ArrowsPointingInIcon,
  CogIcon,
  XMarkIcon
} from '@heroicons/vue/24/outline'

import CameraStreamViewer from '@/components/CameraStreamViewer.vue'
import type { Camera, CameraStatus, CameraProtocol } from '@/types/camera'
import type { CameraProfile } from '@/types/camera/stream'
import { useCameraManager } from '@/composables/useCamera'

const route = useRoute()
const router = useRouter()

const cameraManager = useCameraManager()

// Component state
const loading = ref(true)
const error = ref<string | null>(null)
const selectedStreamProfile = ref<CameraProfile | null>(null)
const showSettings = ref(false)
const showStats = ref(false)
const isFullscreen = ref(false)

// Reactive camera that updates automatically when camera manager data changes
const camera = computed(() => {
  const cameraId = route.params.id as string
  return cameraId ? cameraManager.getCameraById(cameraId) : null
})

// Computed properties
const availableStreams = computed(() => {
  if (!camera.value?.profiles) return []
  return camera.value.profiles.filter((profile: CameraProfile) => profile.webRtcUri)
})

const statusColor = computed(() => {
  if (!camera.value) return 'bg-gray-500'
  
  switch (camera.value.status) {
    case 2: // Online
      return 'bg-green-500'
    case 1: // Connecting
      return 'bg-yellow-500'
    case 3: // Degraded
      return 'bg-orange-500'
    case 4: // Error
      return 'bg-red-500'
    default: // Offline
      return 'bg-gray-500'
  }
})

const streamContainerClasses = computed(() => {
  if (isFullscreen.value) {
    return 'fullscreen-stream'
  } else {
    return 'relative h-[calc(100vh-8rem)] max-h-[calc(100vh-8rem)] flex flex-col'
  }
})

// Methods
const loadCamera = () => {
  try {
    loading.value = true
    error.value = null
    
    const cameraId = route.params.id as string
    if (!cameraId) {
      error.value = 'Camera ID is required'
      return
    }

    // Wait for camera manager to be connected and initialized
    if (!cameraManager.isConnected.value || !cameraManager.isInitialized.value) {
      error.value = 'Camera manager not ready. Please wait for initialization to complete.'
      return
    }

    // Camera is now computed, so just check if it exists
    if (!camera.value) {
      error.value = `Camera with ID \"${cameraId}\" not found`
      return
    }

    // Select the main stream by default, or first available stream
    const mainStream = camera.value.profiles.find((p: CameraProfile) => p.isMainStream && p.webRtcUri)
    const firstStream = camera.value.profiles.find((p: CameraProfile) => p.webRtcUri)
    selectedStreamProfile.value = mainStream || firstStream || null

    error.value = null // Clear any previous errors

  } catch (err) {
    console.error('Failed to load camera:', err)
    error.value = 'Failed to load camera information'
  } finally {
    loading.value = false
  }
}

const getStreamLabel = (profile: CameraProfile): string => {
  const resolution = `${profile.video.resolution.width}x${profile.video.resolution.height}`
  const framerate = `${profile.video.framerate}fps`
  return `${profile.name} (${resolution}, ${framerate})`
}

const getCameraStatusText = (status: CameraStatus): string => {
  switch (status) {
    case 0: return 'Offline'
    case 1: return 'Connecting'
    case 2: return 'Online'
    case 3: return 'Degraded'
    case 4: return 'Error'
    default: return 'Unknown'
  }
}

const getCameraProtocolText = (protocol: CameraProtocol): string => {
  switch (protocol) {
    case 0: return 'ONVIF'
    case 1: return 'RTSP'
    default: return 'Unknown'
  }
}

const handleBackNavigation = () => {
  if (isFullscreen.value) {
    // Exit fullscreen instead of navigating back
    document.exitFullscreen()
  } else {
    router.back()
  }
}

const toggleFullscreen = () => {
  if (!isFullscreen.value) {
    document.documentElement.requestFullscreen()
  } else {
    document.exitFullscreen()
  }
}

const handleFullscreenChange = () => {
  isFullscreen.value = !!document.fullscreenElement
}

const onStreamConnected = () => {
  console.log('Stream connected')
}

const onStreamDisconnected = () => {
  console.log('Stream disconnected')
}

const onStreamError = (error: Error) => {
  console.error('Stream error:', error.message)
}

// Keyboard shortcuts
const handleKeyDown = (event: KeyboardEvent) => {
  switch (event.key) {
    case 'Escape':
      if (showSettings.value) {
        showSettings.value = false
      } else if (isFullscreen.value) {
        document.exitFullscreen()
      } else {
        handleBackNavigation()
      }
      break
    case 'f':
    case 'F':
      if (!showSettings.value) {
        toggleFullscreen()
      }
      break
    case 's':
    case 'S':
      if (!showSettings.value) {
        showStats.value = !showStats.value
      }
      break
  }
}

// Lifecycle
onMounted(() => {
  loadCamera()
  document.addEventListener('fullscreenchange', handleFullscreenChange)
  document.addEventListener('keydown', handleKeyDown)
})

onUnmounted(() => {
  document.removeEventListener('fullscreenchange', handleFullscreenChange)
  document.removeEventListener('keydown', handleKeyDown)
})

// Watch for route changes
watch(() => route.params.id, () => {
  if (route.params.id) {
    loadCamera()
  }
})

// Watch for camera manager initialization
watch(cameraManager.isInitialized, (initialized) => {
  if (initialized && route.params.id) {
    loadCamera()
  }
})

// Watch for camera changes (reactive updates)
watch(camera, (newCamera) => {
  if (newCamera && newCamera.profiles) {
    // Update stream profile when camera data changes
    const mainStream = newCamera.profiles.find((p: CameraProfile) => p.isMainStream && p.webRtcUri)
    const firstStream = newCamera.profiles.find((p: CameraProfile) => p.webRtcUri)
    selectedStreamProfile.value = mainStream || firstStream || null
  }
}, { immediate: true })
</script>

<style scoped>
.live-view {
  @apply w-full overflow-hidden flex flex-col;
  min-height: calc(100vh - 8rem);
  max-height: calc(100vh - 8rem);
}

.live-view.fullscreen-mode {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 50;
  background-color: black;
}

.fullscreen-stream {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 50;
  height: 100vh;
}

/* Smooth transitions for settings panel */
.settings-panel-enter-active,
.settings-panel-leave-active {
  transition: transform 0.3s ease-in-out;
}

.settings-panel-enter-from,
.settings-panel-leave-to {
  transform: translateX(100%);
}

/* Toggle switch styling */
.dot {
  transition: transform 0.2s ease-in-out;
}

input:checked + div .dot {
  background-color: #3b82f6;
}
</style>