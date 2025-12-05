// Shared API configuration
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || (import.meta.env.PROD ? '' : 'http://localhost:5000');

// MediaMTX configuration
export const MEDIAMTX_WEBRTC_URL = import.meta.env.VITE_MEDIAMTX_WEBRTC_URL || (import.meta.env.PROD ? '/webrtc' : 'http://localhost:8889');
export const MEDIAMTX_API_URL = import.meta.env.VITE_MEDIAMTX_API_URL || (import.meta.env.PROD ? '/mediamtx-api' : 'http://localhost:9997/v3');

// Default API timeout
export const API_TIMEOUT = 10000;