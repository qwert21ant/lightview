import { inject, type InjectionKey } from 'vue'
import type { SettingsManager } from '@/services/settingsManager'

export const SETTINGS_HUB_KEY: InjectionKey<SettingsManager> = Symbol('settingsHub')

export function useSettings(): SettingsManager {
  const settingsHub = inject(SETTINGS_HUB_KEY)
  if (!settingsHub) {
    throw new Error('SettingsHubService not provided. Make sure it is provided in the root component.')
  }
  return settingsHub
}