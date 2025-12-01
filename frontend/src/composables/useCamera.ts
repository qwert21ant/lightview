import { inject } from 'vue';
import type { CameraManager } from '@/services/cameraManager';

// Injection key for CameraManager
export const CAMERA_MANAGER_KEY = Symbol('cameraManager') as symbol;

/**
 * Composable for injecting the CameraManager instance
 */
export function useCameraManager(): CameraManager {
  const cameraManager = inject<CameraManager>(CAMERA_MANAGER_KEY);
  
  if (!cameraManager) {
    throw new Error('CameraManager not provided! Make sure to provide it in the app root.');
  }
  
  return cameraManager;
}