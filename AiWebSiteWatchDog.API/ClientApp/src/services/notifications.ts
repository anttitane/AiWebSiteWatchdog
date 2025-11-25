import api from '../api'
import { NotificationItem } from '../types'

export async function getNotifications(): Promise<NotificationItem[]> {
  const r = await api.get<NotificationItem[]>('/notifications')
  return r.data
}

export async function deleteNotification(id: number): Promise<void> {
  await api.delete(`/notifications/${id}`)
}

export async function sendTestNotification(payload: { subject: string; message: string }): Promise<void> {
  await api.post('/notifications', payload)
}
