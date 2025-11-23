export type WatchTask = {
  id: number
  title: string
  url: string
  schedule?: string
  enabled: boolean
}

export type WatchTaskFull = {
  id: number
  title: string
  url: string
  taskPrompt: string
  schedule: string
  lastChecked: string | null
  lastResult?: string | null
  enabled: boolean
}

export type NotificationItem = {
  id: number
  subject: string
  message: string
  sentAt: string
}

export type NotificationChannel = 'Email' | 'Telegram'

export type Settings = {
  userEmail: string
  senderEmail: string
  senderName: string
  geminiApiUrl: string
  notificationChannel: NotificationChannel
  telegramBotToken?: string | null
  telegramChatId?: string | null
  watchTasks: WatchTask[]
}

export type SettingsForm = Pick<Settings, 'userEmail' | 'senderEmail' | 'senderName' | 'geminiApiUrl' | 'notificationChannel' | 'telegramBotToken' | 'telegramChatId'>

export type NewTaskForm = {
  title: string
  url: string
  taskPrompt: string
  schedule: string
  enabled: boolean
}

export type EditTaskForm = Partial<Pick<WatchTaskFull, 'title' | 'url' | 'taskPrompt' | 'schedule' | 'enabled'>>

export type GmailStatus = {
  configured: boolean
  channel: NotificationChannel | null
  hasGmailScope: boolean
  needsReauth: boolean
}

export type GeminiStatus = {
  configured: boolean
  hasGeminiScope: boolean
  needsReauth: boolean
}
