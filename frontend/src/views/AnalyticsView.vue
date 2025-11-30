<template>
  <div>
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900">Analytics</h1>
      <p class="mt-2 text-gray-600">Monitor system performance and camera statistics</p>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
      <!-- Stats Cards -->
      <div class="bg-white rounded-lg shadow p-6">
        <div class="flex items-center">
          <div class="p-3 rounded-md bg-green-50">
            <VideoCameraIcon class="h-6 w-6 text-green-600" />
          </div>
          <div class="ml-4">
            <p class="text-sm font-medium text-gray-600">Active Cameras</p>
            <p class="text-2xl font-semibold text-gray-900">12</p>
          </div>
        </div>
      </div>

      <div class="bg-white rounded-lg shadow p-6">
        <div class="flex items-center">
          <div class="p-3 rounded-md bg-blue-50">
            <SignalIcon class="h-6 w-6 text-blue-600" />
          </div>
          <div class="ml-4">
            <p class="text-sm font-medium text-gray-600">Total Streams</p>
            <p class="text-2xl font-semibold text-gray-900">8</p>
          </div>
        </div>
      </div>

      <div class="bg-white rounded-lg shadow p-6">
        <div class="flex items-center">
          <div class="p-3 rounded-md bg-yellow-50">
            <ExclamationTriangleIcon class="h-6 w-6 text-yellow-600" />
          </div>
          <div class="ml-4">
            <p class="text-sm font-medium text-gray-600">Alerts</p>
            <p class="text-2xl font-semibold text-gray-900">3</p>
          </div>
        </div>
      </div>

      <div class="bg-white rounded-lg shadow p-6">
        <div class="flex items-center">
          <div class="p-3 rounded-md bg-purple-50">
            <ChartBarIcon class="h-6 w-6 text-purple-600" />
          </div>
          <div class="ml-4">
            <p class="text-sm font-medium text-gray-600">Uptime</p>
            <p class="text-2xl font-semibold text-gray-900">99.8%</p>
          </div>
        </div>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Chart Placeholder -->
      <div class="bg-white rounded-lg shadow p-6">
        <h3 class="text-lg font-medium text-gray-900 mb-4">Camera Performance</h3>
        <div class="h-64 bg-gray-50 rounded flex items-center justify-center">
          <p class="text-gray-500">Chart placeholder - integrate with chart library</p>
        </div>
      </div>

      <!-- Recent Activities -->
      <div class="bg-white rounded-lg shadow p-6">
        <h3 class="text-lg font-medium text-gray-900 mb-4">Recent Activities</h3>
        <div class="space-y-4">
          <div class="flex items-center space-x-3">
            <div class="w-2 h-2 bg-green-400 rounded-full"></div>
            <p class="text-sm text-gray-600">Camera 01 connected</p>
            <span class="text-xs text-gray-400">2 min ago</span>
          </div>
          <div class="flex items-center space-x-3">
            <div class="w-2 h-2 bg-yellow-400 rounded-full"></div>
            <p class="text-sm text-gray-600">Motion detected in Zone A</p>
            <span class="text-xs text-gray-400">5 min ago</span>
          </div>
          <div class="flex items-center space-x-3">
            <div class="w-2 h-2 bg-red-400 rounded-full"></div>
            <p class="text-sm text-gray-600">Camera 05 offline</p>
            <span class="text-xs text-gray-400">12 min ago</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import {
  VideoCameraIcon,
  SignalIcon,
  ExclamationTriangleIcon,
  ChartBarIcon
} from '@heroicons/vue/24/outline'
</script>