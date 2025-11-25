import api from '../api'
import { Settings, SettingsForm, GmailStatus, GeminiStatus } from '../types'

export async function getSettings(): Promise<Settings> {
  const r = await api.get<Settings>('/settings')
  return r.data
}

export async function updateSettings(payload: SettingsForm): Promise<void> {
  await api.put('/settings', payload)
}

export async function getGmailStatus(): Promise<GmailStatus> {
  const r = await api.get('/auth/gmail-status')
  return r.data
}

export async function getGeminiStatus(): Promise<GeminiStatus> {
  const r = await api.get('/auth/gemini-status')
  return r.data
}
