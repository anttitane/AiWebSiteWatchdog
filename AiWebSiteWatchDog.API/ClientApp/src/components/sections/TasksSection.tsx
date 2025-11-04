import { WatchTaskFull } from '../../types'

type Props = {
  tasks: WatchTaskFull[] | null
  error: string | null
  onNewTask: () => void
  onEditTask: (t: WatchTaskFull) => void
  onRunTask: (id: number) => void
  onDeleteTask: (id: number) => void
  runningId: number | null
  deletingId: number | null
  createMsg?: string | null
}

export default function TasksSection({ tasks, error, onNewTask, onEditTask, onRunTask, onDeleteTask, runningId, deletingId, createMsg }: Props) {
  return (
    <section>
      <h3>Watch tasks</h3>
      {error && <p style={{ color: 'crimson' }}>{error}</p>}

      <div style={{ margin: '1rem 0' }}>
        <button onClick={onNewTask}>New task</button>
        {createMsg && <span style={{ color: 'seagreen', marginLeft: 12 }}>{createMsg}</span>}
      </div>

      {tasks && tasks.length > 0 ? (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
          {tasks.map(t => (
            <li key={t.id} style={{ marginBottom: 8, display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
              <span style={{ minWidth: 30 }}>
                <strong>#{t.id}</strong>
              </span>
              <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
                <span style={{ fontWeight: 600, minWidth: 600 }}>
                  {t.title} — <code style={{ wordBreak: 'break-word' }}>{t.url}</code> [{t.enabled ? 'enabled' : 'disabled'}]
                  {t.schedule ? <> — schedule: <code>{t.schedule}</code></> : null}
                </span>
                <button onClick={() => onEditTask(t)}>Edit</button>
                <button onClick={() => onRunTask(t.id)} disabled={runningId === t.id}>
                  {runningId === t.id ? 'Running…' : 'Run now'}
                </button>
                <button onClick={() => onDeleteTask(t.id)} disabled={deletingId === t.id}>
                  {deletingId === t.id ? 'Deleting…' : 'Delete'}
                </button>
              </div>
            </li>
          ))}
        </ul>
      ) : (
        <p>No tasks yet.</p>
      )}
    </section>
  ) 
}
