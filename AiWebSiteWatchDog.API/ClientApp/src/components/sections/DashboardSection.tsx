import cronstrue from 'cronstrue'
import { Settings, WatchTaskFull, NotificationItem } from '../../types'
import { formatDateTime, formatRelative } from '../../utils/format'
import { findNextScheduledTask } from '../../utils/scheduling'

interface Props {
  settings: Settings | null
  tasks: WatchTaskFull[] | null
  notifications: NotificationItem[] | null
  loaded: boolean
}

export default function DashboardSection({ settings, tasks, notifications, loaded }: Props) {
  const totalTasks = tasks?.length || 0
  const enabledTasks = (tasks || []).filter(t => t.enabled).length
  const totalNotifs = notifications?.length || 0
  const latestNotification = notifications && notifications.length > 0 ? notifications[0] : null
  const latestFour = (notifications || []).slice(0, 4)
  const nextScheduled = tasks ? findNextScheduledTask(tasks.filter(t => !!t.schedule)) : null

  return (
    <section className="space-y-6">
      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <div className="card">
          <div className="text-xs text-gray-500 dark:text-gray-400">Tasks</div>
          <div className="mt-1 flex items-baseline gap-2">
            <span className="text-2xl font-semibold">{totalTasks}</span>
            <span className="text-sm text-gray-600 dark:text-gray-300">total</span>
          </div>
          <div className="text-xs mt-1 text-gray-600 dark:text-gray-300">{enabledTasks} enabled</div>
        </div>
        <div className="card">
          <div className="text-xs text-gray-500 dark:text-gray-400">Next scheduled task</div>
          {nextScheduled ? (
            <div className="mt-1">
              <div className="font-medium truncate" title={nextScheduled.task.title}>{nextScheduled.task.title}</div>
              {nextScheduled.task.schedule && (
                <div className="text-xs mt-1 text-gray-600 dark:text-gray-300" title={nextScheduled.task.schedule}>
                  {cronstrue.toString(nextScheduled.task.schedule, { use24HourTimeFormat: true })}
                </div>
              )}
              <div className="text-xs mt-1 text-gray-600 dark:text-gray-300" title={formatDateTime(nextScheduled.nextDate)}>
                Runs {formatRelative(nextScheduled.nextDate.toISOString())}
              </div>
            </div>
          ) : (
            <div className="mt-1 text-xs text-gray-600 dark:text-gray-300">No upcoming task</div>
          )}
        </div>
        <div className="card">
          <div className="text-xs text-gray-500 dark:text-gray-400">Notifications</div>
            <div className="mt-1 flex items-baseline gap-2">
              <span className="text-2xl font-semibold">{totalNotifs}</span>
              <span className="text-sm text-gray-600 dark:text-gray-300">total</span>
            </div>
            <div className="text-xs mt-1 text-gray-600 dark:text-gray-300">{latestNotification ? 'Latest ' + formatRelative(latestNotification.sentAt) : 'None yet'}</div>
        </div>
        <div className="card">
          <div className="text-xs text-gray-500 dark:text-gray-400">Settings</div>
          {settings ? (
            <div className="mt-1">
              <span className="text-green-600 dark:text-green-400 font-medium">Configured</span>
              <div className="text-xs text-gray-600 dark:text-gray-300 truncate" title={settings.senderEmail || undefined}>{settings.senderEmail || '(sender not set)'}</div>
            </div>
          ) : loaded ? (
            <div className="mt-1">
              <span className="text-red-600 dark:text-red-400 font-medium">Missing</span>
              <div className="text-xs text-gray-600 dark:text-gray-300">Add settings to enable emails.</div>
            </div>
          ) : (
            <div className="mt-1 text-xs text-gray-600 dark:text-gray-300">Loading…</div>
          )}
        </div>
      </div>

      <div className="card">
        <h3 className="text-sm font-semibold mb-2">Latest Notifications</h3>
        {latestFour.length > 0 ? (
          <ul className="divide-y divide-gray-200 dark:divide-gray-700">
            {latestFour.map(n => (
              <li key={n.id} className="py-3 flex items-start gap-4">
                <span className="w-40 shrink-0 text-xs text-gray-600 dark:text-gray-300" title={formatDateTime(n.sentAt)}>{formatRelative(n.sentAt)}</span>
                <div className="flex-1 min-w-0">
                  <div className="font-medium truncate" title={n.subject}>{n.subject}</div>
                  <div className="text-xs text-gray-600 dark:text-gray-300 truncate" title={n.message}>
                    {(n.message || '').split(/\r?\n/)[0]}
                  </div>
                </div>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-gray-600 dark:text-gray-300">No notifications have been sent yet.</p>
        )}
      </div>
    </section>
  )
}
