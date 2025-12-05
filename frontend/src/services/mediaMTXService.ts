import { MediaMTXWebRTCReader, type MediaMTXReaderConfig } from '@/mediamtx/MediaMTXWebRTCReader'
import { MEDIAMTX_WEBRTC_URL, MEDIAMTX_API_URL } from '@/config/api'

/**
 * MediaMTX WebRTC service for managing MediaMTX WebRTC connections
 */

export class MediaMTXService {
  private static instance: MediaMTXService

  private constructor() {}

  static getInstance(): MediaMTXService {
    if (!MediaMTXService.instance) {
      MediaMTXService.instance = new MediaMTXService()
    }
    return MediaMTXService.instance
  }

  /**
   * Create a new MediaMTX WebRTC reader instance
   */
  createReader(config: MediaMTXReaderConfig): MediaMTXWebRTCReader {
    return new MediaMTXWebRTCReader(config)
  }

  /**
   * Check if MediaMTX reader is available (always true now since it's imported)
   */
  isReaderLoaded(): boolean {
    return true
  }

  /**
   * Create WebRTC stream URL for MediaMTX
   */
  createStreamUrl(baseUrl: string, streamPath: string, options?: {
    user?: string
    pass?: string
    token?: string
  }): string {
    try {
      // Remove trailing slash from base URL
      const cleanBaseUrl = baseUrl.replace(/\/$/, '')
      
      // Remove leading slash from stream path
      const cleanStreamPath = streamPath.replace(/^\//, '')
      
      // Create URL
      const url = new URL(`${cleanBaseUrl}/${cleanStreamPath}/whep`)
      
      // Add authentication parameters
      if (options?.user && options?.pass) {
        url.username = options.user
        url.password = options.pass
      }
      
      if (options?.token) {
        url.searchParams.set('token', options.token)
      }
      
      return url.toString()
    } catch (error) {
      throw new Error(`Invalid MediaMTX URL parameters: ${error}`)
    }
  }

  /**
   * Parse MediaMTX stream URL and extract components
   */
  parseStreamUrl(streamUrl: string): {
    baseUrl: string
    streamPath: string
    user?: string
    pass?: string
    token?: string
  } {
    try {
      const url = new URL(streamUrl)
      
      // Extract base URL (protocol + host + port)
      const baseUrl = `${url.protocol}//${url.host}`
      
      // Extract stream path (remove /whep suffix)
      let streamPath = url.pathname.replace(/\/whep$/, '')
      if (streamPath.startsWith('/')) {
        streamPath = streamPath.substring(1)
      }
      
      const result: any = {
        baseUrl,
        streamPath
      }
      
      // Extract authentication
      if (url.username) {
        result.user = decodeURIComponent(url.username)
      }
      if (url.password) {
        result.pass = decodeURIComponent(url.password)
      }
      
      const token = url.searchParams.get('token')
      if (token) {
        result.token = token
      }
      
      return result
    } catch (error) {
      throw new Error(`Failed to parse MediaMTX stream URL: ${error}`)
    }
  }

  /**
   * Convert direct MediaMTX URL to proxied URL through nginx
   */
  convertToProxiedUrl(mediaUrl: string): string {
    try {
      const url = new URL(mediaUrl)
      
      // Check if this is a MediaMTX URL (port 8889 for WebRTC)
      if (url.port === '8889' || url.pathname.includes('/whep')) {
        // Convert to proxied URL
        const streamPath = url.pathname.replace(/^\//, '').replace(/\/whep$/, '')
        const proxiedUrl = new URL(`${MEDIAMTX_WEBRTC_URL}/${streamPath}/whep`, window.location.origin)
        
        // Preserve query parameters and authentication
        if (url.username) proxiedUrl.username = url.username
        if (url.password) proxiedUrl.password = url.password
        url.searchParams.forEach((value, key) => {
          proxiedUrl.searchParams.set(key, value)
        })
        
        return proxiedUrl.toString()
      }
      
      // Return original URL if it's not a MediaMTX URL
      return mediaUrl
    } catch {
      // If URL parsing fails, return original
      return mediaUrl
    }
  }

  /**
   * Get the base WebRTC URL for this frontend instance
   */
  getWebRTCBaseUrl(): string {
    return MEDIAMTX_WEBRTC_URL.startsWith('/') 
      ? `${window.location.origin}${MEDIAMTX_WEBRTC_URL}`
      : MEDIAMTX_WEBRTC_URL
  }

  /**
   * Validate MediaMTX WebRTC URL format
   */
  validateStreamUrl(url: string): boolean {
    try {
      const parsed = new URL(url)
      
      // Check for valid protocol
      if (!['http:', 'https:'].includes(parsed.protocol)) {
        return false
      }
      
      // Check if path ends with /whep or can be a valid stream path
      const path = parsed.pathname
      if (!path.endsWith('/whep') && !path.match(/^\/[\w\-\/]+$/)) {
        return false
      }
      
      return true
    } catch {
      return false
    }
  }
}

// Export singleton instance
export const mediaMTXService = MediaMTXService.getInstance()

// Re-export types for convenience
export type { MediaMTXReaderConfig } from '@/mediamtx/MediaMTXWebRTCReader'