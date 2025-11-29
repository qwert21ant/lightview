<template>
  <header class="bg-white shadow-sm border-b">
    <div class="container mx-auto px-4">
      <div class="flex items-center justify-between h-16">
        <div class="flex items-center space-x-2">
          <VideoCameraIcon class="h-8 w-8 text-primary-600" />
          <h1 class="text-xl font-bold text-gray-900">LightView</h1>
        </div>
        
        <nav class="flex items-center space-x-6">
          <RouterLink 
            to="/" 
            class="flex items-center space-x-1 px-3 py-2 rounded-md text-sm font-medium transition-colors hover:bg-gray-100"
            :class="{ 'text-primary-600 bg-primary-50': $route.path === '/', 'text-gray-600': $route.path !== '/' }"
          >
            <HomeIcon class="h-4 w-4" />
            <span>Home</span>
          </RouterLink>
          
          <RouterLink 
            to="/cameras" 
            class="flex items-center space-x-1 px-3 py-2 rounded-md text-sm font-medium transition-colors hover:bg-gray-100"
            :class="{ 'text-primary-600 bg-primary-50': $route.path.startsWith('/cameras'), 'text-gray-600': !$route.path.startsWith('/cameras') }"
          >
            <VideoCameraIcon class="h-4 w-4" />
            <span>Cameras</span>
          </RouterLink>
          
          <RouterLink 
            to="/settings" 
            class="flex items-center space-x-1 px-3 py-2 rounded-md text-sm font-medium transition-colors hover:bg-gray-100"
            :class="{ 'text-primary-600 bg-primary-50': $route.path.startsWith('/settings'), 'text-gray-600': !$route.path.startsWith('/settings') }"
          >
            <CogIcon class="h-4 w-4" />
            <span>Settings</span>
          </RouterLink>
        </nav>
      </div>
    </div>
  </header>
</template>
<script setup lang="ts">
import { RouterLink } from 'vue-router'
import { VideoCameraIcon, HomeIcon, CogIcon } from '@heroicons/vue/24/outline'
</script>