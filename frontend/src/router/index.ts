import { createRouter, createWebHistory, type RouteLocationNormalized } from 'vue-router'
import { authService } from '@/services/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/',
      name: 'home',
      component: () => import('@/views/HomeView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/cameras',
      name: 'cameras',
      component: () => import('@/views/CamerasView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/analytics',
      name: 'analytics',
      component: () => import('@/views/AnalyticsView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/views/SettingsView.vue'),
      meta: { requiresAuth: true }
    },
  ],
})

// Auth guard
router.beforeEach((to: RouteLocationNormalized) => {
  const requiresAuth = to.meta.requiresAuth
  const isAuthenticated = authService.isAuthenticated()
  
  if (requiresAuth && !isAuthenticated) {
    // Redirect to login page
    return { name: 'login' }
  }
  
  // If user is authenticated and trying to access login, redirect to home
  if (to.name === 'login' && isAuthenticated) {
    return { name: 'home' }
  }
})

export default router
