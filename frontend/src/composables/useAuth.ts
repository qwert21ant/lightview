import { ref, computed, watch } from 'vue'
import { authService, type UserInfo } from '@/services/auth'

// Global auth state
const user = ref<UserInfo | null>(null)
const isLoading = ref(false)
const error = ref<string | null>(null)

// Initialize user from localStorage on app start
const initUser = authService.getCurrentUser()
if (initUser) {
  user.value = initUser
}

export function useAuth() {
  const isAuthenticated = computed(() => !!user.value && authService.isAuthenticated())

  const login = async (username: string, password: string) => {
    try {
      isLoading.value = true
      error.value = null
      
      const response = await authService.login({ username, password })
      user.value = response.user
      
      return response
    } catch (err: any) {
      error.value = err.message
      throw err
    } finally {
      isLoading.value = false
    }
  }

  const logout = () => {
    authService.logout()
    user.value = null
    error.value = null
  }

  const refreshUserInfo = async () => {
    try {
      if (!isAuthenticated.value) return
      
      const userInfo = await authService.getCurrentUserInfo()
      user.value = userInfo
    } catch (err: any) {
      console.error('Failed to refresh user info:', err)
      // If refresh fails, probably token is invalid
      logout()
    }
  }

  const clearError = () => {
    error.value = null
  }

  return {
    user: computed(() => user.value),
    isAuthenticated,
    isLoading: computed(() => isLoading.value),
    error: computed(() => error.value),
    login,
    logout,
    refreshUserInfo,
    clearError
  }
}