import api from '../api'
import { Settings, SettingsForm, GmailStatus, GeminiStatus } from '../types'

export async function getSettings(): Promise<Settings> {
  const r = await api.get<Settings>('/settings')
  return r.data
}

export async function updateSettings(payload: SettingsForm): Promise<void> {
  const body: Partial<SettingsForm> = {
    userEmail: payload.userEmail ?? '',
    senderEmail: payload.senderEmail ?? '',
    senderName: payload.senderName ?? '',
    geminiApiUrl: payload.geminiApiUrl ?? '',
    notificationChannel: payload.notificationChannel ?? 'Email',
    telegramBotToken: payload.telegramBotToken ? payload.telegramBotToken : undefined,
    telegramChatId: payload.telegramChatId ?? null
  }
  Object.keys(body).forEach(key => {
    const k = key as keyof SettingsForm
    if (body[k] === undefined) delete body[k]
  })
  await api.put('/settings', body)
}

export async function getGmailStatus(): Promise<GmailStatus> {
  const r = await api.get('/auth/gmail-status')
  return r.data
}

export async function getGeminiStatus(): Promise<GeminiStatus> {
  const r = await api.get('/auth/gemini-status')
  return r.data
}
