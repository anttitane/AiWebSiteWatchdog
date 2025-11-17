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
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40 modal-anim-overlay" onClick={onClose} />
      <div className="relative z-10 w-full max-w-3xl mx-auto">
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl border border-gray-200 dark:border-gray-700 p-6 modal-anim-panel">
          <h3 className="text-lg font-semibold">Notification #{notification.id}</h3>
          <p className="text-sm text-gray-600 dark:text-gray-300">{formatDateTime(notification.sentAt)}</p>
          <h4 className="mt-2 font-medium">{notification.subject}</h4>
          <pre className="mt-3 whitespace-pre-wrap break-words bg-gray-50 dark:bg-gray-900/40 p-3 rounded-md max-h-[50vh] overflow-auto">
{notification.message}
          </pre>
          <div className="flex justify-end mt-4">
            <button className="btn-secondary px-3 py-2" onClick={onClose}>Close</button>
          </div>
        </div>
      </div>
    </div>
  )
}
