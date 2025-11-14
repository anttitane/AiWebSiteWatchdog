import { NotificationItem } from '../../types'
import { formatDateTime, formatRelative } from '../../utils/format'

type Props = {
  notifications: NotificationItem[] | null
  onShow: (n: NotificationItem) => void
  onDelete: (id: number) => void
}

export default function NotificationsSection({ notifications, onShow, onDelete }: Props) {
  return (
    <section className="card">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold">Notifications</h3>
      </div>
      {notifications && notifications.length > 0 ? (
        <ul className="divide-y divide-gray-200 dark:divide-gray-700">
          {notifications.map(n => (
            <li key={n.id} className="py-3 flex items-start gap-4">
              <span className="w-40 shrink-0 text-sm text-gray-600 dark:text-gray-300" title={formatDateTime(n.sentAt)}>{formatRelative(n.sentAt)}</span>
              <div className="flex-1 min-w-0">
                <div className="font-medium truncate" title={n.subject}>{n.subject}</div>
              </div>
              <div className="ml-auto flex items-center gap-2">
                <button className="btn-secondary px-3 py-1.5" onClick={() => onShow(n)}>Show</button>
                <button className="btn-secondary px-3 py-1.5" onClick={() => onDelete(n.id)}>Delete</button>
              </div>
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-gray-600 dark:text-gray-300">No notifications yet.</p>
      )}
    </section>
  )
}
