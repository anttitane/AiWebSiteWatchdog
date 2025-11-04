import { useEffect, useState } from 'react'
import axios from 'axios'
import { Settings, WatchTaskFull, NotificationItem, SettingsForm, NewTaskForm, EditTaskForm } from './types'
import SettingsModal from './components/modals/SettingsModal'
import CreateTaskModal from './components/modals/CreateTaskModal'
import EditTaskModal from './components/modals/EditTaskModal'
import AuthModal from './components/modals/AuthModal'
import NotificationDetailsModal from './components/modals/NotificationDetailsModal'
import SettingsSection from './components/sections/SettingsSection'
import TasksSection from './components/sections/TasksSection'
import NotificationsSection from './components/sections/NotificationsSection'
import { getSettings as svcGetSettings, updateSettings as svcUpdateSettings } from './services/settings'
import { getTasks as svcGetTasks, createTask as svcCreateTask, updateTask as svcUpdateTask, runTask as svcRunTask, deleteTask as svcDeleteTask } from './services/tasks'
import { getNotifications as svcGetNotifications, deleteNotification as svcDeleteNotification } from './services/notifications'

export default function App() {
  const [settings, setSettings] = useState<Settings | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loaded, setLoaded] = useState(false)
  const [tasks, setTasks] = useState<WatchTaskFull[] | null>(null)
  const [tasksError, setTasksError] = useState<string | null>(null)
  const [notifications, setNotifications] = useState<NotificationItem[] | null>(null)
  const [notificationsError, setNotificationsError] = useState<string | null>(null)
  const [showNotificationModal, setShowNotificationModal] = useState(false)
  const [currentNotification, setCurrentNotification] = useState<NotificationItem | null>(null)
  const [notifDeleteMsg, setNotifDeleteMsg] = useState<string | null>(null)

  // Editable form state (Settings)
  const [form, setForm] = useState<SettingsForm>({
    userEmail: '',
    senderEmail: '',
    senderName: '',
    geminiApiUrl: ''
  })
  const [saving, setSaving] = useState(false)
  const [saveMsg, setSaveMsg] = useState<string | null>(null)
  const [showSettingsModal, setShowSettingsModal] = useState(false)
  const [showAuthModal, setShowAuthModal] = useState(false)
  const [authEmail, setAuthEmail] = useState('')

  // Create task form state
  const [newTask, setNewTask] = useState<NewTaskForm>({
    title: '',
    url: '',
    taskPrompt: '',
    schedule: '',
    enabled: true
  })
  const [creating, setCreating] = useState(false)
  const [createMsg, setCreateMsg] = useState<string | null>(null)
  const [showCreateTaskModal, setShowCreateTaskModal] = useState(false)

  // Edit task state: track which task is being edited and its form values
  const [editId, setEditId] = useState<number | null>(null)
  const [editTask, setEditTask] = useState<EditTaskForm>({})
  const [updating, setUpdating] = useState(false)
  const [updateMsg, setUpdateMsg] = useState<string | null>(null)
  const [showEditTaskModal, setShowEditTaskModal] = useState(false)
  const [runningId, setRunningId] = useState<number | null>(null)
  const [deletingId, setDeletingId] = useState<number | null>(null)
  const [runMsg, setRunMsg] = useState<string | null>(null)
  const [deleteMsg, setDeleteMsg] = useState<string | null>(null)

  useEffect(() => {
    svcGetSettings()
      .then(data => {
        setSettings(data)
        if (data) {
          setForm({
            userEmail: data.userEmail || '',
            senderEmail: data.senderEmail || '',
            senderName: data.senderName || '',
            geminiApiUrl: data.geminiApiUrl || ''
          })
        }
        setLoaded(true)
      })
      .catch((err: unknown) => {
        const ax = err as any
        if (axios.isAxiosError(ax) && ax.response?.status === 404) {
          setSettings(null)
          setLoaded(true)
          return
        }
        setError((err as Error).message || String(err))
        setLoaded(true)
      })
  }, [])

  useEffect(() => {
    svcGetTasks()
      .then((data) => setTasks(data))
      .catch((e: unknown) => setTasksError((e as Error).message || String(e)))
  }, [])

  useEffect(() => {
    loadNotifications()
  }, [])

  async function loadNotifications() {
    try {
      const list = await svcGetNotifications()
      const data = (list || []).slice().sort((a, b) => new Date(b.sentAt).getTime() - new Date(a.sentAt).getTime())
      setNotifications(data)
    } catch (e: unknown) {
      setNotificationsError((e as Error).message || String(e))
    }
  }

  async function saveSettings(): Promise<boolean> {
    setSaving(true)
    setSaveMsg(null)
    setError(null)
    try {
      const payload = {
        userEmail: form.userEmail,
        senderEmail: form.senderEmail,
        senderName: form.senderName,
        geminiApiUrl: form.geminiApiUrl
      }
      await svcUpdateSettings(payload)
      // Refresh settings from server
      const latest: Settings = await svcGetSettings()
      setSettings(latest)
      setForm({
        userEmail: latest.userEmail || '',
        senderEmail: latest.senderEmail || '',
        senderName: latest.senderName || '',
        geminiApiUrl: latest.geminiApiUrl || ''
      })
      setSaveMsg('Settings saved')
      return true
    } catch (e) {
      setError(String(e))
      return false
    } finally {
      setSaving(false)
      // Auto-clear success message after a moment
      setTimeout(() => setSaveMsg(null), 2500)
    }
  }

  async function createTask(): Promise<boolean> {
    setCreating(true)
    setCreateMsg(null)
    setError(null)
    try {
      const payload = {
        title: newTask.title.trim(),
        url: newTask.url.trim(),
        taskPrompt: newTask.taskPrompt,
        schedule: newTask.schedule.trim() || undefined,
        enabled: newTask.enabled
      }
      await svcCreateTask(payload)
      // Refresh tasks and settings summaries
      const [t, s] = await Promise.all([
        svcGetTasks(),
        svcGetSettings().catch(() => null as any)
      ])
      setTasks(t)
      setSettings(s as Settings | null)
      setNewTask({ title: '', url: '', taskPrompt: '', schedule: '', enabled: true })
      setCreateMsg('Task created')
      return true
    } catch (e: unknown) {
      const ax = e as any
      if (axios.isAxiosError(ax) && ax.response) {
        setError(`Create failed: HTTP ${ax.response.status} ${JSON.stringify(ax.response.data)}`)
      } else {
        setError((e as Error).message || String(e))
      }
      return false
    } finally {
      setCreating(false)
      setTimeout(() => setCreateMsg(null), 2500)
    }
  }

  function startEdit(t: WatchTaskFull) {
    setEditId(t.id)
    setEditTask({
      title: t.title,
      url: t.url,
      taskPrompt: t.taskPrompt,
      schedule: t.schedule,
      enabled: t.enabled
    })
    setUpdateMsg(null)
    setShowEditTaskModal(true)
  }

  function cancelEdit() {
    setEditId(null)
    setEditTask({})
    setShowEditTaskModal(false)
  }

  async function saveEdit(id: number): Promise<boolean> {
    setUpdating(true)
    setUpdateMsg(null)
    setError(null)
    try {
      const payload: any = {}
      if (editTask.title !== undefined) payload.title = editTask.title
      if (editTask.url !== undefined) payload.url = editTask.url
      if (editTask.taskPrompt !== undefined) payload.taskPrompt = editTask.taskPrompt
      if (editTask.schedule !== undefined) payload.schedule = editTask.schedule
      if (editTask.enabled !== undefined) payload.enabled = editTask.enabled

      await svcUpdateTask(id, payload)

      // Update tasks list and settings summaries
      const [t, s] = await Promise.all([
        svcGetTasks(),
        svcGetSettings().catch(() => null as any)
      ])
      setTasks(t)
      setSettings(s as Settings | null)
      setUpdateMsg('Task updated')
      setEditId(null)
      setEditTask({})
      return true
    } catch (e: unknown) {
      const ax = e as any
      if (axios.isAxiosError(ax) && ax.response) {
        setError(`Update failed: HTTP ${ax.response.status} ${JSON.stringify(ax.response.data)}`)
      } else {
        setError((e as Error).message || String(e))
      }
      return false
    } finally {
      setUpdating(false)
      setTimeout(() => setUpdateMsg(null), 2500)
    }
  }

  async function runTask(id: number) {
    setRunningId(id)
    setRunMsg(null)
    setError(null)
    try {
      await svcRunTask(id, { sendEmail: true })
      // Refresh task list to get lastChecked/lastResult updates
      const t = await svcGetTasks()
      setTasks(t)
      setRunMsg(`Task #${id} triggered and email sent`)
    } catch (e: unknown) {
      const ax = e as any
      if (axios.isAxiosError(ax) && ax.response) {
        setError(`Run failed: HTTP ${ax.response.status} ${JSON.stringify(ax.response.data)}`)
      } else {
        setError((e as Error).message || String(e))
      }
    } finally {
      setRunningId(null)
      setTimeout(() => setRunMsg(null), 2500)
    }
  }

  async function deleteTask(id: number) {
    if (!confirm(`Delete task #${id}? This cannot be undone.`)) return
    setDeletingId(id)
    setDeleteMsg(null)
    setError(null)
    try {
      await svcDeleteTask(id)
      const [t, s] = await Promise.all([
        svcGetTasks(),
        svcGetSettings().catch(() => null as any)
      ])
      setTasks(t)
      setSettings(s as Settings | null)
      setDeleteMsg(`Task #${id} deleted`)
    } catch (e: unknown) {
      const ax = e as any
      if (axios.isAxiosError(ax) && ax.response) {
        setError(`Delete failed: HTTP ${ax.response.status} ${JSON.stringify(ax.response.data)}`)
      } else {
        setError((e as Error).message || String(e))
      }
    } finally {
      setDeletingId(null)
      setTimeout(() => setDeleteMsg(null), 2500)
    }
  }

  function showNotification(n: NotificationItem) {
    setCurrentNotification(n)
    setShowNotificationModal(true)
  }

  async function deleteNotification(id: number) {
    if (!confirm(`Delete notification #${id}?`)) return
    try {
      await svcDeleteNotification(id)
      await loadNotifications()
      setNotifDeleteMsg(`Notification #${id} deleted`)
      setTimeout(() => setNotifDeleteMsg(null), 2500)
    } catch (e: unknown) {
      const ax = e as any
      if (axios.isAxiosError(ax) && ax.response) {
        setNotificationsError(`Delete failed: HTTP ${ax.response.status} ${JSON.stringify(ax.response.data)}`)
      } else {
        setNotificationsError((e as Error).message || String(e))
      }
    }
  }

  return (
    <div style={{ fontFamily: 'system-ui, sans-serif', margin: '2rem' }}>
      <h1>AiWebSiteWatchdog</h1>

      {error && <p style={{ color: 'crimson' }}>{error}</p>}
      {saveMsg && <p style={{ color: 'seagreen' }}>{saveMsg}</p>}
      {runMsg && <p style={{ color: 'seagreen' }}>{runMsg}</p>}
      {deleteMsg && <p style={{ color: 'seagreen' }}>{deleteMsg}</p>}

      {/* Settings edit moved to modal, controlled by showSettingsModal */}
      {!loaded ? (
        <p>Loading…</p>
      ) : null}

      <SettingsSection
        settings={settings}
        loaded={loaded}
        onEditSettings={() => setShowSettingsModal(true)}
        onAuthorizeGoogle={() => { const email = settings?.senderEmail || ''; setAuthEmail(email); setShowAuthModal(true); }}
      />

      {settings ? (
        <>
          <TasksSection
            tasks={tasks}
            error={tasksError}
            onNewTask={() => setShowCreateTaskModal(true)}
            onEditTask={startEdit}
            onRunTask={runTask}
            onDeleteTask={deleteTask}
            runningId={runningId}
            deletingId={deletingId}
            createMsg={createMsg}
          />

          <NotificationsSection
            notifications={notifications}
            error={notificationsError}
            onShow={showNotification}
            onDelete={deleteNotification}
            deleteMsg={notifDeleteMsg}
          />
        </>
      ) : null}

      <SettingsModal
        open={!!showSettingsModal}
        form={form}
        setForm={setForm}
        saving={saving}
        onSave={saveSettings}
        onClose={() => setShowSettingsModal(false)}
      />

      <CreateTaskModal
        open={!!showCreateTaskModal}
        newTask={newTask}
        setNewTask={setNewTask}
        creating={creating}
        onCreate={createTask}
        onClose={() => setShowCreateTaskModal(false)}
      />

      <EditTaskModal
        open={!!showEditTaskModal}
        editId={editId}
        editTask={editTask}
        setEditTask={setEditTask}
        updating={updating}
        updateMsg={updateMsg}
        onSave={saveEdit}
        onCancel={cancelEdit}
      />

      <AuthModal
        open={!!showAuthModal}
        email={authEmail}
        setEmail={setAuthEmail}
        onClose={() => setShowAuthModal(false)}
      />

      <NotificationDetailsModal
        open={!!showNotificationModal}
        notification={currentNotification}
        onClose={() => setShowNotificationModal(false)}
      />
    </div>
  )
}
