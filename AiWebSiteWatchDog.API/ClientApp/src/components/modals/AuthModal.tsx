import { useModalAnimation } from '../../hooks/useModalAnimation'

type Props = {
  open: boolean
  email: string
  setEmail: (v: string) => void
  onClose: () => void
}

export default function AuthModal({ open, email, setEmail, onClose }: Props) {
  const { mounted, leaving } = useModalAnimation(open)
  if (!mounted) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className={"absolute inset-0 bg-black/40 " + (leaving ? 'modal-anim-overlay-out' : 'modal-anim-overlay')} onClick={onClose} />
      <div className="relative z-10 w-full max-w-xl mx-auto">
        <div className={"bg-white dark:bg-gray-800 rounded-lg shadow-xl border border-gray-200 dark:border-gray-700 p-6 " + (leaving ? 'modal-anim-panel-out' : 'modal-anim-panel')}>
          <h3 className="text-lg font-semibold mb-2">Authorize Gmail/Gemini</h3>
          <p className="text-sm text-gray-600 dark:text-gray-300">Open Google consent in a new tab. After you approve, you can close that tab.</p>
          <label className="block text-sm mt-4">
            <span className="text-gray-700 dark:text-gray-200">Sender email</span>
            <input
              className="modal-input"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="you@example.com"
            />
          </label>
          <div className="flex justify-end gap-3 mt-4">
            <button
              className="btn-primary px-3 py-2 disabled:opacity-50"
              onClick={() => {
                const em = (email || '').trim()
                if (!em) return
                window.open(`/auth/start?senderEmail=${encodeURIComponent(em)}`, '_blank')
                onClose()
              }}
              disabled={!email.trim()}
            >
              Open Google consent
            </button>
            <button className="btn-secondary px-3 py-2" onClick={onClose}>Cancel</button>
          </div>
          <p className="text-xs text-gray-500 mt-3">If your email is saved in settings, you can leave it as-is.</p>
        </div>
      </div>
    </div>
  )
}
