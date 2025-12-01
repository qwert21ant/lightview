// Shared API configuration
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || (import.meta.env.PROD ? '' : 'http://localhost:5000');

// Default API timeout
export const API_TIMEOUT = 10000;