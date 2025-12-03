<template>
  <div v-if="isOpen" class="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
    <div class="bg-white rounded-lg shadow-xl w-full max-w-md">
      <!-- Modal Header -->
      <div class="flex items-center justify-between p-6 border-b">
        <h2 class="text-lg font-semibold text-gray-900">Add New Camera</h2>
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
            placeholder="rtsp://192.168.1.100:554/stream or http://192.168.1.100/stream"
            class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            :class="{ 'border-red-300': errors.url }"
          />
          <p v-if="errors.url" class="text-red-600 text-xs mt-1">{{ errors.url }}</p>
          <p class="text-gray-500 text-xs mt-1">
            Enter camera stream URL (rtsp://, http://, or https://)
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
        </div>
        
        <!-- Note -->
        <div class="p-3 bg-blue-50 border border-blue-200 rounded-md">
          <p class="text-blue-700 text-sm">
            <strong>Note:</strong> Camera capabilities such as PTZ support and available resolutions will be automatically detected after adding the camera.
          </p>
        </div>
        
        <!-- Error Message -->
        <div v-if="submitError" class="p-3 bg-red-50 border border-red-200 rounded-md">
          <p class="text-red-600 text-sm">{{ submitError }}</p>
        </div>
      </form>
      
      <!-- Modal Footer -->
      <div class="flex items-center justify-end space-x-3 p-6 border-t bg-gray-50">
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
            Adding...
          </span>
          <span v-else>Add Camera</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { XMarkIcon } from '@heroicons/vue/24/outline'
import type { AddCameraRequest } from '@/types/camera'
import { CameraProtocol } from '@/types/camera'
import { validateCameraUrl, validateCameraName, validateCameraProtocol } from '@/utils/validation'

interface Props {
  isOpen: boolean
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
  'submit': [camera: AddCameraRequest]
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
const submitError = ref('')

// Reset form when modal opens/closes
watch(() => props.isOpen, (isOpen) => {
  if (isOpen) {
    resetForm()
  }
})

const resetForm = () => {
  Object.assign(form, {
    name: '',
    url: '',
    username: '',
    password: '',
    protocol: '' as CameraProtocol | ''
  })
  
  Object.assign(errors, {
    name: '',
    url: '',
    protocol: ''
  })
  
  submitError.value = ''
  isSubmitting.value = false
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
  const nameValidation = validateCameraName(form.name)
  if (!nameValidation.isValid) {
    errors.name = nameValidation.error || ''
    isValid = false
  }
  
  // Validate protocol
  const protocolValidation = validateCameraProtocol(form.protocol)
  if (!protocolValidation.isValid) {
    errors.protocol = protocolValidation.error || ''
    isValid = false
  }
  
  // Validate URL
  const urlValidation = validateCameraUrl(form.url, form.protocol)
  if (!urlValidation.isValid) {
    errors.url = urlValidation.error || ''
    isValid = false
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
    const cameraData: AddCameraRequest = {
      name: form.name.trim(),
      url: form.url.trim(),
      username: form.username.trim(),
      password: form.password.trim(),
      protocol: form.protocol as CameraProtocol
    }
    
    emit('submit', cameraData)
  } catch (error) {
    submitError.value = error instanceof Error ? error.message : 'Failed to add camera'
  } finally {
    isSubmitting.value = false
  }
}

const closeModal = () => {
  if (!isSubmitting.value) {
    emit('close')
  }
}
</script>