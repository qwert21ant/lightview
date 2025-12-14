<template>
  <div class="space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-3xl font-bold text-gray-900">Settings</h1>
      <p class="text-gray-600 mt-1">Configure camera monitoring defaults</p>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto"></div>
      <p class="text-gray-600 mt-2">Loading settings...</p>
    </div>

    <div v-else class="grid lg:grid-cols-2 gap-6">
      <!-- Camera Monitoring Settings -->
      <div class="bg-white rounded-lg border shadow-sm">
        <div class="p-6">
          <div class="flex items-center justify-between mb-4">
            <div class="flex items-center space-x-3">
              <div class="p-2 bg-primary-100 rounded-lg">
                <CogIcon class="h-6 w-6 text-primary-600" />
              </div>
              <h3 class="text-lg font-semibold text-gray-900">Camera Monitoring</h3>
            </div>
            <div class="flex items-center space-x-2">
              <button 
                v-if="!isEditMode"
                @click="enterEditMode"
                class="inline-flex items-center px-3 py-1.5 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 transition-colors"
              >
                Edit
              </button>
              <template v-else>
                <button 
                  @click="cancelEdit"
                  class="inline-flex items-center px-3 py-1.5 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button 
                  @click="save"
                  class="inline-flex items-center px-3 py-1.5 border text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 transition-colors"
                >
                  Save
                </button>
              </template>
            </div>
          </div>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Health Check Interval (seconds)</label>
              <input v-model.number="formSettings.healthCheckInterval" type="number" min="10" :disabled="!isEditMode" class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-50 disabled:text-gray-500" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Health Check Timeout (seconds)</label>
              <input v-model.number="formSettings.healthCheckTimeout" type="number" min="1" :disabled="!isEditMode" class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-50 disabled:text-gray-500" />
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Failure Threshold</label>
                <input v-model.number="formSettings.failureThreshold" type="number" min="1" :disabled="!isEditMode" class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-50 disabled:text-gray-500" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Success Threshold</label>
                <input v-model.number="formSettings.successThreshold" type="number" min="1" :disabled="!isEditMode" class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-50 disabled:text-gray-500" />
              </div>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Snapshot Interval (seconds)</label>
              <input v-model.number="formSettings.snapshotInterval" type="number" min="10" :disabled="!isEditMode" class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-50 disabled:text-gray-500" />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
<script setup lang="ts">
import { ref, reactive, watch, computed } from 'vue'
import { CogIcon, ServerIcon, CircleStackIcon } from '@heroicons/vue/24/outline'
import type { CameraMonitoringSettings } from '@/models/settings'
import { useSettings } from '@/composables/useSettings'

const settingsHubService = useSettings()
const isEditMode = ref(false)

// Loading state based on settings hub initialization
const loading = computed(() => {
  return !settingsHubService.isInitialized.value
})

// Separate form object for editing
const formSettings = reactive<CameraMonitoringSettings>({
  healthCheckInterval: 60,
  healthCheckTimeout: 30,
  failureThreshold: 3,
  successThreshold: 1,
  snapshotInterval: 300
})

// Copy current settings to form when hub settings change
watch(() => settingsHubService.settings.value, (newSettings) => {
  if (newSettings && !isEditMode.value) {
    Object.assign(formSettings, newSettings)
  }
}, { immediate: true })

function enterEditMode() {
  // Copy current settings from hub to form
  Object.assign(formSettings, settingsHubService.settings.value)
  isEditMode.value = true
}

function cancelEdit() {
  // Revert form to current hub settings
  Object.assign(formSettings, settingsHubService.settings.value)
  isEditMode.value = false
}

async function save() {
  try {
    await settingsHubService.updateCameraMonitoringSettings(formSettings)
    isEditMode.value = false
  } catch (e: any) {
    console.error('Failed to save settings:', e.message)
    // Could show a toast notification here
  }
}
</script>