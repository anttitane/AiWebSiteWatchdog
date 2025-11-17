import { Settings, SettingsForm } from '../../types'
import { useState, useEffect, useMemo } from 'react'

type Props = {
  settings: Settings | null
  loaded: boolean
  form: SettingsForm
  setForm: React.Dispatch<React.SetStateAction<SettingsForm>>
  saving: boolean
  onSave: () => Promise<boolean>
}

export default function SettingsSection({ settings, loaded, form, setForm, saving, onSave }: Props) {
  const [editing, setEditing] = useState(false)
  const [original, setOriginal] = useState<SettingsForm | null>(null)

  // If settings change while not editing, sync form
  useEffect(() => {
    if (!editing && settings) {
      setForm({
        userEmail: settings.userEmail || '',
        senderEmail: settings.senderEmail || '',
        senderName: settings.senderName || '',
        geminiApiUrl: settings.geminiApiUrl || ''
      })
    }
  }, [settings, editing, setForm])

  function startEdit() {
    setOriginal({ ...form })
    setEditing(true)
  }
  function cancelEdit() {
    if (original) {
      setForm(original)
    } else if (settings) {
      setForm({
        userEmail: settings.userEmail || '',
        senderEmail: settings.senderEmail || '',
        senderName: settings.senderName || '',
        geminiApiUrl: settings.geminiApiUrl || ''
      })
    }
    setEditing(false)
    setOriginal(null)
  }
  async function saveEdit() {
    const ok = await onSave()
    if (ok) {
      setEditing(false)
      setOriginal(null)
    }
  }
  // Changed field helper
  const changed = useMemo(() => {
    if (!editing || !original) return {
      userEmail: false,
      senderEmail: false,
      senderName: false,
      geminiApiUrl: false
    }
    return {
      userEmail: form.userEmail !== original.userEmail,
      senderEmail: form.senderEmail !== original.senderEmail,
      senderName: form.senderName !== original.senderName,
      geminiApiUrl: form.geminiApiUrl !== original.geminiApiUrl
    }
  }, [editing, original, form])
  return (
    <section className="card">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold">Settings</h2>
        <div className="flex items-center gap-3">
          {!editing && (
            <button className="btn-primary px-3 py-2" onClick={startEdit} disabled={saving || !loaded}>
              Edit
            </button>
          )}
        </div>
      </div>
      <div className={"space-y-4 text-sm " + (editing ? '' : 'pointer-events-none')}> {/* keep interactive only when editing */}
          <label className={"text-sm flex flex-col relative " + (changed.userEmail ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
            <span className="text-gray-700 dark:text-gray-200 flex items-center gap-1">
              User email {changed.userEmail && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}
            </span>
            <input
              className={
                "modal-input transition-colors " +
                (editing
                  ? ''
                  : 'bg-gray-50 text-gray-600 border border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-500'
                ) +
                (changed.userEmail ? ' ring-2 ring-amber-300 dark:ring-amber-500' : '')
              }
              type="email"
              value={form.userEmail}
              onChange={e => setForm(f => ({ ...f, userEmail: e.target.value }))}
              placeholder="you@example.com"
              disabled={saving || !editing}
            />
          </label>
          <label className={"text-sm flex flex-col relative " + (changed.senderEmail ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
            <span className="text-gray-700 dark:text-gray-200 flex items-center gap-1">
              Sender email {changed.senderEmail && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}
            </span>
            <input
              className={
                "modal-input transition-colors " +
                (editing
                  ? ''
                  : 'bg-gray-50 text-gray-600 border border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-500'
                ) +
                (changed.senderEmail ? ' ring-2 ring-amber-300 dark:ring-amber-500' : '')
              }
              type="email"
              value={form.senderEmail}
              onChange={e => setForm(f => ({ ...f, senderEmail: e.target.value }))}
              placeholder="bot@example.com"
              disabled={saving || !editing}
            />
          </label>
          <label className={"text-sm flex flex-col relative " + (changed.senderName ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
            <span className="text-gray-700 dark:text-gray-200 flex items-center gap-1">
              Sender name {changed.senderName && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}
            </span>
            <input
              className={
                "modal-input transition-colors " +
                (editing
                  ? ''
                  : 'bg-gray-50 text-gray-600 border border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-500'
                ) +
                (changed.senderName ? ' ring-2 ring-amber-300 dark:ring-amber-500' : '')
              }
              type="text"
              value={form.senderName}
              onChange={e => setForm(f => ({ ...f, senderName: e.target.value }))}
              placeholder="Ai Watchdog"
              disabled={saving || !editing}
            />
          </label>
          <label className={"text-sm flex flex-col relative " + (changed.geminiApiUrl ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
            <span className="text-gray-700 dark:text-gray-200 flex items-center gap-1">
              Gemini API URL {changed.geminiApiUrl && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}
            </span>
            <input
              className={
                "modal-input font-mono text-xs transition-colors " +
                (editing
                  ? ''
                  : 'bg-gray-50 text-gray-600 border border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-500'
                ) +
                (changed.geminiApiUrl ? ' ring-2 ring-amber-300 dark:ring-amber-500' : '')
              }
              type="url"
              value={form.geminiApiUrl}
              onChange={e => setForm(f => ({ ...f, geminiApiUrl: e.target.value }))}
              placeholder="https://generativelanguage.googleapis.com/v1beta/models/...:generateContent"
              disabled={saving || !editing}
            />
          </label>
          {editing && (
            <div className="flex justify-end gap-3 pt-2">
              <button className="btn-secondary px-3 py-2" onClick={cancelEdit} disabled={saving}>Cancel</button>
              <button className="btn-primary px-3 py-2" onClick={saveEdit} disabled={saving}>Save</button>
            </div>
          )}
      </div>
    </section>
  )
}
