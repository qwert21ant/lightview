import { inject, type InjectionKey } from 'vue'
import { NotificationService } from '@/services/notificationService'

export const NOTIFICATION_SERVICE_KEY: InjectionKey<NotificationService> = Symbol('NotificationService')

export function useNotifications() {
  const notificationService = inject(NOTIFICATION_SERVICE_KEY)
  
  if (!notificationService) {
    throw new Error('NotificationService not provided. Make sure to provide it in the app root.')
  }

  return notificationService;
}