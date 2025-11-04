import { SettingsForm } from '../../types'

type Props = {
  open: boolean
  form: SettingsForm
  setForm: React.Dispatch<React.SetStateAction<SettingsForm>>
  saving: boolean
  onSave: () => Promise<boolean>
  onClose: () => void
}

export default function SettingsModal({ open, form, setForm, saving, onSave, onClose }: Props) {
  if (!open) return null
  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,.35)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
      <div style={{ background: '#fff', padding: 16, borderRadius: 8, width: 'min(680px, 95vw)' }}>
        <h3>Edit settings</h3>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: 8 }}>
          <label>
            User email
            <input
              type="email"
              value={form.userEmail}
              onChange={e => setForm(f => ({ ...f, userEmail: e.target.value }))}
              placeholder="you@example.com"
              style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label>
            Sender email
            <input
              type="email"
              value={form.senderEmail}
              onChange={e => setForm(f => ({ ...f, senderEmail: e.target.value }))}
              placeholder="bot@example.com"
              style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label>
            Sender name
            <input
              type="text"
              value={form.senderName}
              onChange={e => setForm(f => ({ ...f, senderName: e.target.value }))}
              placeholder="Ai Watchdog"
              style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label>
            Gemini API URL
            <input
              type="url"
              value={form.geminiApiUrl}
              onChange={e => setForm(f => ({ ...f, geminiApiUrl: e.target.value }))}
              placeholder="https://generativelanguage.googleapis.com/v1beta/models/...:generateContent"
              style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <div>
            <button onClick={async () => { const ok = await onSave(); if (ok) onClose(); }} disabled={saving}>
              {saving ? 'Saving…' : 'Save'}
            </button>
            <button onClick={onClose} style={{ marginLeft: 8 }} disabled={saving}>Cancel</button>
          </div>
        </div>
      </div>
    </div>
  )
}
