<template>
  <nav class="bg-white shadow-lg h-full flex flex-col">
    <!-- Navigation Items -->
    <div class="flex-1 px-4 py-6 space-y-2">
      <RouterLink
        v-for="item in navigation"
        :key="item.name"
        :to="item.to"
        class="group flex items-center px-3 py-2 text-sm font-medium rounded-md transition-colors"
        :class="[
          $route.path === item.to
            ? 'bg-indigo-100 text-indigo-700'
            : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'
        ]"
      >
        <component
          :is="item.icon"
          class="mr-3 h-5 w-5 flex-shrink-0"
          :class="[
            $route.path === item.to
              ? 'text-indigo-500'
              : 'text-gray-400 group-hover:text-gray-500'
          ]"
        />
        {{ item.name }}
      </RouterLink>
    </div>
    
    <!-- Connection Status -->
    <div class="px-4 py-3 border-t border-gray-200 bg-gray-50">
      <div class="flex items-center space-x-3">
        <!-- Status LED -->
        <div 
          class="w-3 h-3 rounded-full transition-all duration-300"
          :class="{
            'bg-green-500 shadow-green-500/50 shadow-md': connectionStatus === 'Connected',
            'bg-yellow-500 shadow-yellow-500/50 shadow-md animate-pulse': connectionStatus === 'Connecting',
            'bg-red-500 shadow-red-500/50 shadow-md': connectionStatus === 'Disconnected',
            'bg-gray-400': connectionStatus === 'Unknown'
          }"
        ></div>
        
        <!-- Status Text -->
        <div class="flex-1">
          <div class="text-xs font-medium text-gray-700">
            Camera Hub
          </div>
          <div 
            class="text-xs transition-colors"
            :class="{
              'text-green-600': connectionStatus === 'Connected',
              'text-yellow-600': connectionStatus === 'Connecting',
              'text-red-600': connectionStatus === 'Disconnected',
              'text-gray-500': connectionStatus === 'Unknown'
            }"
          >
            {{ connectionStatusText }}
          </div>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import {
  HomeIcon,
  VideoCameraIcon,
  Cog6ToothIcon,
  ChartBarIcon
} from '@heroicons/vue/24/outline'
import { HubConnectionState } from '@microsoft/signalr'
import { useCameraManager } from '@/composables/useCamera'

const $route = useRoute()
const cameraManager = useCameraManager()

const navigation = [
  { name: 'Dashboard', to: '/', icon: HomeIcon },
  { name: 'Cameras', to: '/cameras', icon: VideoCameraIcon },
  { name: 'Analytics', to: '/analytics', icon: ChartBarIcon },
  { name: 'Settings', to: '/settings', icon: Cog6ToothIcon }
]

// Connection status computed properties
const connectionStatus = computed(() => {
  if (!cameraManager) return 'Unknown'
  
  const state = cameraManager.connectionState.value
  if (cameraManager.isConnecting.value) return 'Connecting'
  
  switch (state) {
    case HubConnectionState.Connected:
      return 'Connected'
    case HubConnectionState.Connecting:
    case HubConnectionState.Reconnecting:
      return 'Connecting'
    case HubConnectionState.Disconnected:
    case HubConnectionState.Disconnecting:
      return 'Disconnected'
    default:
      return 'Unknown'
  }
})

const connectionStatusText = computed(() => {
  if (!cameraManager) return 'Unknown'
  
  if (cameraManager.connectionError.value) {
    return 'Error'
  }
  
  switch (connectionStatus.value) {
    case 'Connected':
      return 'Connected'
    case 'Connecting':
      return 'Connecting...'
    case 'Disconnected':
      return 'Disconnected'
    default:
      return 'Unknown'
  }
})
</script>