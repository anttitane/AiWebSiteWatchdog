import { useState, useEffect } from 'react'
import { Settings } from '../../types'

interface Props {
  settings: Settings | null
}

export default function AuthCard({ settings }: Props) {
  const [email, setEmail] = useState('')

  useEffect(() => {
    if (settings?.senderEmail && !email) {
      setEmail(settings.senderEmail)
    }
  }, [settings, email])

  function openConsent() {
    const em = (email || '').trim()
    if (!em) return
    window.open(`/auth/start?senderEmail=${encodeURIComponent(em)}`, '_blank')
  }

  return (
    <section className="card">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold flex items-center gap-2">Google Authorization</h2>
      </div>
      <div className="space-y-4 text-sm">
        <p className="text-gray-600 dark:text-gray-300">Authorize access for sending email and Gemini usage. This will open the Google consent screen in a new tab. After approving you can close that tab.</p>
        <label className="text-sm flex flex-col">
          <span className="text-gray-700 dark:text-gray-200">Sender email</span>
          <input
            className="modal-input"
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            placeholder="you@example.com"
          />
        </label>
        <p className="text-xs text-gray-500">If your email is saved in settings you can leave it as-is.</p>
        <div className="flex justify-end gap-3 pt-2">
          <button
            className="btn-primary px-3 py-2 disabled:opacity-50"
            onClick={openConsent}
            disabled={!email.trim()}
          >
            Open Google consent
          </button>
        </div>
      </div>
    </section>
  )
}
