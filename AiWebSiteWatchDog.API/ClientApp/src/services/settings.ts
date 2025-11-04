import api from '../api'
import { Settings, SettingsForm } from '../types'

export async function getSettings(): Promise<Settings> {
  const r = await api.get<Settings>('/settings')
  return r.data
}

export async function updateSettings(payload: SettingsForm): Promise<void> {
  await api.put('/settings', payload)
}
