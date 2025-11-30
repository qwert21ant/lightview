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
  </nav>
</template>

<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router'
import {
  HomeIcon,
  VideoCameraIcon,
  Cog6ToothIcon,
  ChartBarIcon
} from '@heroicons/vue/24/outline'

const $route = useRoute()

const navigation = [
  { name: 'Dashboard', to: '/', icon: HomeIcon },
  { name: 'Cameras', to: '/cameras', icon: VideoCameraIcon },
  { name: 'Analytics', to: '/analytics', icon: ChartBarIcon },
  { name: 'Settings', to: '/settings', icon: Cog6ToothIcon }
]
</script>