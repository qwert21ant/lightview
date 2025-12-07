<template>
  <div class="bg-white rounded-lg border shadow-sm hover:shadow-md transition-shadow">
    <!-- Camera Preview -->
    <div class="relative bg-gray-900 rounded-t-lg overflow-hidden">
      <div class="aspect-video flex items-center justify-center">
        <!-- Snapshot preview or placeholder -->
        <div class="absolute inset-0">
          <img 
            v-if="latestSnapshot" 
            :src="latestSnapshot.imageData" 
            :alt="`Latest snapshot from ${camera.name}`"
            class="w-full h-full object-cover"
            @error="onSnapshotError"
          />
          <div v-else class="bg-gradient-to-br from-gray-800 to-gray-900 w-full h-full">
            <div class="absolute inset-0 bg-black/20 flex items-center justify-center">
              <div class="text-white/60 text-center">
                <VideoCameraSlashIcon class="h-12 w-12 mx-auto mb-2 opacity-50" />
                <p class="text-sm">No snapshot available</p>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Snapshot refresh indicator -->
        <div v-if="isLoadingSnapshot" class="absolute inset-0 bg-black/30 flex items-center justify-center">
          <div class="animate-spin rounded-full h-8 w-8 border-2 border-white border-t-transparent"></div>
        </div>
        
        <!-- Video controls overlay -->
        <div class="absolute inset-0 bg-black/40 opacity-0 hover:opacity-100 transition-opacity duration-200 flex items-center justify-center">
          <div class="flex space-x-3">
            <button 
              @click="$emit('view-stream', camera)"
              class="p-3 bg-white/20 backdrop-blur-sm rounded-full text-white hover:bg-white/30 transition-colors"
              :title="`View ${camera.name} stream`"
            >
              <PlayIcon class="h-6 w-6" />
            </button>
            <button 
              @click="$emit('edit', camera)"
              class="p-3 bg-white/20 backdrop-blur-sm rounded-full text-white hover:bg-white/30 transition-colors"
              title="Edit camera settings"
            >
              <Cog6ToothIcon class="h-6 w-6" />
            </button>
          </div>
        </div>
        
        <!-- Status indicator -->
        <div class="absolute top-3 left-3">
          <span 
            class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium backdrop-blur-sm"
            :class="[
              isOnline
                ? 'bg-green-500/80 text-white'
                : camera.status === CameraStatus.Degraded
                ? 'bg-yellow-500/80 text-white'
                : camera.status === CameraStatus.Connecting
                ? 'bg-blue-500/80 text-white'
                : 'bg-red-500/80 text-white'
            ]"
          >
            <div 
              class="w-2 h-2 rounded-full mr-1.5"
              :class="[
                isOnline ? 'bg-green-200' 
                : camera.status === CameraStatus.Degraded ? 'bg-yellow-200' 
                : camera.status === CameraStatus.Connecting ? 'bg-blue-200'
                : 'bg-red-200'
              ]"
            ></div>
            {{ statusText }}
          </span>
        </div>
        
        <!-- Camera type badge -->
        <div class="absolute top-3 right-3">
          <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-black/60 text-white backdrop-blur-sm">
            {{ protocolText }}
          </span>
        </div>
      </div>
    </div>
    
    <!-- Camera Info -->
    <div class="p-4">
      <div class="flex items-start justify-between mb-3">
        <div class="flex items-center space-x-3 min-w-0 flex-1">
          <div class="p-2 bg-indigo-100 rounded-lg flex-shrink-0">
            <VideoCameraIcon class="h-5 w-5 text-indigo-600" />
          </div>
          <div class="min-w-0 flex-1">
            <h3 class="font-semibold text-gray-900 truncate">{{ camera.name }}</h3>
            <p class="text-sm text-gray-500 truncate" :title="camera.url">
              {{ formatUrl(camera.url) }}
            </p>
          </div>
        </div>
      </div>
      
      <!-- Camera Details -->
      <div class="space-y-2 text-sm text-gray-600">
        <div class="flex justify-between items-center">
          <span>Protocol:</span>
          <span class="font-medium">{{ protocolText }}</span>
        </div>
        <div class="flex justify-between items-center">
          <span>Resolution:</span>
          <span class="font-medium">{{ resolutionText }}</span>
        </div>
        <div class="flex justify-between items-center">
          <span>Last connected:</span>
          <span class="font-medium">{{ formatLastSeen(camera.lastConnectedAt) }}</span>
        </div>
        <div v-if="camera.capabilities?.supportsPtz" class="flex justify-between items-center">
          <span>PTZ:</span>
          <span class="font-medium text-green-600">Supported</span>
        </div>
      </div>
      
      <!-- Actions -->
      <div class="mt-4 space-y-2">
        <!-- Primary actions row -->
        <div class="flex space-x-2">
          <button 
            @click="$emit('view-stream', camera)"
            class="flex-1 px-3 py-2 text-sm bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="!isOnline"
          >
            View Stream
          </button>
          <button 
            @click="handleConnectionToggle"
            class="px-3 py-2 text-sm rounded-md transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            :class="[
              isOnline 
                ? 'bg-orange-600 text-white hover:bg-orange-700' 
                : 'bg-green-600 text-white hover:bg-green-700'
            ]"
            :disabled="camera.status === CameraStatus.Connecting"
          >
            <span v-if="camera.status === CameraStatus.Connecting" class="flex items-center">
              <div class="animate-spin rounded-full h-3 w-3 border-b-2 border-white mr-1"></div>
              Connecting...
            </span>
            <span v-else>
              {{ isOnline ? 'Disconnect' : 'Connect' }}
            </span>
          </button>
        </div>
        
        <!-- Secondary actions row -->
        <div class="flex space-x-2">
          <button 
            @click="$emit('edit', camera)"
            class="flex-1 px-3 py-2 text-sm border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 transition-colors"
          >
            Edit
          </button>
          <button 
            @click="$emit('delete', camera)"
            class="flex-1 px-3 py-2 text-sm border border-red-300 text-red-700 rounded-md hover:bg-red-50 transition-colors"
          >
            Delete
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { VideoCameraIcon, PlayIcon, Cog6ToothIcon, VideoCameraSlashIcon } from '@heroicons/vue/24/outline'
import type { Camera } from '@/types/camera'
import { CameraStatus, CameraProtocol } from '@/types/camera'
import { useCameraManager } from '@/composables/useCamera'

interface Props {
  camera: Camera
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'view-stream': [camera: Camera]
  'edit': [camera: Camera]
  'delete': [camera: Camera]
  'connect': [camera: Camera]
  'disconnect': [camera: Camera]
}>()

const cameraManager = useCameraManager()

// Subscribe to reactive snapshot state from camera manager
const latestSnapshot = cameraManager.getSnapshotForCamera(props.camera.id)
const isLoadingSnapshot = cameraManager.isLoadingSnapshotForCamera(props.camera.id)

// Handle snapshot display errors
const onSnapshotError = () => {
  console.warn(`Failed to display snapshot for camera ${props.camera.id}`)
}

// Computed properties for handling the new backend models
const isOnline = computed(() => 
  props.camera.status === CameraStatus.Online || props.camera.status === CameraStatus.Degraded
)

const statusText = computed(() => {
  switch (props.camera.status) {
    case CameraStatus.Offline:
      return 'Offline'
    case CameraStatus.Connecting:
      return 'Connecting'
    case CameraStatus.Online:
      return 'Online'
    case CameraStatus.Degraded:
      return 'Degraded'
    case CameraStatus.Error:
      return 'Error'
    default:
      return 'Unknown'
  }
})

const protocolText = computed(() => {
  switch (props.camera.protocol) {
    case CameraProtocol.Onvif:
      return 'ONVIF'
    case CameraProtocol.Rtsp:
      return 'RTSP'
    default:
      return 'RTSP'
  }
})

const resolutionText = computed(() => {
  // Try to get resolution from main profile first
  const mainProfile = props.camera.profiles?.find(p => p.isMainStream)
  if (mainProfile?.video?.resolution) {
    const { width, height } = mainProfile.video.resolution
    return `${width}x${height}`
  }
  
  // Fallback to first profile
  const firstProfile = props.camera.profiles?.[0]
  if (firstProfile?.video?.resolution) {
    const { width, height } = firstProfile.video.resolution
    return `${width}x${height}`
  }
  
  return 'Unknown'
})

const formatUrl = (url: string): string => {
  try {
    const urlObj = new URL(url)
    return urlObj.hostname + (urlObj.port ? `:${urlObj.port}` : '')
  } catch {
    return url
  }
}

const handleConnectionToggle = () => {
  if (isOnline.value) {
    emit('disconnect', props.camera)
  } else {
    emit('connect', props.camera)
  }
}

const formatLastSeen = (lastSeen: string): string => {
  if (!lastSeen) return 'Never'
  
  const date = new Date(lastSeen)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  
  if (diffMs < 60000) { // Less than 1 minute
    return 'Just now'
  } else if (diffMs < 3600000) { // Less than 1 hour
    const minutes = Math.floor(diffMs / 60000)
    return `${minutes}m ago`
  } else if (diffMs < 86400000) { // Less than 24 hours
    const hours = Math.floor(diffMs / 3600000)
    return `${hours}h ago`
  } else {
    return date.toLocaleDateString()
  }
}
</script>