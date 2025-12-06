<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Check if current route requires auth and user is authenticated -->
    <div v-if="showMainLayout" class="h-screen flex flex-col">
      <!-- Header spans full width -->
      <AppHeader />
      
      <!-- Content area with sidebar -->
      <div class="flex flex-1 overflow-hidden">
        <!-- Sidebar -->
        <aside class="w-64 bg-white shadow-lg">
          <AppSidebar />
        </aside>
        
        <!-- Main Content -->
        <main class="flex-1 overflow-y-auto bg-gray-50">
          <div class="container mx-auto px-6 py-8">
            <RouterView />
          </div>
        </main>
      </div>
    </div>
    
    <!-- Login page (no sidebar/header) -->
    <div v-else>
      <RouterView />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, provide, onMounted, watch } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import { CameraManager } from '@/services/cameraManager'
import { CAMERA_MANAGER_KEY } from '@/composables/useCamera'
import AppHeader from '@/components/AppHeader.vue'
import AppSidebar from '@/components/AppSidebar.vue'

const route = useRoute()
const { isAuthenticated } = useAuth()

// Create and provide CameraManager instance
const cameraManager = new CameraManager()
// @ts-ignore
window.__cameraManager = cameraManager // For debugging
provide(CAMERA_MANAGER_KEY, cameraManager)

// Show main layout (sidebar + header) for authenticated users on protected routes
const showMainLayout = computed(() => {
  return isAuthenticated.value && route.meta.requiresAuth
})

// Auto-connect and initialize camera manager when user is authenticated
onMounted(() => {
  if (isAuthenticated.value) {
    initializeCameraManager()
  }
})

// Watch for authentication changes
watch(isAuthenticated, (newValue) => {
  if (newValue) {
    initializeCameraManager()
  } else {
    disconnectFromHub()
  }
})

async function initializeCameraManager() {
  try {
    if (!cameraManager.isConnected.value) {
      console.log('Initializing Camera Manager...')
      
      // Set up event handlers for camera updates
      cameraManager.setEventHandlers({
        onCameraAdded: (camera) => {
          console.log('Camera added:', camera.name)
        },
        onCameraUpdated: (camera) => {
          console.log('Camera updated:', camera.name)
        },
        onCameraDeleted: (cameraId) => {
          console.log('Camera deleted:', cameraId)
        },
        onCameraConnected: (cameraId) => {
          console.log('Camera connected:', cameraId)
        },
        onCameraDisconnected: (cameraId) => {
          console.log('Camera disconnected:', cameraId)
        },
        onCameraEvent: (cameraId, eventType, data) => {
          console.log(`Camera ${cameraId} event:`, eventType, data)
        }
      })
      
      // Connect to hub and load initial cameras
      await cameraManager.connect()
      console.log('Successfully initialized Camera Manager')
    }
  } catch (error) {
    console.error('Failed to initialize Camera Manager:', error)
  }
}

async function disconnectFromHub() {
  try {
    if (cameraManager.isConnected.value) {
      console.log('Disconnecting from Camera Hub...')
      await cameraManager.disconnect()
      // Clear cameras when disconnecting
      cameraManager.cameras.value = []
      console.log('Successfully disconnected from Camera Hub')
    }
  } catch (error) {
    console.error('Failed to disconnect from Camera Hub:', error)
  }
}
</script>