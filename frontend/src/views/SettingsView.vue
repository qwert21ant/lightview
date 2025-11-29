<template>
  <div class="space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-3xl font-bold text-gray-900">Settings</h1>
      <p class="text-gray-600 mt-1">Configure system settings and preferences</p>
    </div>

    <div class="grid lg:grid-cols-3 gap-6">
      <!-- Core Service Settings -->
      <div class="bg-white rounded-lg border shadow-sm">
        <div class="p-6">
          <div class="flex items-center space-x-3 mb-4">
            <div class="p-2 bg-primary-100 rounded-lg">
              <ServerIcon class="h-6 w-6 text-primary-600" />
            </div>
            <h3 class="text-lg font-semibold text-gray-900">Core Service</h3>
          </div>
          
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Service URL</label>
              <input 
                v-model="settings.coreService.url"
                type="url" 
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Timeout (seconds)</label>
              <input 
                v-model="settings.coreService.timeout"
                type="number" 
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Camera Controller Settings -->
      <div class="bg-white rounded-lg border shadow-sm">
        <div class="p-6">
          <div class="flex items-center space-x-3 mb-4">
            <div class="p-2 bg-primary-100 rounded-lg">
              <CogIcon class="h-6 w-6 text-primary-600" />
            </div>
            <h3 class="text-lg font-semibold text-gray-900">Camera Controller</h3>
          </div>
          
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Service URL</label>
              <input 
                v-model="settings.cameraController.url"
                type="url" 
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Timeout (seconds)</label>
              <input 
                v-model="settings.cameraController.timeout"
                type="number" 
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Database Settings -->
      <div class="bg-white rounded-lg border shadow-sm">
        <div class="p-6">
          <div class="flex items-center space-x-3 mb-4">
            <div class="p-2 bg-primary-100 rounded-lg">
              <CircleStackIcon class="h-6 w-6 text-primary-600" />
            </div>
            <h3 class="text-lg font-semibold text-gray-900">Database</h3>
          </div>
          
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Host</label>
              <input 
                v-model="settings.database.host"
                type="text" 
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Port</label>
              <input 
                v-model="settings.database.port"
                type="number" 
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Database Name</label>
              <input 
                v-model="settings.database.database"
                type="text" 
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Save Button -->
    <div class="text-right">
      <button 
        @click="saveSettings"
        class="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 transition-colors"
      >
        Save Settings
      </button>
    </div>
  </div>
</template>
<script setup lang="ts">
import { ref } from 'vue'
import { CogIcon, ServerIcon, CircleStackIcon } from '@heroicons/vue/24/outline'

const settings = ref({
  coreService: {
    url: 'http://localhost:5000',
    timeout: 30
  },
  cameraController: {
    url: 'http://localhost:5002',
    timeout: 30
  },
  database: {
    host: 'localhost',
    port: 5432,
    database: 'lightview_core'
  }
})

const saveSettings = () => {
  // TODO: Save settings via API
  console.log('Saving settings:', settings.value)
}
</script>