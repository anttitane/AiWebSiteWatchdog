import { useState } from 'react'
import { NotificationItem } from '../../types'
import { formatDateTime, formatRelative } from '../../utils/format'

type Props = {
  notifications: NotificationItem[] | null
  onDelete: (id: number) => void
}

export default function NotificationsSection({ notifications, onDelete }: Props) {
  const [expanded, setExpanded] = useState<Record<number, boolean>>({})
  const toggle = (id: number) => setExpanded(prev => ({ ...prev, [id]: !prev[id] }))
  const expandAll = () => {
    if (!notifications) return
    const map: Record<number, boolean> = {}
    notifications.forEach(n => { map[n.id] = true })
    setExpanded(map)
  }
  const collapseAll = () => setExpanded({})
  return (
    <section className="card">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold">Notifications</h3>
        <div className="flex items-center gap-2">
          <button
            className="btn-secondary px-2 py-1"
            onClick={expandAll}
            title="Expand all"
            aria-label="Expand all"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4">
              <path fillRule="evenodd" d="M12 4.5a.75.75 0 01.53.22l6.75 6.75a.75.75 0 11-1.06 1.06L12 6.31l-6.22 6.22a.75.75 0 11-1.06-1.06l6.75-6.75a.75.75 0 01.53-.22z" clipRule="evenodd" />
              <path fillRule="evenodd" d="M12 10.5a.75.75 0 01.53.22l6.75 6.75a.75.75 0 11-1.06 1.06L12 12.31l-6.22 6.22a.75.75 0 11-1.06-1.06l6.75-6.75a.75.75 0 01.53-.22z" clipRule="evenodd" />
            </svg>
            <span className="sr-only">Expand all</span>
          </button>
          <button
            className="btn-secondary px-2 py-1"
            onClick={collapseAll}
            title="Collapse all"
            aria-label="Collapse all"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4">
              <path fillRule="evenodd" d="M12 19.5a.75.75 0 01-.53-.22l-6.75-6.75a.75.75 0 011.06-1.06L12 17.69l6.22-6.22a.75.75 0 111.06 1.06l-6.75 6.75a.75.75 0 01-.53.22z" clipRule="evenodd" />
              <path fillRule="evenodd" d="M12 13.5a.75.75 0 01-.53-.22l-6.75-6.75a.75.75 0 111.06-1.06L12 11.69l6.22-6.22a.75.75 0 111.06 1.06l-6.75 6.75a.75.75 0 01-.53.22z" clipRule="evenodd" />
            </svg>
            <span className="sr-only">Collapse all</span>
          </button>
        </div>
      </div>
      {(notifications && notifications.length > 0) ? (
        <ul className="divide-y divide-gray-200 dark:divide-gray-700">
          {notifications.map(n => {
            const isOpen = !!expanded[n.id]
            return (
              <li key={n.id} className="py-3">
                <div className="flex flex-col sm:flex-row items-stretch sm:items-start gap-3 sm:gap-4">
                  <button
                    className="btn-secondary w-8 h-8 p-0 shrink-0"
                    onClick={() => toggle(n.id)}
                    aria-expanded={isOpen}
                    aria-label={isOpen ? 'Collapse notification' : 'Expand notification'}
                    title={isOpen ? 'Collapse' : 'Expand'}
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className={"w-4 h-4 transition-transform " + (isOpen ? 'rotate-180' : '')}>
                      <path fillRule="evenodd" d="M12 15.75a.75.75 0 01-.53-.22l-6-6a.75.75 0 111.06-1.06L12 13.44l5.47-5.47a.75.75 0 111.06 1.06l-6 6a.75.75 0 01-.53.22z" clipRule="evenodd" />
                    </svg>
                  </button>
                  <span className="sm:w-40 w-full shrink-0 text-xs sm:text-sm text-gray-600 dark:text-gray-300" title={formatDateTime(n.sentAt)}>{formatRelative(n.sentAt)}</span>
                  <div className="flex-1 min-w-0">
                    <div className="font-medium truncate" title={n.subject}>{n.subject}</div>
                    {isOpen && (
                      <div className="mt-2 text-xs">
                        <div className="text-gray-600 dark:text-gray-300">{formatDateTime(n.sentAt)}</div>
                        <pre className="mt-2 whitespace-pre-wrap break-words bg-gray-50 dark:bg-gray-900/40 p-3 rounded-md max-h-[40vh] overflow-auto max-w-full" title={n.message}>{n.message}</pre>
                      </div>
                    )}
                  </div>
                  <div className="sm:ml-auto ml-0 flex items-center gap-2 sm:self-auto self-start mt-2 sm:mt-0">
                    <button className="btn-secondary px-3 py-1.5" onClick={() => onDelete(n.id)}>Delete</button>
                  </div>
                </div>
              </li>
            )
          })}
        </ul>
      ) : (
        <p className="text-gray-600 dark:text-gray-300">No notifications yet.</p>
      )}
    </section>
  )
}
