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
provide(CAMERA_MANAGER_KEY, cameraManager)

// Show main layout (sidebar + header) for authenticated users on protected routes
const showMainLayout = computed(() => {
  return isAuthenticated.value && route.meta.requiresAuth
})

// Auto-connect to camera hub when user is authenticated
onMounted(() => {
  if (isAuthenticated.value) {
    connectToHub()
  }
})

// Watch for authentication changes
watch(isAuthenticated, (newValue) => {
  if (newValue) {
    connectToHub()
  } else {
    disconnectFromHub()
  }
})

async function connectToHub() {
  try {
    if (!cameraManager.isConnected()) {
      console.log('Auto-connecting to Camera Hub...')
      await cameraManager.connect()
      console.log('Successfully connected to Camera Hub')
    }
  } catch (error) {
    console.error('Failed to auto-connect to Camera Hub:', error)
  }
}

async function disconnectFromHub() {
  try {
    if (cameraManager.isConnected()) {
      console.log('Disconnecting from Camera Hub...')
      await cameraManager.disconnect()
      console.log('Successfully disconnected from Camera Hub')
    }
  } catch (error) {
    console.error('Failed to disconnect from Camera Hub:', error)
  }
}
</script>