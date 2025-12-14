import { ref, computed } from 'vue'
import { BaseSignalRService } from './baseSignalR'
import type { CameraMonitoringSettings } from '@/models/settings'

type SettingsUpdatedHandler = (s: CameraMonitoringSettings) => void

export class SettingsManager extends BaseSignalRService {
  protected hubPath = '/settingsHub'
  private handlers: Set<SettingsUpdatedHandler> = new Set()

  // Reactive state
  private _isInitialized = ref(false)
  public readonly isInitialized = computed(() => this._isInitialized.value)
  public readonly settings = ref<CameraMonitoringSettings>(null as any)

  // Override base class methods for lifecycle management
  protected async onConnected(): Promise<void> {
    try {
      console.log('SettingsHubService: Loading initial settings...')
      const data = await this.invoke<CameraMonitoringSettings>('GetCameraMonitoringSettings')
      this.settings.value = { ...data }
      this._isInitialized.value = true
      console.log('SettingsHubService: Initialized with settings')
    } catch (error) {
      console.error('SettingsHubService: Failed to load initial settings:', error)
      this._isInitialized.value = false
      throw error
    }
  }

  protected onReconnected(): void {
    console.log('SettingsHubService: Reconnected, refreshing settings...')
    this.invoke<CameraMonitoringSettings>('GetCameraMonitoringSettings')
      .then((data) => {
        this.settings.value = { ...data }
        this._isInitialized.value = true
        console.log('SettingsHubService: Refreshed settings after reconnection')
      })
      .catch((error) => {
        console.error('SettingsHubService: Failed to refresh settings after reconnection:', error)
        this._isInitialized.value = false
      })
  }

  protected onClosed(error?: Error): void {
    this._isInitialized.value = false
    console.log('SettingsHubService: Connection closed, clearing state', error ? `(${error.message})` : '')
  }

  protected setupEventHandlers(): void {
    this.on('SettingsUpdated', (payload: CameraMonitoringSettings) => {
      // Update local settings state
      this.settings.value = { ...payload }
      // Notify handlers
      this.handlers.forEach(h => h(payload))
    })
  }

  onSettingsUpdated(handler: SettingsUpdatedHandler): void {
    this.handlers.add(handler)
  }

  offSettingsUpdated(handler: SettingsUpdatedHandler): void {
    this.handlers.delete(handler)
  }

  async updateCameraMonitoringSettings(settings: CameraMonitoringSettings): Promise<void> {
    await this.invoke<void>('UpdateCameraMonitoringSettings', settings)
    // Update local state immediately (will also be updated via SettingsUpdated event)
    this.settings.value = { ...settings }
  }
}
