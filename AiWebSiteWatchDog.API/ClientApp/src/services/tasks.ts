import api from '../api'
import { WatchTaskFull } from '../types'

export type CreateTaskPayload = {
  title: string
  url: string
  taskPrompt: string
  schedule?: string
  enabled: boolean
}

export type UpdateTaskPayload = Partial<{
  title: string
  url: string
  taskPrompt: string
  schedule: string
  enabled: boolean
}>

export async function getTasks(): Promise<WatchTaskFull[]> {
  const r = await api.get<WatchTaskFull[]>('/tasks')
  return r.data
}

export async function createTask(payload: CreateTaskPayload): Promise<void> {
  await api.post('/tasks', payload)
}

export async function updateTask(id: number, payload: UpdateTaskPayload): Promise<void> {
  await api.put(`/tasks/${id}`, payload)
}

export async function runTask(id: number, opts: { sendEmail?: boolean } = { sendEmail: true }): Promise<void> {
  const params = { sendEmail: opts.sendEmail ?? true }
  await api.post(`/tasks/${id}/run`, null, { params })
}

export async function deleteTask(id: number): Promise<void> {
  await api.delete(`/tasks/${id}`)
}
