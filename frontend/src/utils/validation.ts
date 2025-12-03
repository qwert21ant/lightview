/**
 * Validation utilities for camera configuration
 */

import { CameraProtocol } from '@/types/camera'

export interface UrlValidationResult {
  isValid: boolean
  error?: string
}

/**
 * Validates a camera URL for proper format and protocol
 * @param url The URL to validate
 * @param protocol The camera protocol (optional, for additional validation)
 * @returns Validation result with error message if invalid
 */
export function validateCameraUrl(url: string, protocol?: CameraProtocol | ''): UrlValidationResult {
  if (!url.trim()) {
    return {
      isValid: false,
      error: 'Camera URL is required'
    }
  }

  const urlValue = url.trim()
  let isValidUrl = false
  
  try {
    const parsedUrl = new URL(urlValue)
    // Check for valid camera protocols
    if (['rtsp:', 'http:', 'https:'].includes(parsedUrl.protocol)) {
      isValidUrl = true
    } else {
      return {
        isValid: false,
        error: 'URL must use rtsp://, http://, or https:// protocol'
      }
    }
  } catch {
    // Check if it's an IP address with optional port
    const ipRegex = /^(\d{1,3}\.){3}\d{1,3}(:\d+)?(\/.*)?$/
    const ipWithProtocolRegex = /^(rtsp|http|https):\/\/(\d{1,3}\.){3}\d{1,3}(:\d+)?(\/.*)?$/i
    
    if (ipRegex.test(urlValue) || ipWithProtocolRegex.test(urlValue)) {
      // Validate IP address ranges
      const ipParts = urlValue.match(/(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})/)
      if (ipParts) {
        const validIp = ipParts.slice(1, 5).every(part => {
          const num = parseInt(part)
          return num >= 0 && num <= 255
        })
        
        if (validIp) {
          isValidUrl = true
        } else {
          return {
            isValid: false,
            error: 'Invalid IP address format'
          }
        }
      }
    } else {
      return {
        isValid: false,
        error: 'Please enter a valid camera URL (e.g., rtsp://192.168.1.100:554/stream)'
      }
    }
  }
  
  // Additional validation for common camera URL patterns
  if (isValidUrl && protocol === CameraProtocol.Rtsp) {
    if (!urlValue.toLowerCase().startsWith('rtsp://')) {
      return {
        isValid: false,
        error: 'RTSP cameras should use rtsp:// protocol'
      }
    }
  }

  return { isValid: true }
}

/**
 * Validates a camera name for proper format
 * @param name The camera name to validate
 * @returns Validation result with error message if invalid
 */
export function validateCameraName(name: string): UrlValidationResult {
  if (!name.trim()) {
    return {
      isValid: false,
      error: 'Camera name is required'
    }
  }

  return { isValid: true }
}

/**
 * Validates a camera protocol selection
 * @param protocol The protocol to validate
 * @returns Validation result with error message if invalid
 */
export function validateCameraProtocol(protocol: CameraProtocol | ''): UrlValidationResult {
  if (!protocol) {
    return {
      isValid: false,
      error: 'Camera protocol is required'
    }
  }

  return { isValid: true }
}