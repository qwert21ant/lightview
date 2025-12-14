import { ref, reactive, computed } from 'vue';
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

  // Event handlers
  private eventHandlers = reactive<CameraEventHandlers>({});
  
  // Track initialization state
  private _isInitialized = ref(false);
  
  // Camera-specific reactive state
  public cameras = ref<Camera[]>([]);
  
  // Snapshot state - reactive map of camera ID to snapshot data
  private _snapshots = ref<Map<string, {
    cameraId: string;
    imageData: string;
    capturedAt: string;
    profileToken?: string;
    fileSize: number;
  }>>(new Map());
  
  private _snapshotLoadingStates = ref<Map<string, boolean>>(new Map());

  // Reactive computed properties for easier component access
  public readonly isInitialized = computed(() => this._isInitialized.value);

  // Override base class methods
  protected async onConnected(): Promise<void> {
    try {
      // Load initial cameras after connection
      console.log('CameraManager: Loading initial cameras...');
      await this.getAllCameras();
      
      // Load snapshots for online cameras
      const onlineCameras = this.cameras.value.filter(c => c.status === CameraStatus.Online);
      await Promise.all(onlineCameras.map(camera => this.loadSnapshotForCamera(camera.id)));
      
      this._isInitialized.value = true;
      console.log(`CameraManager: Initialized with ${this.cameras.value.length} cameras, loaded ${onlineCameras.length} snapshots`);
    } catch (error) {
      console.error('CameraManager: Failed to load initial cameras:', error);
      this._isInitialized.value = false;
      throw error;
    }
  }

  protected onReconnected(): void {
    // Refresh cameras after reconnection
    console.log('CameraManager: Reconnected, refreshing cameras...');
    this.getAllCameras().then(() => {
      this._isInitialized.value = true;
      console.log(`CameraManager: Refreshed with ${this.cameras.value.length} cameras`);
    }).catch(error => {
      console.error('CameraManager: Failed to refresh cameras after reconnection:', error);
      this._isInitialized.value = false;
    });
  }
  
  protected onClosed(error?: Error): void {
    // Mark as not initialized when disconnected
    this._isInitialized.value = false;
    console.log('CameraManager: Connection closed, clearing state', error ? `(${error.message})` : '');
    
    // Call parent implementation
    super.onClosed?.(error);
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

    // Snapshot events
    this.on('CameraSnapshotCaptured', (eventData: {
      cameraId: string;
      timestamp: string;
      captureTime: number;
      profileToken?: string;
    }) => {
      // Automatically load the new snapshot
      this.loadSnapshotForCamera(eventData.cameraId);
      
      // Still call the event handler for any custom logic
      this.eventHandlers.onSnapshotCaptured?.(eventData.cameraId, eventData);
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
      // Load initial snapshot when camera comes online
      this.loadSnapshotForCamera(camera.id);
    } else if (data.currentStatus === CameraStatus.Offline && previousStatus === CameraStatus.Online) {
      // Clear snapshot when camera goes offline
      this.clearSnapshotForCamera(camera.id);
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
      this.cameras.value = cameras || [];
      console.log(`CameraManager: Loaded ${this.cameras.value.length} cameras`);
      return this.cameras.value;
    } catch (error) {
      console.error('Failed to get all cameras:', error);
      // Don't clear existing cameras on error, just re-throw
      throw error;
    }
  }
  
  // Get camera from local state (no hub call)
  getCameraById(cameraId: string): Camera | null {
    return this.cameras.value.find(c => c.id === cameraId) || null;
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

  // Snapshot operations
  async getLatestSnapshot(cameraId: string): Promise<{
    cameraId: string;
    imageData: string;
    capturedAt: string;
    profileToken?: string;
    fileSize: number;
  } | null> {
    try {
      return await this.invoke('GetLatestSnapshot', cameraId);
    } catch (error) {
      console.error('Failed to get latest snapshot:', error);
      return null;
    }
  }
  
  // Internal method to load and cache snapshot for a camera
  private async loadSnapshotForCamera(cameraId: string): Promise<void> {
    if (this._snapshotLoadingStates.value.get(cameraId)) {
      return; // Already loading
    }
    
    // Create new Map to trigger reactivity
    const newLoadingStates = new Map(this._snapshotLoadingStates.value);
    newLoadingStates.set(cameraId, true);
    this._snapshotLoadingStates.value = newLoadingStates;
    
    try {
      const snapshot = await this.getLatestSnapshot(cameraId);
      if (snapshot) {
        // Create new Map to trigger reactivity
        const newSnapshots = new Map(this._snapshots.value);
        newSnapshots.set(cameraId, snapshot);
        this._snapshots.value = newSnapshots;
        console.log(`Loaded snapshot for camera ${cameraId}: ${snapshot.fileSize} bytes`);
      }
    } catch (error) {
      console.error(`Failed to load snapshot for camera ${cameraId}:`, error);
    } finally {
      // Create new Map to trigger reactivity
      const newLoadingStates = new Map(this._snapshotLoadingStates.value);
      newLoadingStates.set(cameraId, false);
      this._snapshotLoadingStates.value = newLoadingStates;
    }
  }
  
  // Public methods for accessing snapshot state
  getSnapshotForCamera(cameraId: string) {
    return computed(() => this._snapshots.value.get(cameraId) || null);
  }
  
  isLoadingSnapshotForCamera(cameraId: string) {
    return computed(() => this._snapshotLoadingStates.value.get(cameraId) || false);
  }
  
  // Manually trigger snapshot load (e.g., on camera connect)
  async refreshSnapshotForCamera(cameraId: string): Promise<void> {
    await this.loadSnapshotForCamera(cameraId);
  }
  
  // Clear snapshot for a camera (e.g., on disconnect)
  clearSnapshotForCamera(cameraId: string): void {
    // Create new Maps to trigger reactivity
    const newSnapshots = new Map(this._snapshots.value);
    newSnapshots.delete(cameraId);
    this._snapshots.value = newSnapshots;
    
    const newLoadingStates = new Map(this._snapshotLoadingStates.value);
    newLoadingStates.delete(cameraId);
    this._snapshotLoadingStates.value = newLoadingStates;
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