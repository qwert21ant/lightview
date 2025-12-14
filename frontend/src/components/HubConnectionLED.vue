<template>
  <div 
    class="relative"
    @mouseenter="showTooltip = true"
    @mouseleave="showTooltip = false"
  >
    <div 
      class="w-3 h-3 rounded-full transition-all duration-300 cursor-help"
      :class="{
        'bg-green-500 shadow-green-500/50 shadow-md': connectionStatus === 'Connected',
        'bg-yellow-500 shadow-yellow-500/50 shadow-md animate-pulse': connectionStatus === 'Connecting',
        'bg-red-500 shadow-red-500/50 shadow-md': connectionStatus === 'Disconnected',
        'bg-gray-400': connectionStatus === 'Unknown'
      }"
    ></div>
    
    <!-- Tooltip -->
    <div 
      v-if="showTooltip"
      class="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-2 py-1 bg-gray-900 text-white text-xs rounded whitespace-nowrap z-50 shadow-lg"
    >
      {{ tooltipText }}
      <div class="absolute top-full left-1/2 transform -translate-x-1/2 w-0 h-0 border-l-2 border-r-2 border-t-2 border-transparent border-t-gray-900"></div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { HubConnectionState } from '@microsoft/signalr'
import type { BaseSignalRService } from '@/services/baseSignalR'

interface Props {
  hubService: BaseSignalRService
  hubName: string
}

const props = defineProps<Props>()
const showTooltip = ref(false)

const connectionStatus = computed(() => {
  if (!props.hubService) return 'Unknown'
  
  const state = props.hubService.connectionState.value
  if (props.hubService.isConnecting?.value) return 'Connecting'
  
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
  if (!props.hubService) return 'Unknown'
  
  if (props.hubService.connectionError?.value) {
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

const tooltipText = computed(() => {
  if (!props.hubService) return `${props.hubName}: Unknown`
  
  if (props.hubService.connectionError?.value) {
    return `${props.hubName}: Error - ${props.hubService.connectionError.value}`
  }
  
  return `${props.hubName}: ${connectionStatusText.value}`
})
</script>