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

export type Settings = {
  userEmail: string
  senderEmail: string
  senderName: string
  geminiApiUrl: string
  watchTasks: WatchTask[]
}

export type SettingsForm = Pick<Settings, 'userEmail' | 'senderEmail' | 'senderName' | 'geminiApiUrl'>

export type NewTaskForm = {
  title: string
  url: string
  taskPrompt: string
  schedule: string
  enabled: boolean
}

export type EditTaskForm = Partial<Pick<WatchTaskFull, 'title' | 'url' | 'taskPrompt' | 'schedule' | 'enabled'>>
