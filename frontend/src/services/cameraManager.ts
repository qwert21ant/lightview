import { ref, reactive } from 'vue';
import { BaseSignalRService } from './baseSignalR';
import type { 
  Camera, 
  AddCameraRequest, 
  PtzMoveRequest, 
  PtzMoveResponse, 
  CameraStatusResponse, 
  StreamUrlResponse,
  CameraEventHandlers,
  CameraChangedNotification,
  CameraStatusChangedData,
  CameraMetadataUpdatedData,
  CameraEventType
} from '@/types/camera';
import { 
  CameraStatus,
  CameraEventTypes,
  isCameraStatusChangedData,
  isCameraMetadataUpdatedData
} from '@/types/camera';

export class CameraManager extends BaseSignalRService {
  protected hubPath = '/cameraHub';

  // Camera-specific reactive state
  public cameras = ref<Camera[]>([]);

  // Event handlers
  private eventHandlers = reactive<CameraEventHandlers>({});

  // Override base class methods
  protected async onConnected(): Promise<void> {
    // Load initial cameras after connection
    await this.getAllCameras();
  }

  protected onReconnected(): void {
    // Refresh cameras after reconnection
    this.getAllCameras();
  }

  protected setupEventHandlers(): void {
    // Camera CRUD events
    this.on('CameraAdded', (camera: Camera) => {
      this.cameras.value.push(camera);
      this.eventHandlers.onCameraAdded?.(camera);
    });

    this.on('CameraUpdated', (camera: Camera) => {
      const index = this.cameras.value.findIndex(c => c.id === camera.id);
      if (index !== -1) {
        this.cameras.value[index] = camera;
      }
      this.eventHandlers.onCameraUpdated?.(camera);
    });

    this.on('CameraDeleted', (cameraId: string) => {
      const index = this.cameras.value.findIndex(c => c.id === cameraId);
      if (index !== -1) {
        this.cameras.value.splice(index, 1);
      }
      this.eventHandlers.onCameraDeleted?.(cameraId);
    });

    // Camera connection events
    this.on('CameraConnected', (cameraId: string) => {
      const camera = this.cameras.value.find(c => c.id === cameraId);
      if (camera) {
        camera.status = CameraStatus.Online;
        camera.lastConnectedAt = new Date().toISOString();
      }
      this.eventHandlers.onCameraConnected?.(cameraId);
    });

    this.on('CameraDisconnected', (cameraId: string) => {
      const camera = this.cameras.value.find(c => c.id === cameraId);
      if (camera) {
        camera.status = CameraStatus.Offline;
      }
      this.eventHandlers.onCameraDisconnected?.(cameraId);
    });

    // Handle unified CameraChanged events from core service
    this.on('CameraChanged', (notification: CameraChangedNotification) => {
      this.handleCameraChangedNotification(notification);
    });

    // PTZ events
    this.on('PtzMoved', (cameraId: string, moveType: string, response: PtzMoveResponse) => {
      this.eventHandlers.onPtzMoved?.(cameraId, moveType, response);
    });

    this.on('PtzStopped', (cameraId: string) => {
      this.eventHandlers.onPtzStopped?.(cameraId);
    });

    // Generic camera events
    this.on('CameraEvent', (cameraId: string, eventType: string, data: any) => {
      this.eventHandlers.onCameraEvent?.(cameraId, eventType, data);
    });
  }

  private handleCameraChangedNotification(notification: CameraChangedNotification): void {
    const camera = this.cameras.value.find(c => c.id === notification.cameraId);
    if (!camera) {
      console.warn(`Camera ${notification.cameraId} not found for event ${notification.eventType}`);
      return;
    }

    try {
      switch (notification.eventType) {
        case CameraEventTypes.StatusChanged:
          if (isCameraStatusChangedData(notification.data)) {
            this.handleStatusChanged(camera, notification.data);
          }
          break;

        case CameraEventTypes.Error:
          camera.status = CameraStatus.Error;
          this.eventHandlers.onCameraEvent?.(camera.id, 'Error', notification.data);
          break;

        case CameraEventTypes.MetadataUpdated:
          if (isCameraMetadataUpdatedData(notification.data)) {
            this.handleMetadataUpdated(camera, notification.data);
          }
          break;

        case CameraEventTypes.PtzMoved:
          this.eventHandlers.onCameraEvent?.(camera.id, 'PtzMoved', notification.data);
          break;

        case CameraEventTypes.Statistics:
          this.eventHandlers.onCameraEvent?.(camera.id, 'Statistics', notification.data);
          break;

        default:
          console.warn(`Unknown camera event type: ${notification.eventType}`);
          this.eventHandlers.onCameraEvent?.(camera.id, notification.eventType, notification.data);
          break;
      }
    } catch (error) {
      console.error(`Error handling camera event ${notification.eventType} for camera ${notification.cameraId}:`, error);
    }
  }

  private handleStatusChanged(camera: Camera, data: CameraStatusChangedData): void {
    const previousStatus = camera.status;
    camera.status = data.currentStatus;
    
    if (data.currentStatus === CameraStatus.Online && previousStatus !== CameraStatus.Online) {
      camera.lastConnectedAt = new Date().toISOString();
    }
    
    // Trigger appropriate event handlers based on status change
    if (data.currentStatus === CameraStatus.Online && previousStatus === CameraStatus.Connecting) {
      this.eventHandlers.onCameraConnected?.(camera.id);
    } else if (data.currentStatus === CameraStatus.Offline && previousStatus === CameraStatus.Online) {
      this.eventHandlers.onCameraDisconnected?.(camera.id);
    }
    
    this.eventHandlers.onCameraEvent?.(camera.id, 'StatusChanged', data);
  }

  private handleMetadataUpdated(camera: Camera, data: CameraMetadataUpdatedData): void {
    let updated = false;
    
    if (data.profiles) {
      camera.profiles = data.profiles;
      updated = true;
    }
    
    if (data.capabilities) {
      camera.capabilities = data.capabilities;
      updated = true;
    }
    
    if (data.deviceInfo) {
      camera.deviceInfo = data.deviceInfo;
      updated = true;
    }
    
    if (updated) {
      console.log(`Updated metadata for camera ${camera.id}:`, {
        profiles: data.profiles?.length || 0,
        hasCapabilities: !!data.capabilities,
        hasDeviceInfo: !!data.deviceInfo,
        updateType: data.updateType
      });
      
      this.eventHandlers.onCameraUpdated?.(camera);
      this.eventHandlers.onCameraEvent?.(camera.id, 'MetadataUpdated', data);
    }
  }

  // Event handler registration methods
  setEventHandlers(handlers: CameraEventHandlers): void {
    Object.assign(this.eventHandlers, handlers);
  }

  // Camera operations
  async getAllCameras(): Promise<Camera[]> {
    try {
      const cameras = await this.invoke<Camera[]>('GetAllCameras');
      this.cameras.value = cameras;
      return cameras;
    } catch (error) {
      console.error('Failed to get all cameras:', error);
      throw error;
    }
  }

  async getCamera(cameraId: string): Promise<Camera | null> {
    try {
      return await this.invoke<Camera | null>('GetCamera', cameraId);
    } catch (error) {
      console.error(`Failed to get camera ${cameraId}:`, error);
      throw error;
    }
  }

  async addCamera(request: AddCameraRequest): Promise<Camera> {
    try {
      return await this.invoke<Camera>('AddCamera', request);
    } catch (error) {
      console.error('Failed to add camera:', error);
      throw error;
    }
  }

  async updateCamera(cameraId: string, camera: Camera): Promise<Camera> {
    try {
      return await this.invoke<Camera>('UpdateCamera', cameraId, camera);
    } catch (error) {
      console.error(`Failed to update camera ${cameraId}:`, error);
      throw error;
    }
  }

  async deleteCamera(cameraId: string): Promise<boolean> {
    try {
      return await this.invoke<boolean>('DeleteCamera', cameraId);
    } catch (error) {
      console.error(`Failed to delete camera ${cameraId}:`, error);
      throw error;
    }
  }

  // Camera control operations
  async connectCamera(cameraId: string): Promise<boolean> {
    try {
      return await this.invoke<boolean>('ConnectCamera', cameraId);
    } catch (error) {
      console.error(`Failed to connect camera ${cameraId}:`, error);
      throw error;
    }
  }

  async disconnectCamera(cameraId: string): Promise<boolean> {
    try {
      return await this.invoke<boolean>('DisconnectCamera', cameraId);
    } catch (error) {
      console.error(`Failed to disconnect camera ${cameraId}:`, error);
      throw error;
    }
  }

  async getCameraStatus(cameraId: string): Promise<CameraStatusResponse | null> {
    try {
      return await this.invoke<CameraStatusResponse | null>('GetCameraStatus', cameraId);
    } catch (error) {
      console.error(`Failed to get camera status ${cameraId}:`, error);
      throw error;
    }
  }

  async getStreamUrl(cameraId: string, profileToken?: string): Promise<StreamUrlResponse | null> {
    try {
      return await this.invoke<StreamUrlResponse | null>('GetStreamUrl', cameraId, profileToken);
    } catch (error) {
      console.error(`Failed to get stream URL for camera ${cameraId}:`, error);
      throw error;
    }
  }

  // PTZ operations
  async movePtz(cameraId: string, request: PtzMoveRequest): Promise<PtzMoveResponse | null> {
    try {
      return await this.invoke<PtzMoveResponse | null>('MovePtz', cameraId, request);
    } catch (error) {
      console.error(`Failed to move PTZ for camera ${cameraId}:`, error);
      throw error;
    }
  }

  async stopPtz(cameraId: string): Promise<boolean> {
    try {
      return await this.invoke<boolean>('StopPtz', cameraId);
    } catch (error) {
      console.error(`Failed to stop PTZ for camera ${cameraId}:`, error);
      throw error;
    }
  }

  // Convenience PTZ methods
  async movePtzUp(cameraId: string, speed: number = 0.5): Promise<PtzMoveResponse | null> {
    try {
      return await this.invoke<PtzMoveResponse | null>('MovePtzUp', cameraId, speed);
    } catch (error) {
      console.error(`Failed to move PTZ up for camera ${cameraId}:`, error);
      throw error;
    }
  }

  async movePtzDown(cameraId: string, speed: number = 0.5): Promise<PtzMoveResponse | null> {
    try {
      return await this.invoke<PtzMoveResponse | null>('MovePtzDown', cameraId, speed);
    } catch (error) {
      console.error(`Failed to move PTZ down for camera ${cameraId}:`, error);
      throw error;
    }
  }

  async movePtzLeft(cameraId: string, speed: number = 0.5): Promise<PtzMoveResponse | null> {
    try {
      return await this.invoke<PtzMoveResponse | null>('MovePtzLeft', cameraId, speed);
    } catch (error) {
      console.error(`Failed to move PTZ left for camera ${cameraId}:`, error);
      throw error;
    }
  }

  async movePtzRight(cameraId: string, speed: number = 0.5): Promise<PtzMoveResponse | null> {
    try {
      return await this.invoke<PtzMoveResponse | null>('MovePtzRight', cameraId, speed);
    } catch (error) {
      console.error(`Failed to move PTZ right for camera ${cameraId}:`, error);
      throw error;
    }
  }

  async zoomIn(cameraId: string, speed: number = 0.5): Promise<PtzMoveResponse | null> {
    try {
      return await this.invoke<PtzMoveResponse | null>('ZoomIn', cameraId, speed);
    } catch (error) {
      console.error(`Failed to zoom in for camera ${cameraId}:`, error);
      throw error;
    }
  }

  async zoomOut(cameraId: string, speed: number = 0.5): Promise<PtzMoveResponse | null> {
    try {
      return await this.invoke<PtzMoveResponse | null>('ZoomOut', cameraId, speed);
    } catch (error) {
      console.error(`Failed to zoom out for camera ${cameraId}:`, error);
      throw error;
    }
  }

  // Group operations
  async joinCameraGroup(cameraId: string): Promise<void> {
    try {
      await this.invoke<void>('JoinCameraGroup', cameraId);
    } catch (error) {
      console.error(`Failed to join camera group ${cameraId}:`, error);
      throw error;
    }
  }

  async leaveCameraGroup(cameraId: string): Promise<void> {
    try {
      await this.invoke<void>('LeaveCameraGroup', cameraId);
    } catch (error) {
      console.error(`Failed to leave camera group ${cameraId}:`, error);
      throw error;
    }
  }
}