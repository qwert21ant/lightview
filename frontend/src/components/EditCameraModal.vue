<template>
  <div v-if="isOpen" class="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
    <div class="bg-white rounded-lg shadow-xl w-full max-w-md">
      <!-- Modal Header -->
      <div class="flex items-center justify-between p-6 border-b">
        <h2 class="text-lg font-semibold text-gray-900">Edit Camera</h2>
        <button
          @click="closeModal"
          class="text-gray-400 hover:text-gray-600 transition-colors"
        >
          <XMarkIcon class="h-6 w-6" />
        </button>
      </div>
      
      <!-- Modal Body -->
      <form @submit.prevent="handleSubmit" class="p-6 space-y-4">
        <!-- Camera Name -->
        <div>
          <label for="name" class="block text-sm font-medium text-gray-700 mb-1">
            Camera Name *
          </label>
          <input
            id="name"
            v-model="form.name"
            type="text"
            required
            placeholder="e.g., Front Door Camera"
            class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            :class="{ 'border-red-300': errors.name }"
          />
          <p v-if="errors.name" class="text-red-600 text-xs mt-1">{{ errors.name }}</p>
        </div>
        
        <!-- Camera Protocol -->
        <div>
          <label for="protocol" class="block text-sm font-medium text-gray-700 mb-1">
            Camera Protocol *
          </label>
          <select
            id="protocol"
            v-model="form.protocol"
            required
            class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            :class="{ 'border-red-300': errors.protocol }"
          >
            <option value="">Select camera protocol</option>
            <option :value="CameraProtocol.Onvif">ONVIF</option>
            <option :value="CameraProtocol.Rtsp">RTSP</option>
          </select>
          <p v-if="errors.protocol" class="text-red-600 text-xs mt-1">{{ errors.protocol }}</p>
        </div>
        
        <!-- Camera URL/IP -->
        <div>
          <label for="url" class="block text-sm font-medium text-gray-700 mb-1">
            Camera URL *
          </label>
          <input
            id="url"
            v-model="form.url"
            type="text"
            required
            placeholder="rtsp://192.168.1.100:554/stream1"
            class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            :class="{ 'border-red-300': errors.url }"
          />
          <p v-if="errors.url" class="text-red-600 text-xs mt-1">{{ errors.url }}</p>
          <p class="text-gray-500 text-xs mt-1">
            Enter the full URL or IP address for the camera stream
          </p>
        </div>
        
        <!-- Username (Optional) -->
        <div>
          <label for="username" class="block text-sm font-medium text-gray-700 mb-1">
            Username
          </label>
          <input
            id="username"
            v-model="form.username"
            type="text"
            placeholder="Optional authentication username"
            class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
          />
        </div>
        
        <!-- Password (Optional) -->
        <div>
          <label for="password" class="block text-sm font-medium text-gray-700 mb-1">
            Password
          </label>
          <input
            id="password"
            v-model="form.password"
            type="password"
            placeholder="Optional authentication password"
            class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
          />
          <p class="text-gray-500 text-xs mt-1">
            Leave blank to keep existing password
          </p>
        </div>
        

        
        <!-- Camera Status -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Current Status
          </label>
          <div class="flex items-center space-x-2">
            <span 
              class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
              :class="[
                camera.status === CameraStatus.Online 
                  ? 'bg-green-100 text-green-800' 
                  : 'bg-red-100 text-red-800'
              ]"
            >
              <div 
                class="w-2 h-2 rounded-full mr-1.5"
                :class="[
                  camera.status === CameraStatus.Online ? 'bg-green-400' : 'bg-red-400'
                ]"
              ></div>
              {{ getStatusText(camera.status) }}
            </span>
            <span class="text-xs text-gray-500">
              Last connected: {{ formatLastSeen(camera.lastConnectedAt) }}
            </span>
          </div>
          
          <!-- Device Info if available -->
          <div v-if="camera.deviceInfo" class="mt-2 text-xs text-gray-600">
            <div v-if="camera.deviceInfo.manufacturer">{{ camera.deviceInfo.manufacturer }} {{ camera.deviceInfo.model }}</div>
            <div v-if="camera.deviceInfo.firmwareVersion">Firmware: {{ camera.deviceInfo.firmwareVersion }}</div>
          </div>
          
          <!-- Capabilities if available -->
          <div v-if="camera.capabilities" class="mt-2 flex flex-wrap gap-1">
            <span v-if="camera.capabilities.supportsPtz" class="inline-flex items-center px-2 py-1 rounded-full text-xs bg-blue-100 text-blue-800">
              PTZ
            </span>
            <span v-if="camera.capabilities.supportsAudio" class="inline-flex items-center px-2 py-1 rounded-full text-xs bg-purple-100 text-purple-800">
              Audio
            </span>
            <span v-if="camera.capabilities.supportsMotionDetection" class="inline-flex items-center px-2 py-1 rounded-full text-xs bg-green-100 text-green-800">
              Motion
            </span>
          </div>
        </div>
        
        <!-- Error Message -->
        <div v-if="submitError" class="p-3 bg-red-50 border border-red-200 rounded-md">
          <p class="text-red-600 text-sm">{{ submitError }}</p>
        </div>
      </form>
      
      <!-- Modal Footer -->
      <div class="flex items-center justify-between p-6 border-t bg-gray-50">
        <!-- Test Connection Button -->
        <button
          @click="testConnection"
          :disabled="isSubmitting || isTestingConnection"
          type="button"
          class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          <span v-if="isTestingConnection" class="flex items-center">
            <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-600 mr-2"></div>
            Testing...
          </span>
          <span v-else>Test Connection</span>
        </button>
        
        <div class="flex items-center space-x-3">
          <button
            @click="closeModal"
            type="button"
            class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
          >
            Cancel
          </button>
          <button
            @click="handleSubmit"
            :disabled="isSubmitting"
            type="submit"
            class="px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-md hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <span v-if="isSubmitting" class="flex items-center">
              <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
              Saving...
            </span>
            <span v-else>Save Changes</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { XMarkIcon } from '@heroicons/vue/24/outline'
import type { Camera } from '@/types/camera'
import { CameraProtocol, CameraStatus } from '@/types/camera'

interface Props {
  isOpen: boolean
  camera: Camera
}

interface CameraForm {
  name: string
  url: string
  username: string
  password: string
  protocol: CameraProtocol | ''
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'close': []
  'submit': [camera: Camera]
  'test-connection': [camera: Camera]
}>()

// Form data
const form = reactive<CameraForm>({
  name: '',
  url: '',
  username: '',
  password: '',
  protocol: '' as CameraProtocol | ''
})

// Form validation
const errors = reactive({
  name: '',
  url: '',
  protocol: ''
})

const isSubmitting = ref(false)
const isTestingConnection = ref(false)
const submitError = ref('')

// Populate form when camera prop changes
watch(() => props.camera, (camera) => {
  if (camera && props.isOpen) {
    populateForm(camera)
  }
}, { immediate: true })

// Reset form when modal opens/closes
watch(() => props.isOpen, (isOpen) => {
  if (isOpen && props.camera) {
    populateForm(props.camera)
  }
})

const populateForm = (camera: Camera) => {
  Object.assign(form, {
    name: camera.name,
    url: camera.url,
    username: camera.username,
    password: '', // Always empty for security
    protocol: camera.protocol
  })
  
  // Reset validation state
  Object.assign(errors, {
    name: '',
    type: '',
    url: ''
  })
  
  submitError.value = ''
  isSubmitting.value = false
  isTestingConnection.value = false
}

const validateForm = (): boolean => {
  let isValid = true
  
  // Reset errors
  Object.assign(errors, {
    name: '',
    url: '',
    protocol: ''
  })
  
  // Validate name
  if (!form.name.trim()) {
    errors.name = 'Camera name is required'
    isValid = false
  }
  
  // Validate protocol
  if (!form.protocol) {
    errors.protocol = 'Camera protocol is required'
    isValid = false
  }
  
  // Validate URL
  if (!form.url.trim()) {
    errors.url = 'Camera URL is required'
    isValid = false
  } else {
    // Basic URL validation
    try {
      new URL(form.url)
    } catch {
      // Check if it's an IP address
      const ipRegex = /^(\d{1,3}\.){3}\d{1,3}(:\d+)?$/
      if (!ipRegex.test(form.url)) {
        errors.url = 'Please enter a valid URL or IP address'
        isValid = false
      }
    }
  }
  
  return isValid
}

const handleSubmit = async () => {
  if (!validateForm()) {
    return
  }
  
  isSubmitting.value = true
  submitError.value = ''
  
  try {
    const updatedCamera: Camera = {
      ...props.camera,
      name: form.name.trim(),
      url: form.url.trim(),
      username: form.username.trim(),
      password: form.password.trim() || props.camera.password, // Keep existing if not changed
      protocol: form.protocol as CameraProtocol
    }
    
    emit('submit', updatedCamera)
  } catch (error) {
    submitError.value = error instanceof Error ? error.message : 'Failed to update camera'
  } finally {
    isSubmitting.value = false
  }
}

const testConnection = async () => {
  if (!validateForm()) {
    return
  }
  
  isTestingConnection.value = true
  
  try {
    const testCamera: Camera = {
      ...props.camera,
      name: form.name.trim(),
      url: form.url.trim(),
      username: form.username.trim(),
      password: form.password.trim() || props.camera.password,
      protocol: form.protocol as CameraProtocol
    }
    
    emit('test-connection', testCamera)
    
    // Simulate connection test delay
    await new Promise(resolve => setTimeout(resolve, 2000))
  } catch (error) {
    console.error('Connection test failed:', error)
  } finally {
    isTestingConnection.value = false
  }
}

const closeModal = () => {
  if (!isSubmitting.value && !isTestingConnection.value) {
    emit('close')
  }
}

const getStatusText = (status: CameraStatus): string => {
  switch (status) {
    case CameraStatus.Online:
      return 'Online'
    case CameraStatus.Offline:
      return 'Offline'
    case CameraStatus.Connecting:
      return 'Connecting'
    case CameraStatus.Error:
      return 'Error'
    case CameraStatus.Maintenance:
      return 'Maintenance'
    default:
      return 'Unknown'
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