// Authentication-related types and interfaces
export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  expiresAt: string
  user: UserInfo
}

export interface UserInfo {
  id: string
  username: string
}