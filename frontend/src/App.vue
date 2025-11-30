<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Check if current route requires auth and user is authenticated -->
    <div v-if="showMainLayout" class="flex h-screen">
      <!-- Sidebar -->
      <aside class="w-64 bg-white shadow-lg">
        <AppSidebar />
      </aside>
      
      <!-- Main Content Area -->
      <div class="flex-1 flex flex-col overflow-hidden">
        <!-- Header -->
        <AppHeader />
        
        <!-- Main Content -->
        <main class="flex-1 overflow-y-auto bg-gray-50">
          <div class="container mx-auto px-6 py-8">
            <RouterView />
          </div>
        </main>
      </div>
    </div>
    
    <!-- Login page (no sidebar/header) -->
    <div v-else>
      <RouterView />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import AppHeader from '@/components/AppHeader.vue'
import AppSidebar from '@/components/AppSidebar.vue'

const route = useRoute()
const { isAuthenticated } = useAuth()

// Show main layout (sidebar + header) for authenticated users on protected routes
const showMainLayout = computed(() => {
  return isAuthenticated.value && route.meta.requiresAuth
})
</script>