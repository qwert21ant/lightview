import axios from 'axios'
import { API_BASE_URL, API_TIMEOUT } from '@/config/api'
import type { LoginRequest, LoginResponse, UserInfo } from '@/types/auth'
import router from '@/router'

// Create axios instance
const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: API_TIMEOUT,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Re-export types for convenience
export type { LoginRequest, LoginResponse, UserInfo }

// Auth Service
class AuthService {
  private readonly TOKEN_KEY = 'lightview_token'
  private readonly USER_KEY = 'lightview_user'

  constructor() {
    // Add request interceptor to include auth token
    api.interceptors.request.use((config) => {
      const token = this.getToken()
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      return config
    })

    // Add response interceptor to handle auth errors
    api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          // Don't redirect if this is a login attempt (login endpoint returns 401 for bad credentials)
          const isLoginAttempt = error.config?.url?.includes('/api/auth/login')
          if (!isLoginAttempt) {
            console.warn('Authentication failed - redirecting to login')
            this.logout()
          }
        }
        return Promise.reject(error)
      }
    )
  }

  async login(credentials: LoginRequest): Promise<LoginResponse> {
    try {
      const response = await api.post<LoginResponse>('/api/auth/login', credentials)
      const { token, user, expiresAt } = response.data
      
      // Store token and user info
      localStorage.setItem(this.TOKEN_KEY, token)
      localStorage.setItem(this.USER_KEY, JSON.stringify(user))
      
      return response.data
    } catch (error: any) {
      // Handle different error scenarios
      if (error.response?.status === 401) {
        throw new Error('Invalid username or password')
      }
      throw new Error(error.response?.data?.message || error.message || 'Login failed')
    }
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY)
    localStorage.removeItem(this.USER_KEY)

    // Use router navigation if available, fallback to window.location
    if (router) {
      router.push('/login').catch(() => {
        // Fallback if router navigation fails
        window.location.href = '/login'
      })
    } else {
      window.location.href = '/login'
    }
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY)
  }

  getCurrentUser(): UserInfo | null {
    const userStr = localStorage.getItem(this.USER_KEY)
    if (!userStr) return null
    
    try {
      return JSON.parse(userStr)
    } catch {
      return null
    }
  }

  isAuthenticated(): boolean {
    const token = this.getToken()
    const user = this.getCurrentUser()
    return !!(token && user)
  }

  async getCurrentUserInfo(): Promise<UserInfo> {
    try {
      const response = await api.get<UserInfo>('/api/auth/me')
      return response.data
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Failed to get user info')
    }
  }
}

export const authService = new AuthService()
export { api }