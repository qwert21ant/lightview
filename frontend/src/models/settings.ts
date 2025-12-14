export interface CameraMonitoringSettings {
  healthCheckInterval: number; // seconds
  healthCheckTimeout: number;  // seconds
  failureThreshold: number;
  successThreshold: number;
  snapshotInterval: number;    // seconds
}
