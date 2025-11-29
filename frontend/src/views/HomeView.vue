<template>
  <div class="space-y-8">
    <!-- Hero Section -->
    <div class="text-center">
      <h1 class="text-4xl font-bold text-gray-900 mb-4">
        Welcome to LightView
      </h1>
      <p class="text-xl text-gray-600 max-w-2xl mx-auto">
        Professional camera monitoring and streaming solution for managing your IP cameras with ease.
      </p>
    </div>

    <!-- Features Grid -->
    <div class="grid md:grid-cols-3 gap-8 mt-16">
      <div class="text-center p-6 rounded-lg bg-white shadow-sm border">
        <div class="mx-auto h-16 w-16 bg-primary-100 rounded-full flex items-center justify-center mb-4">
          <VideoCameraIcon class="h-8 w-8 text-primary-600" />
        </div>
        <h3 class="text-lg font-semibold text-gray-900 mb-2">Camera Management</h3>
        <p class="text-gray-600">
          Add, configure, and monitor multiple IP cameras from a single dashboard.
        </p>
      </div>

      <div class="text-center p-6 rounded-lg bg-white shadow-sm border">
        <div class="mx-auto h-16 w-16 bg-primary-100 rounded-full flex items-center justify-center mb-4">
          <EyeIcon class="h-8 w-8 text-primary-600" />
        </div>
        <h3 class="text-lg font-semibold text-gray-900 mb-2">Live Streaming</h3>
        <p class="text-gray-600">
          Stream live video feeds from your cameras with WebRTC technology.
        </p>
      </div>

      <div class="text-center p-6 rounded-lg bg-white shadow-sm border">
        <div class="mx-auto h-16 w-16 bg-primary-100 rounded-full flex items-center justify-center mb-4">
          <SignalIcon class="h-8 w-8 text-primary-600" />
        </div>
        <h3 class="text-lg font-semibold text-gray-900 mb-2">Health Monitoring</h3>
        <p class="text-gray-600">
          Real-time monitoring of camera status and automatic reconnection handling.
        </p>
      </div>
    </div>

    <!-- Quick Actions -->
    <div class="text-center mt-12">
      <RouterLink 
        to="/cameras" 
        class="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 transition-colors"
      >
        <VideoCameraIcon class="h-5 w-5 mr-2" />
        View Cameras
      </RouterLink>
    </div>
  </div>
</template>
<script setup lang="ts">
import { VideoCameraIcon, EyeIcon, SignalIcon } from '@heroicons/vue/24/outline'
</script>