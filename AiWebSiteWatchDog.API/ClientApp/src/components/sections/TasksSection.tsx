import { WatchTaskFull } from '../../types'

type Props = {
  tasks: WatchTaskFull[] | null
  onNewTask: () => void
  onEditTask: (t: WatchTaskFull) => void
  onRunTask: (id: number) => void
  onDeleteTask: (id: number) => void
  runningId: number | null
  deletingId: number | null
}

export default function TasksSection({ tasks, onNewTask, onEditTask, onRunTask, onDeleteTask, runningId, deletingId }: Props) {
  return (
    <section className="card">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold">Watch tasks</h3>
        <div className="flex items-center gap-3 text-sm">
          <button className="btn-primary px-3 py-2" onClick={onNewTask}>New task</button>
        </div>
      </div>

      {tasks && tasks.length > 0 ? (
        <ul className="divide-y divide-gray-200 dark:divide-gray-700">
          {tasks.map(t => (
            <li key={t.id} className="py-3">
              <div className="flex items-start gap-4 flex-wrap">
                <span className="shrink-0 inline-flex items-center justify-center w-8 h-8 rounded-md bg-gray-100 dark:bg-gray-700 text-xs font-semibold">#{t.id}</span>
                <div className="min-w-[280px] flex-1">
                  <div className="font-medium leading-6">
                    {t.title}
                  </div>
                  <div className="text-xs font-mono text-gray-600 dark:text-gray-300 truncate max-w-[680px]">{t.url}</div>
                  <div className="mt-1 flex items-center gap-2 text-xs">
                    <span className={"badge " + (t.enabled ? 'badge-success' : 'badge-error')}>{t.enabled ? 'Enabled' : 'Disabled'}</span>
                    {t.schedule && (
                      <span className="inline-flex items-center rounded-full bg-gray-100 dark:bg-gray-700 px-2 py-0.5 text-xs text-gray-700 dark:text-gray-200">cron: {t.schedule}</span>
                    )}
                    {t.lastChecked && (
                      <span className="text-gray-500">last checked: {new Date(t.lastChecked).toLocaleString()}</span>
                    )}
                  </div>
                </div>
                <div className="ml-auto flex items-center gap-2">
                  <button className="btn-secondary px-3 py-1.5" onClick={() => onEditTask(t)}>Edit</button>
                  <button className="btn-secondary px-3 py-1.5" onClick={() => onRunTask(t.id)} disabled={runningId === t.id}>
                    {runningId === t.id ? 'Running…' : 'Run now'}
                  </button>
                  <button className="btn-secondary px-3 py-1.5" onClick={() => onDeleteTask(t.id)} disabled={deletingId === t.id}>
                    {deletingId === t.id ? 'Deleting…' : 'Delete'}
                  </button>
                </div>
              </div>
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-gray-600 dark:text-gray-300">No tasks yet.</p>
      )}
    </section>
  ) 
}
