<template>
  <Teleport to="body">
    <div class="fixed top-4 right-4 z-50 space-y-2 max-w-sm">
      <TransitionGroup
        name="notification"
        tag="div"
        class="space-y-2"
      >
        <div
          v-for="notification in notificationService.getNotifications().value"
          :key="notification.id"
          :class="getNotificationClasses(notification.type)"
          class="notification-item rounded-lg shadow-lg p-4 border-l-4 backdrop-blur-sm"
        >
          <div class="flex items-start justify-between">
            <div class="flex items-start space-x-3 flex-1 min-w-0">
              <!-- Icon -->
              <div class="flex-shrink-0">
                <CheckCircleIcon v-if="notification.type === 'success'" class="w-5 h-5" />
                <ExclamationTriangleIcon v-else-if="notification.type === 'warning'" class="w-5 h-5" />
                <XCircleIcon v-else-if="notification.type === 'error'" class="w-5 h-5" />
                <InformationCircleIcon v-else class="w-5 h-5" />
              </div>
              
              <!-- Content -->
              <div class="flex-1 min-w-0">
                <h4 class="text-sm font-medium truncate">{{ notification.title }}</h4>
                <p class="text-sm opacity-90 mt-1 break-words">{{ notification.message }}</p>
              </div>
            </div>
            
            <!-- Close button -->
            <button
              @click="notificationService.remove(notification.id)"
              class="flex-shrink-0 ml-3 text-current opacity-60 hover:opacity-100 transition-opacity"
            >
              <XMarkIcon class="w-4 h-4" />
            </button>
          </div>
          
          <!-- Progress bar for auto-dismiss -->
          <div
            v-if="notification.duration && notification.duration > 0"
            class="mt-3 h-1 bg-black/10 rounded-full overflow-hidden"
          >
            <div
              class="h-full bg-current rounded-full notification-progress"
              :style="{ animationDuration: `${notification.duration}ms` }"
            ></div>
          </div>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { useNotifications } from '@/composables/useNotifications'
import {
  CheckCircleIcon,
  ExclamationTriangleIcon,
  XCircleIcon,
  InformationCircleIcon,
  XMarkIcon
} from '@heroicons/vue/24/outline'
import type { Notification } from '@/services/notificationService'

const notificationService = useNotifications()

function getNotificationClasses(type: Notification['type']): string {
  const baseClasses = 'border-l-4 text-white'
  
  switch (type) {
    case 'success':
      return `${baseClasses} bg-green-600/95 border-green-500`
    case 'error':
      return `${baseClasses} bg-red-600/95 border-red-500`
    case 'warning':
      return `${baseClasses} bg-yellow-600/95 border-yellow-500`
    case 'info':
      return `${baseClasses} bg-blue-600/95 border-blue-500`
    default:
      return `${baseClasses} bg-gray-600/95 border-gray-500`
  }
}
</script>

<style scoped>
/* Notification animations */
.notification-enter-active {
  transition: all 0.3s ease-out;
}

.notification-leave-active {
  transition: all 0.3s ease-in;
}

.notification-enter-from {
  transform: translateX(100%);
  opacity: 0;
}

.notification-leave-to {
  transform: translateX(100%);
  opacity: 0;
}

.notification-move {
  transition: transform 0.3s ease;
}

/* Progress bar animation */
.notification-progress {
  animation: progress-countdown linear forwards;
  transform-origin: left;
}

@keyframes progress-countdown {
  from {
    transform: scaleX(1);
  }
  to {
    transform: scaleX(0);
  }
}

/* Notification item styling */
.notification-item {
  max-width: 24rem;
  word-wrap: break-word;
}
</style>