<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-3xl font-bold text-gray-900">Cameras</h1>
        <p class="text-gray-600 mt-1">Manage and monitor your IP cameras</p>
      </div>
      <button 
        @click="showAddModal = true"
        class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 transition-colors"
      >
        <PlusIcon class="h-4 w-4 mr-2" />
        Add Camera
      </button>
    </div>

    <!-- Connection Status Alert -->
    <div v-if="!isConnected" class="bg-yellow-50 border border-yellow-200 rounded-md p-4">
      <div class="flex">
        <ExclamationTriangleIcon class="h-5 w-5 text-yellow-400" />
        <div class="ml-3">
          <h3 class="text-sm font-medium text-yellow-800">
            Connection Issue
          </h3>
          <p class="mt-1 text-sm text-yellow-700">
            Not connected to camera hub. Some features may be limited.
          </p>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mx-auto"></div>
      <p class="text-gray-600 mt-2">Loading cameras...</p>
    </div>

    <!-- Cameras Grid -->
    <div v-else-if="cameras.length > 0" class="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
      <CameraTile 
        v-for="camera in cameras" 
        :key="camera.id"
        :camera="camera"
        @view-stream="handleViewStream"
        @edit="handleEditCamera"
        @delete="handleDeleteCamera"
        @connect="handleConnectCamera"
        @disconnect="handleDisconnectCamera"
      />
    </div>

    <!-- Empty State -->
    <div v-else class="text-center py-12">
      <VideoCameraIcon class="h-16 w-16 text-gray-400 mx-auto mb-4" />
      <h3 class="text-lg font-medium text-gray-900 mb-2">No cameras configured</h3>
      <p class="text-gray-600 mb-4">Get started by adding your first camera</p>
      <button 
        @click="showAddModal = true"
        class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 transition-colors"
      >
        <PlusIcon class="h-4 w-4 mr-2" />
        Add Camera
      </button>
    </div>

    <!-- Add Camera Modal -->
    <AddCameraModal 
      :is-open="showAddModal"
      @close="showAddModal = false"
      @submit="handleAddCamera"
    />

    <!-- Edit Camera Modal -->
    <EditCameraModal 
      :is-open="showEditModal"
      :camera="selectedCamera"
      @close="showEditModal = false"
      @submit="handleUpdateCamera"
      @test-connection="handleTestConnection"
    />
  </div>
</template>
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { VideoCameraIcon, PlusIcon, ExclamationTriangleIcon } from '@heroicons/vue/24/outline'

import CameraTile from '@/components/CameraTile.vue'
import AddCameraModal from '@/components/AddCameraModal.vue'
import EditCameraModal from '@/components/EditCameraModal.vue'
import { useCameraManager } from '@/composables/useCamera'
import type { Camera, AddCameraRequest } from '@/types/camera'

const router = useRouter()
const cameraManager = useCameraManager()

// Reactive data - cameras come from the shared camera manager
const cameras = computed(() => {
  if (!cameraManager.cameras.value) return []
  
  return [...cameraManager.cameras.value].sort((a, b) => {
    // Sort alphabetically by name
    return a.name.localeCompare(b.name)
  })
})

// Loading state based on camera manager initialization
const loading = computed(() => {
  return !cameraManager.isConnected.value || !cameraManager.isInitialized.value
})

// Connection status
const isConnected = cameraManager.isConnected

const showAddModal = ref(false)
const showEditModal = ref(false)
const selectedCamera = ref<Camera>({} as Camera)

onMounted(() => {
  // Camera loading is now handled by the App.vue initialization
  // This view just displays the shared camera state
})

// Camera actions
const handleAddCamera = async (cameraData: AddCameraRequest) => {
  try {
    if (!cameraManager) {
      throw new Error('Camera manager not available')
    }

    await cameraManager.addCamera(cameraData)
    showAddModal.value = false
    
    // Show success message (you might want to use a toast notification)
    console.log('Camera added successfully:', cameraData.name)
  } catch (error) {
    console.error('Failed to add camera:', error)
    // Handle error (show error message to user)
  }
}

const handleEditCamera = (camera: Camera) => {
  selectedCamera.value = camera
  showEditModal.value = true
}

const handleUpdateCamera = async (updatedCamera: Camera) => {
  try {
    if (!cameraManager) {
      throw new Error('Camera manager not available')
    }

    await cameraManager.updateCamera(updatedCamera.id, updatedCamera)
    showEditModal.value = false
    
    console.log('Camera updated successfully:', updatedCamera.name)
  } catch (error) {
    console.error('Failed to update camera:', error)
    // Handle error
  }
}

const handleDeleteCamera = async (camera: Camera) => {
  // Show confirmation dialog
  if (!confirm(`Are you sure you want to delete "${camera.name}"?`)) {
    return
  }

  try {
    if (!cameraManager) {
      throw new Error('Camera manager not available')
    }

    await cameraManager.deleteCamera(camera.id)
    
    console.log('Camera deleted successfully:', camera.name)
  } catch (error) {
    console.error('Failed to delete camera:', error)
    // Handle error
  }
}

const handleViewStream = (camera: Camera) => {
  // Navigate to the live stream view
  router.push({ name: 'live', params: { id: camera.id } })
}

const handleConnectCamera = async (camera: Camera) => {
  try {
    console.log('Connecting to camera:', camera.name)
    const success = await cameraManager.connectCamera(camera.id)
    
    if (success) {
      console.log('Camera connected successfully:', camera.name)
    } else {
      console.warn('Failed to connect to camera:', camera.name)
    }
  } catch (error) {
    console.error('Error connecting to camera:', error)
  }
}

const handleDisconnectCamera = async (camera: Camera) => {
  try {
    console.log('Disconnecting from camera:', camera.name)
    const success = await cameraManager.disconnectCamera(camera.id)
    
    if (success) {
      console.log('Camera disconnected successfully:', camera.name)
    } else {
      console.warn('Failed to disconnect from camera:', camera.name)
    }
  } catch (error) {
    console.error('Error disconnecting from camera:', error)
  }
}

const handleTestConnection = async (camera: Camera) => {
  try {
    if (!cameraManager) {
      throw new Error('Camera manager not available')
    }

    const status = await cameraManager.getCameraStatus(camera.id)
    console.log('Connection test result:', status)
    
    // You could show a toast notification with the result
    alert(`Connection test: ${status?.isOnline ? 'Success' : 'Failed'}`)
  } catch (error) {
    console.error('Connection test failed:', error)
    alert('Connection test failed')
  }
}
</script>