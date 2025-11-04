type Props = {
  open: boolean
  email: string
  setEmail: (v: string) => void
  onClose: () => void
}

export default function AuthModal({ open, email, setEmail, onClose }: Props) {
  if (!open) return null
  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,.35)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
      <div style={{ background: '#fff', padding: 16, borderRadius: 8, width: 'min(580px, 95vw)' }}>
        <h3>Authorize Gmail/Gemini</h3>
        <p>Open Google consent in a new tab. After you approve, you can close that tab.</p>
        <label style={{ display: 'block', marginTop: 8 }}>
          Sender email
          <input
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            placeholder="you@example.com"
            style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
          />
        </label>
        <div style={{ marginTop: 12 }}>
          <button
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
          <button onClick={onClose} style={{ marginLeft: 8 }}>Cancel</button>
        </div>
        <p style={{ marginTop: 8 }}>
          <small>If your email is saved in settings, you can leave it as-is.</small>
        </p>
      </div>
    </div>
  )
}
