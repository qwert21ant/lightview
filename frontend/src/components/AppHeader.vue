<template>
  <header class="bg-white shadow-sm border-b">
    <div class="flex items-center justify-between h-16 px-6">
      <!-- Project Title -->
      <div class="flex items-center space-x-2">
        <VideoCameraIcon class="h-8 w-8 text-indigo-600" />
        <h1 class="text-xl font-bold text-gray-900">LightView</h1>
      </div>
      
      <!-- User Info -->
      <div class="flex items-center space-x-4">
        <div class="text-sm">
          <span class="text-gray-600">Welcome, </span>
          <span class="font-medium text-gray-900">{{ user?.username }}</span>
        </div>
        
        <button
          @click="handleLogout"
          class="flex items-center space-x-1 px-3 py-2 text-sm font-medium text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
        >
          <ArrowRightOnRectangleIcon class="h-4 w-4" />
          <span>Logout</span>
        </button>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { useAuth } from '@/composables/useAuth'
import { VideoCameraIcon, ArrowRightOnRectangleIcon } from '@heroicons/vue/24/outline'

const { user, logout } = useAuth()

const handleLogout = () => {
  logout()
}
</script>