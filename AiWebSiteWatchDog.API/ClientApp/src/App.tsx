import { useEffect, useState } from 'react'
import { useLocalStorageString } from './hooks/useLocalStorage'
import { toast } from 'react-hot-toast'
import ConfirmDialog from './components/modals/ConfirmDialog'
import { useRef } from 'react'
import axios from 'axios'
import { Settings, WatchTaskFull, NotificationItem, SettingsForm, NewTaskForm, EditTaskForm } from './types'
import CreateTaskModal from './components/modals/CreateTaskModal'
import EditTaskModal from './components/modals/EditTaskModal'
// Removed AuthModal (replaced by inline AuthCard)
import AuthCard from './components/sections/AuthCard'
import SettingsSection from './components/sections/SettingsSection'
import TasksSection from './components/sections/TasksSection'
import NotificationsSection from './components/sections/NotificationsSection'
import DashboardSection from './components/sections/DashboardSection'
import { getSettings as svcGetSettings, updateSettings as svcUpdateSettings } from './services/settings'
import { getTasks as svcGetTasks, createTask as svcCreateTask, updateTask as svcUpdateTask, runTask as svcRunTask, deleteTask as svcDeleteTask } from './services/tasks'
import { getNotifications as svcGetNotifications, deleteNotification as svcDeleteNotification } from './services/notifications'

export default function App() {
  const [activeTab, setActiveTab] = useLocalStorageString<'dashboard' | 'tasks' | 'notifications' | 'settings'>('ui.activeTab', 'dashboard')
  const [theme, setTheme] = useLocalStorageString<'light' | 'dark'>('ui.theme', 'light')
  const [settings, setSettings] = useState<Settings | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loaded, setLoaded] = useState(false)
  const [tasks, setTasks] = useState<WatchTaskFull[] | null>(null)
  const [tasksError, setTasksError] = useState<string | null>(null)
  const [notifications, setNotifications] = useState<NotificationItem[] | null>(null)
  const [notificationsError, setNotificationsError] = useState<string | null>(null)
  const [notifDeleteMsg, setNotifDeleteMsg] = useState<string | null>(null)
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const mobileMenuButtonRef = useRef<HTMLButtonElement | null>(null)
  const mobileNavRootRef = useRef<HTMLDivElement | null>(null)
  const mobileNavDrawerRef = useRef<HTMLElement | null>(null)
  const mobileNavCloseBtnRef = useRef<HTMLButtonElement | null>(null)

  // Editable form state (Settings)
  const [form, setForm] = useState<SettingsForm>({
    userEmail: '',
    senderEmail: '',
    senderName: '',
    geminiApiUrl: ''
  })
  const [saving, setSaving] = useState(false)
  const [saveMsg, setSaveMsg] = useState<string | null>(null)
  // Auth now inline via AuthCard

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

  // Confirm dialog state
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [confirmTitle, setConfirmTitle] = useState<string>('Are you sure?')
  const [confirmMessage, setConfirmMessage] = useState<string>('This action cannot be undone.')
  const [confirmConfirmText, setConfirmConfirmText] = useState<string>('Confirm')
  const [confirmCancelText, setConfirmCancelText] = useState<string>('Cancel')
  const confirmResolver = useRef<((v: boolean) => void) | null>(null)

  function openConfirm(opts: { title?: string; message?: string; confirmText?: string; cancelText?: string }): Promise<boolean> {
    setConfirmTitle(opts.title ?? 'Are you sure?')
    setConfirmMessage(opts.message ?? 'This action cannot be undone.')
    setConfirmConfirmText(opts.confirmText ?? 'Confirm')
    setConfirmCancelText(opts.cancelText ?? 'Cancel')
    setConfirmOpen(true)
    return new Promise<boolean>((resolve) => {
      confirmResolver.current = resolve
    })
  }
  function handleConfirm(result: boolean) {
    setConfirmOpen(false)
    const r = confirmResolver.current
    confirmResolver.current = null
    if (r) r(result)
  }

  // Initialize theme from system if not set, and apply to <html>
  useEffect(() => {
    try {
      const stored = localStorage.getItem('ui.theme')
      if (!stored) {
        const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
        setTheme(prefersDark ? 'dark' : 'light')
      }
    } catch {}
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    const root = document.documentElement
    if (theme === 'dark') root.classList.add('dark')
    else root.classList.remove('dark')
  }, [theme])

  // Close mobile sidebar on Escape
  useEffect(() => {
    if (!mobileNavOpen) return
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') setMobileNavOpen(false)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [mobileNavOpen])

  // (Removed) inert handling since we unmount the drawer when closed

  // Focus trap within the mobile drawer when open
  useEffect(() => {
    if (!mobileNavOpen) return
    const drawer = mobileNavDrawerRef.current
    if (!drawer) return

    const focusSelector = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    const getFocusable = () =>
      Array.from(drawer.querySelectorAll<HTMLElement>(focusSelector)).filter(
        el => !el.hasAttribute('disabled') && el.tabIndex !== -1
      )

    // Set initial focus to close button or first focusable
    const initial = mobileNavCloseBtnRef.current || getFocusable()[0]
    initial?.focus()

    function onKeyDown(e: KeyboardEvent) {
      if (e.key !== 'Tab') return
      // Re-guard in case ref changed
      const host = mobileNavDrawerRef.current
      if (!host) return
      const focusables = getFocusable()
      if (!focusables.length) return
      const first = focusables[0]
      const last = focusables[focusables.length - 1]
      const active = document.activeElement as HTMLElement | null
      const isInDrawer = !!(active && host.contains(active))

      if (e.shiftKey) {
        if (!isInDrawer || active === first) {
          e.preventDefault()
          last.focus()
        }
      } else {
        if (!isInDrawer || active === last) {
          e.preventDefault()
          first.focus()
        }
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [mobileNavOpen])

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
      .catch((e: unknown) => {
        const msg = (e as Error).message || String(e)
        setTasksError(msg)
        toast.error(`Failed to load tasks: ${msg}`)
      })
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
      const msg = (e as Error).message || String(e)
      setNotificationsError(msg)
      toast.error(`Failed to load notifications: ${msg}`)
    }
  }

  async function saveSettings(): Promise<boolean> {
    setSaving(true)
    setSaveMsg(null)
    setError(null)
    const toastId = toast.loading('Saving settings…')
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
      toast.success('Settings saved', { id: toastId })
      return true
    } catch (e) {
      const msg = String(e)
      setError(msg)
      toast.error(msg)
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
    const toastId = toast.loading('Creating task…')
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
      const [tasksList, s] = await Promise.all([
        svcGetTasks(),
        svcGetSettings().catch(() => null as any)
      ])
      setTasks(tasksList)
      setSettings(s as Settings | null)
      setNewTask({ title: '', url: '', taskPrompt: '', schedule: '', enabled: true })
      setCreateMsg('Task created')
      toast.success('Task created', { id: toastId })
      return true
    } catch (e: unknown) {
      const ax = e as any
      if (axios.isAxiosError(ax) && ax.response) {
        const msg = `Create failed: HTTP ${ax.response.status} ${JSON.stringify(ax.response.data)}`
        setError(msg)
        toast.error(msg, { id: toastId })
      } else {
        const msg = (e as Error).message || String(e)
        setError(msg)
        toast.error(msg, { id: toastId })
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
    const toastId = toast.loading('Updating task…')
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
      toast.success('Task updated', { id: toastId })
      setEditId(null)
      setEditTask({})
      return true
    } catch (e: unknown) {
      const ax = e as any
      if (axios.isAxiosError(ax) && ax.response) {
        const msg = `Update failed: HTTP ${ax.response.status} ${JSON.stringify(ax.response.data)}`
        setError(msg)
        toast.error(msg, { id: toastId })
      } else {
        const msg = (e as Error).message || String(e)
        setError(msg)
        toast.error(msg, { id: toastId })
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
    const toastId = toast.loading('Running task…')
    try {
      await svcRunTask(id, { sendEmail: true })
      // Refresh task list to get lastChecked/lastResult updates
      const tasksList = await svcGetTasks()
      setTasks(tasksList)
      setRunMsg(`Task #${id} triggered and email sent`)
      toast.success(`Task #${id} triggered`, { id: toastId })
    } catch (e: unknown) {
      const ax = e as any
      if (axios.isAxiosError(ax) && ax.response) {
        const msg = `Run failed: HTTP ${ax.response.status} ${JSON.stringify(ax.response.data)}`
        setError(msg)
        toast.error(msg, { id: toastId })
      } else {
        const msg = (e as Error).message || String(e)
        setError(msg)
        toast.error(msg, { id: toastId })
      }
    } finally {
      setRunningId(null)
      setTimeout(() => setRunMsg(null), 2500)
    }
  }

  async function deleteTask(id: number) {
    const ok = await openConfirm({
      title: `Delete task #${id}?`,
      message: 'This cannot be undone.',
      confirmText: 'Delete',
      cancelText: 'Cancel'
    })
    if (!ok) return
    setDeletingId(id)
    setDeleteMsg(null)
    setError(null)
    const toastId = toast.loading('Deleting task…')
    try {
      await svcDeleteTask(id)
      const [tasksList, s] = await Promise.all([
        svcGetTasks(),
        svcGetSettings().catch(() => null as any)
      ])
      setTasks(tasksList)
      setSettings(s as Settings | null)
      setDeleteMsg(`Task #${id} deleted`)
      toast.success(`Task #${id} deleted`, { id: toastId })
    } catch (e: unknown) {
      const ax = e as any
      if (axios.isAxiosError(ax) && ax.response) {
        const msg = `Delete failed: HTTP ${ax.response.status} ${JSON.stringify(ax.response.data)}`
        setError(msg)
        toast.error(msg, { id: toastId })
      } else {
        const msg = (e as Error).message || String(e)
        setError(msg)
        toast.error(msg, { id: toastId })
      }
    } finally {
      setDeletingId(null)
      setTimeout(() => setDeleteMsg(null), 2500)
    }
  }

  async function deleteNotification(id: number) {
    const ok = await openConfirm({
      title: `Delete notification #${id}?`,
      message: 'This cannot be undone.',
      confirmText: 'Delete',
      cancelText: 'Cancel'
    })
    if (!ok) return
    const toastId = toast.loading('Deleting notification…')
    try {
      await svcDeleteNotification(id)
      await loadNotifications()
      setNotifDeleteMsg(`Notification #${id} deleted`)
      toast.success(`Notification #${id} deleted`, { id: toastId })
      setTimeout(() => setNotifDeleteMsg(null), 2500)
    } catch (e: unknown) {
      const ax = e as any
      if (axios.isAxiosError(ax) && ax.response) {
        const msg = `Delete failed: HTTP ${ax.response.status} ${JSON.stringify(ax.response.data)}`
        setNotificationsError(msg)
        toast.error(msg)
      } else {
        const msg = (e as Error).message || String(e)
        setNotificationsError(msg)
        toast.error(msg)
      }
    }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <header className="border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <button
                className="sm:hidden btn-secondary w-9 h-9 p-0 -ml-1"
                onClick={() => setMobileNavOpen(true)}
                ref={mobileMenuButtonRef}
                aria-label="Open navigation"
                aria-expanded={mobileNavOpen}
                aria-controls="mobile-nav-drawer"
                title="Open navigation"
              >
                {/* Hamburger icon */}
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5">
                  <path d="M3.75 6.75h16.5a.75.75 0 000-1.5H3.75a.75.75 0 000 1.5zm16.5 5.25H3.75a.75.75 0 000 1.5h16.5a.75.75 0 000-1.5zm0 6.75H3.75a.75.75 0 000 1.5h16.5a.75.75 0 000-1.5z" />
                </svg>
              </button>
              <h1 className="text-xl font-semibold tracking-tight">AiWebSiteWatchdog</h1>
            </div>
            <div className="flex items-center gap-2">
              <nav aria-label="Primary" className="hidden sm:block">
                <ul className="flex items-center gap-1">
                  {[
                    { key: 'dashboard', label: 'Dashboard' },
                    { key: 'tasks', label: 'Tasks' },
                    { key: 'notifications', label: 'Notifications' },
                    { key: 'settings', label: 'Settings' }
                  ].map(t => (
                    <li key={t.key}>
                      <button
                        onClick={() => setActiveTab(t.key as any)}
                        className={"px-3 py-2 rounded-md text-sm font-medium transition-colors " + (activeTab === t.key
                          ? 'bg-indigo-600 text-white dark:bg-indigo-500'
                          : 'text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700')}
                        aria-current={activeTab === t.key ? 'page' : undefined}
                      >
                        {t.label}
                      </button>
                    </li>
                  ))}
                </ul>
              </nav>
              <button
                className="btn-secondary w-9 h-9 p-0 ml-2"
                onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
                title={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
                aria-label="Toggle dark mode"
              >
                {theme === 'dark' ? (
                  // Sun icon
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5">
                    <path d="M12 18a6 6 0 100-12 6 6 0 000 12z" />
                    <path fillRule="evenodd" d="M12 1.5a.75.75 0 01.75.75v2a.75.75 0 01-1.5 0v-2A.75.75 0 0112 1.5zm0 17.5a.75.75 0 01.75.75v2a.75.75 0 01-1.5 0v-2A.75.75 0 0112 19zM4.72 4.72a.75.75 0 011.06 0l1.414 1.415a.75.75 0 11-1.06 1.06L4.72 5.78a.75.75 0 010-1.06zm11.086 11.086a.75.75 0 011.06 0l1.415 1.414a.75.75 0 01-1.06 1.06l-1.415-1.414a.75.75 0 010-1.06zM1.5 12a.75.75 0 01.75-.75h2a.75.75 0 010 1.5h-2A.75.75 0 011.5 12zm17.5 0a.75.75 0 01.75-.75h2a.75.75 0 010 1.5h-2a.75.75 0 01-.75-.75zM4.72 19.28a.75.75 0 010-1.06l1.415-1.415a.75.75 0 011.06 1.06L5.78 19.28a.75.75 0 01-1.06 0zm11.086-11.086a.75.75 0 010-1.06l1.414-1.415a.75.75 0 111.06 1.06l-1.414 1.415a.75.75 0 01-1.06 0z" clipRule="evenodd" />
                  </svg>
                ) : (
                  // Moon icon
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5">
                    <path d="M21 12.79A9 9 0 1111.21 3a7 7 0 109.79 9.79z" />
                  </svg>
                )}
              </button>
            </div>
          </div>
          {/* Mobile sidebar drawer for navigation (unmounted when closed) */}
          {mobileNavOpen && (
            <div ref={mobileNavRootRef} className="sm:hidden fixed inset-0 z-40">
              {/* Backdrop */}
              <div
                className="absolute inset-0 bg-black/50"
                onClick={() => { setMobileNavOpen(false); setTimeout(() => mobileMenuButtonRef.current?.focus(), 0) }}
              />
              {/* Drawer */}
              <aside
                role="dialog"
                aria-label="Navigation"
                aria-modal="true"
                id="mobile-nav-drawer"
                ref={mobileNavDrawerRef}
                className="absolute left-0 top-0 h-full w-64 max-w-[80vw] bg-white dark:bg-gray-800 shadow-lg transform translate-x-0 transition-transform"
              >
                <div className="p-4 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
                  <span className="text-base font-semibold">Navigation</span>
                  <button
                    className="btn-secondary w-8 h-8 p-0"
                    aria-label="Close navigation"
                    ref={mobileNavCloseBtnRef}
                    onClick={() => { setMobileNavOpen(false); setTimeout(() => mobileMenuButtonRef.current?.focus(), 0) }}
                  >
                    {/* Close icon */}
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4">
                      <path fillRule="evenodd" d="M5.47 5.47a.75.75 0 011.06 0L12 10.94l5.47-5.47a.75.75 0 111.06 1.06L13.06 12l5.47 5.47a.75.75 0 11-1.06 1.06L12 13.06l-5.47 5.47a.75.75 0 11-1.06-1.06L10.94 12 5.47 6.53a.75.75 0 010-1.06z" clipRule="evenodd" />
                    </svg>
                  </button>
                </div>
                <nav className="p-2">
                  <ul className="space-y-1">
                    {[
                      { key: 'dashboard', label: 'Dashboard' },
                      { key: 'tasks', label: 'Tasks' },
                      { key: 'notifications', label: 'Notifications' },
                      { key: 'settings', label: 'Settings' }
                    ].map(t => (
                      <li key={t.key}>
                        <button
                          onClick={() => { setActiveTab(t.key as any); setMobileNavOpen(false); setTimeout(() => mobileMenuButtonRef.current?.focus(), 0) }}
                          className={"w-full text-left px-3 py-2 rounded-md text-sm font-medium transition-colors " + (activeTab === t.key
                            ? 'bg-indigo-600 text-white dark:bg-indigo-500'
                            : 'text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700')}
                          aria-current={activeTab === t.key ? 'page' : undefined}
                        >
                          {t.label}
                        </button>
                      </li>
                    ))}
                  </ul>
                </nav>
              </aside>
            </div>
          )}
        </div>
      </header>
      <main className="flex-1 max-w-7xl mx-auto w-full px-6 py-6 space-y-6">
        {!loaded && <p className="text-gray-600 dark:text-gray-300">Loading…</p>}

        {activeTab === 'dashboard' && (
          <DashboardSection
            settings={settings}
            tasks={tasks}
            notifications={notifications}
            loaded={loaded}
          />
        )}

        {activeTab === 'settings' && (
          <>
            <SettingsSection
              settings={settings}
              loaded={loaded}
              form={form}
              setForm={setForm}
              saving={saving}
              onSave={saveSettings}
            />
            <AuthCard settings={settings} />
          </>
        )}

        {activeTab === 'tasks' && (
          <TasksSection
            tasks={tasks}
            onNewTask={() => setShowCreateTaskModal(true)}
            onEditTask={startEdit}
            onRunTask={runTask}
            onDeleteTask={deleteTask}
            runningId={runningId}
            deletingId={deletingId}
          />
        )}

        {activeTab === 'notifications' && (
          <NotificationsSection
            notifications={notifications}
            onDelete={deleteNotification}
          />
        )}
      </main>

      {/* Modals */}
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

      <ConfirmDialog
        open={confirmOpen}
        title={confirmTitle}
        message={confirmMessage}
        confirmText={confirmConfirmText}
        cancelText={confirmCancelText}
        onConfirm={() => handleConfirm(true)}
        onCancel={() => handleConfirm(false)}
      />
    </div>
  )
}
