import { NewTaskForm } from '../../types'

type Props = {
  open: boolean
  newTask: NewTaskForm
  setNewTask: React.Dispatch<React.SetStateAction<NewTaskForm>>
  creating: boolean
  onCreate: () => Promise<boolean>
  onClose: () => void
}

export default function CreateTaskModal({ open, newTask, setNewTask, creating, onCreate, onClose }: Props) {
  if (!open) return null
  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,.35)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
      <div style={{ background: '#fff', padding: 16, borderRadius: 8, width: 'min(720px, 95vw)' }}>
        <h3>Create new task</h3>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: 8 }}>
          <label>
            Title
            <input
              type="text"
              value={newTask.title}
              onChange={e => setNewTask(s => ({ ...s, title: e.target.value }))}
              placeholder="Describe what to check"
                style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label>
            URL (remember to include https:// or http://)
            <input
              type="url"
              value={newTask.url}
              onChange={e => setNewTask(s => ({ ...s, url: e.target.value }))}
              placeholder="https://example.com"
                style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label>
            Task prompt
            <textarea
              value={newTask.taskPrompt}
              onChange={e => setNewTask(s => ({ ...s, taskPrompt: e.target.value }))}
              placeholder="Explain what AI should look from the page"
              rows={3}
                style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label>
            Schedule (cron, 5 or 6 fields)
            <input
              type="text"
              value={newTask.schedule}
              onChange={e => setNewTask(s => ({ ...s, schedule: e.target.value }))}
              placeholder="*/15 * * * *"
                style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
            <input
              type="checkbox"
              checked={newTask.enabled}
              onChange={e => setNewTask(s => ({ ...s, enabled: e.target.checked }))}
            />
            Task enabled
          </label>
          <div>
            <button onClick={async () => { const ok = await onCreate(); if (ok) onClose(); }} disabled={creating}>
              {creating ? 'Creating…' : 'Create task'}
            </button>
            <button onClick={onClose} style={{ marginLeft: 8 }} disabled={creating}>Cancel</button>
          </div>
        </div>
      </div>
    </div>
  )
}
