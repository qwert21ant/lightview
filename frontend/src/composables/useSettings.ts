import { inject, type InjectionKey } from 'vue'
import type { SettingsHubService } from '@/services/settingsHubService'

export const SETTINGS_HUB_KEY: InjectionKey<SettingsHubService> = Symbol('settingsHub')

export function useSettings(): SettingsHubService {
  const settingsHub = inject(SETTINGS_HUB_KEY)
  if (!settingsHub) {
    throw new Error('SettingsHubService not provided. Make sure it is provided in the root component.')
  }
  return settingsHub
}