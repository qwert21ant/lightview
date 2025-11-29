<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-3xl font-bold text-gray-900">Cameras</h1>
        <p class="text-gray-600 mt-1">Manage and monitor your IP cameras</p>
      </div>
      <button class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 transition-colors">
        <PlusIcon class="h-4 w-4 mr-2" />
        Add Camera
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto"></div>
      <p class="text-gray-600 mt-2">Loading cameras...</p>
    </div>

    <!-- Cameras Grid -->
    <div v-else class="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div 
        v-for="camera in cameras" 
        :key="camera.id"
        class="bg-white rounded-lg border shadow-sm hover:shadow-md transition-shadow"
      >
        <div class="p-6">
          <!-- Camera Header -->
          <div class="flex items-start justify-between mb-4">
            <div class="flex items-center space-x-3">
              <div class="p-2 bg-gray-100 rounded-lg">
                <VideoCameraIcon class="h-6 w-6 text-gray-600" />
              </div>
              <div>
                <h3 class="font-semibold text-gray-900">{{ camera.name }}</h3>
                <p class="text-sm text-gray-500">{{ camera.url }}</p>
              </div>
            </div>
          </div>

          <!-- Camera Status -->
          <div class="flex items-center justify-between">
            <div class="flex items-center space-x-2">
              <span 
                class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                :class="[getStatusColor(camera.status), getStatusBg(camera.status)]"
              >
                {{ camera.status.charAt(0).toUpperCase() + camera.status.slice(1) }}
              </span>
            </div>
            <div class="text-xs text-gray-500">
              Last seen: {{ new Date(camera.lastSeen).toLocaleTimeString() }}
            </div>
          </div>

          <!-- Actions -->
          <div class="mt-4 flex space-x-2">
            <button class="flex-1 px-3 py-2 text-sm bg-primary-600 text-white rounded-md hover:bg-primary-700 transition-colors">
              View Stream
            </button>
            <button class="px-3 py-2 text-sm border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 transition-colors">
              Settings
            </button>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-if="cameras.length === 0" class="col-span-full text-center py-12">
        <VideoCameraIcon class="h-16 w-16 text-gray-400 mx-auto mb-4" />
        <h3 class="text-lg font-medium text-gray-900 mb-2">No cameras configured</h3>
        <p class="text-gray-600 mb-4">Get started by adding your first camera</p>
        <button class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 transition-colors">
          <PlusIcon class="h-4 w-4 mr-2" />
          Add Camera
        </button>
      </div>
    </div>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { VideoCameraIcon, PlusIcon } from '@heroicons/vue/24/outline'

// Demo camera data - will be replaced with API calls
const cameras = ref([
  {
    id: '1',
    name: 'Front Door Camera',
    status: 'online',
    url: 'rtsp://192.168.1.100/stream1',
    lastSeen: new Date().toISOString(),
  },
  {
    id: '2',
    name: 'Backyard Camera',
    status: 'offline',
    url: 'rtsp://192.168.1.101/stream1',
    lastSeen: new Date(Date.now() - 300000).toISOString(),
  },
])

const loading = ref(false)

onMounted(() => {
  // TODO: Load cameras from API
})

const getStatusColor = (status: string) => {
  return status === 'online' ? 'text-green-600' : 'text-red-600'
}

const getStatusBg = (status: string) => {
  return status === 'online' ? 'bg-green-100' : 'bg-red-100'
}
</script>