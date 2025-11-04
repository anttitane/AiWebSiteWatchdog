import { NotificationItem } from '../../types'
import { formatDateTime } from '../../utils/format'

type Props = {
  open: boolean
  notification: NotificationItem | null
  onClose: () => void
}

export default function NotificationDetailsModal({ open, notification, onClose }: Props) {
  if (!open || !notification) return null
  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,.35)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
      <div style={{ background: '#fff', padding: 16, borderRadius: 8, width: 'min(720px, 95vw)' }}>
        <h3>Notification #{notification.id}</h3>
        <p style={{ marginTop: 0, color: '#666' }}>{formatDateTime(notification.sentAt)}</p>
        <h4 style={{ marginTop: 0 }}>{notification.subject}</h4>
        <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', background: '#f7f7f7', padding: 12, borderRadius: 6, maxHeight: '50vh', overflow: 'auto' }}>
{notification.message}
        </pre>
        <div style={{ marginTop: 12 }}>
          <button onClick={onClose}>Close</button>
        </div>
      </div>
    </div>
  )
}
