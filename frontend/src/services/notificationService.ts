import { ref, reactive } from 'vue'

export interface Notification {
  id: string
  type: 'success' | 'error' | 'warning' | 'info'
  title: string
  message: string
  duration?: number
  timestamp: Date
}

export class NotificationService {
  private notifications = ref<Notification[]>([])
  private nextId = 1

  getNotifications() {
    return this.notifications
  }

  show(notification: Omit<Notification, 'id' | 'timestamp'>) {
    const id = `notification-${this.nextId++}`
    const newNotification: Notification = {
      ...notification,
      id,
      timestamp: new Date(),
      duration: notification.duration ?? this.getDefaultDuration(notification.type)
    }

    this.notifications.value.push(newNotification)

    // Auto-remove after duration
    if (newNotification.duration && newNotification.duration > 0) {
      setTimeout(() => {
        this.remove(id)
      }, newNotification.duration)
    }

    return id
  }

  success(title: string, message: string, duration?: number) {
    return this.show({ type: 'success', title, message, duration })
  }

  error(title: string, message: string, duration?: number) {
    return this.show({ type: 'error', title, message, duration })
  }

  warning(title: string, message: string, duration?: number) {
    return this.show({ type: 'warning', title, message, duration })
  }

  info(title: string, message: string, duration?: number) {
    return this.show({ type: 'info', title, message, duration })
  }

  remove(id: string) {
    const index = this.notifications.value.findIndex(n => n.id === id)
    if (index > -1) {
      this.notifications.value.splice(index, 1)
    }
  }

  clear() {
    this.notifications.value = []
  }

  private getDefaultDuration(type: Notification['type']): number {
    switch (type) {
      case 'success':
        return 4000
      case 'info':
        return 5000
      case 'warning':
        return 6000
      case 'error':
        return 0 // Don't auto-dismiss errors
      default:
        return 5000
    }
  }
}