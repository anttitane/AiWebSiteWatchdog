import { EditTaskForm } from '../../types'

type Props = {
  open: boolean
  editId: number | null
  editTask: EditTaskForm
  setEditTask: React.Dispatch<React.SetStateAction<EditTaskForm>>
  updating: boolean
  updateMsg: string | null
  onSave: (id: number) => Promise<boolean>
  onCancel: () => void
}

export default function EditTaskModal({ open, editId, editTask, setEditTask, updating, updateMsg, onSave, onCancel }: Props) {
  if (!open || editId === null) return null
  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,.35)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
      <div style={{ background: '#fff', padding: 16, borderRadius: 8, width: 'min(720px, 95vw)' }}>
        <h3>Edit task #{editId}</h3>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: 8 }}>
          <label>
            Title
            <input
              type="text"
              value={editTask.title ?? ''}
              onChange={e => setEditTask(s => ({ ...s, title: e.target.value }))}
              style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label>
            URL
            <input
              type="url"
              value={editTask.url ?? ''}
              onChange={e => setEditTask(s => ({ ...s, url: e.target.value }))}
              style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label>
            Task prompt
            <textarea
              value={editTask.taskPrompt ?? ''}
              onChange={e => setEditTask(s => ({ ...s, taskPrompt: e.target.value }))}
              rows={3}
              style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label>
            Schedule
            <input
              type="text"
              value={editTask.schedule ?? ''}
              onChange={e => setEditTask(s => ({ ...s, schedule: e.target.value }))}
              style={{ width: '100%', padding: '.5rem', marginTop: 4, boxSizing: 'border-box' }}
            />
          </label>
          <label style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
            <input
              type="checkbox"
              checked={!!editTask.enabled}
              onChange={e => setEditTask(s => ({ ...s, enabled: e.target.checked }))}
            />
            Enabled
          </label>
          <div>
            <button onClick={async () => { const ok = await onSave(editId); if (ok) onCancel(); }} disabled={updating}>
              {updating ? 'Saving…' : 'Save'}
            </button>
            <button onClick={onCancel} style={{ marginLeft: 8 }} disabled={updating}>Cancel</button>
            {updateMsg && <span style={{ color: 'seagreen', marginLeft: 12 }}>{updateMsg}</span>}
          </div>
        </div>
      </div>
    </div>
  )
}
