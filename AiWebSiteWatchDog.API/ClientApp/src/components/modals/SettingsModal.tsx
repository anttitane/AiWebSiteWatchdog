import { SettingsForm } from '../../types'
import { useModalAnimation } from '../../hooks/useModalAnimation'

type Props = {
  open: boolean
  form: SettingsForm
  setForm: React.Dispatch<React.SetStateAction<SettingsForm>>
  saving: boolean
  onSave: () => Promise<boolean>
  onClose: () => void
}

export default function SettingsModal({ open, form, setForm, saving, onSave, onClose }: Props) {
  const { mounted, leaving } = useModalAnimation(open)
  if (!mounted) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className={"absolute inset-0 bg-black/40 " + (leaving ? 'modal-anim-overlay-out' : 'modal-anim-overlay')} onClick={onClose} />
      <div className="relative z-10 w-full max-w-2xl mx-auto">
        <div className={("bg-white dark:bg-gray-800 rounded-lg shadow-xl border border-gray-200 dark:border-gray-700 p-6 ") + (leaving ? 'modal-anim-panel-out' : 'modal-anim-panel')}>
          <h3 className="text-lg font-semibold mb-4">Edit settings</h3>
          <div className="grid grid-cols-1 gap-4">
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">User email</span>
              <input
                className="modal-input"
                type="email"
                value={form.userEmail}
                onChange={e => setForm(f => ({ ...f, userEmail: e.target.value }))}
                placeholder="you@example.com"
              />
            </label>
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Sender email</span>
              <input
                className="modal-input"
                type="email"
                value={form.senderEmail}
                onChange={e => setForm(f => ({ ...f, senderEmail: e.target.value }))}
                placeholder="bot@example.com"
              />
            </label>
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Sender name</span>
              <input
                className="modal-input"
                type="text"
                value={form.senderName}
                onChange={e => setForm(f => ({ ...f, senderName: e.target.value }))}
                placeholder="Ai Watchdog"
              />
            </label>
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Gemini API URL</span>
              <input
                className="modal-input font-mono text-xs"
                type="url"
                value={form.geminiApiUrl}
                onChange={e => setForm(f => ({ ...f, geminiApiUrl: e.target.value }))}
                placeholder="https://generativelanguage.googleapis.com/v1beta/models/...:generateContent"
              />
            </label>
            <div className="flex justify-end gap-3 pt-2">
              <button className="btn-secondary px-3 py-2" onClick={onClose} disabled={saving}>Cancel</button>
              <button className="btn-primary px-3 py-2" onClick={async () => { const ok = await onSave(); if (ok) onClose(); }} disabled={saving}>
                {saving ? 'Saving…' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
