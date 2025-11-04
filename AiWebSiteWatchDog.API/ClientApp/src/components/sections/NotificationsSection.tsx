import { NotificationItem } from '../../types'
import { formatDateTime } from '../../utils/format'

type Props = {
  notifications: NotificationItem[] | null
  error: string | null
  onShow: (n: NotificationItem) => void
  onDelete: (id: number) => void
  deleteMsg?: string | null
}

export default function NotificationsSection({ notifications, error, onShow, onDelete, deleteMsg }: Props) {
  return (
    <section>
      <h3>Notifications</h3>
      {error && <p style={{ color: 'crimson' }}>{error}</p>}
      {deleteMsg && <p style={{ color: 'seagreen' }}>{deleteMsg}</p>}
      {notifications && notifications.length > 0 ? (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
          {notifications.map(n => (
            <li key={n.id} style={{ marginBottom: 8, display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
              <span style={{ minWidth: 240 }}>{formatDateTime(n.sentAt)}</span>
              <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                <span style={{ fontWeight: 600, minWidth: 400 }}>{n.subject}</span>
                <button onClick={() => onShow(n)}>Show</button>
                <button onClick={() => onDelete(n.id)}>Delete</button>
              </div>
            </li>
          ))}
        </ul>
      ) : (
        <p>No notifications yet.</p>
      )}
    </section>
  )
}
