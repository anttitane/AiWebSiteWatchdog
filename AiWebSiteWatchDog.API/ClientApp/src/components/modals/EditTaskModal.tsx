import { EditTaskForm } from '../../types'
import { useModalAnimation } from '../../hooks/useModalAnimation'
import ScheduleEditor from '../scheduling/ScheduleEditor'

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
  const { mounted, leaving } = useModalAnimation(open)
  if (!mounted) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className={"absolute inset-0 bg-black/40 " + (leaving ? 'modal-anim-overlay-out' : 'modal-anim-overlay')} onClick={onCancel} />
      <div className="relative z-10 w-full max-w-3xl mx-auto">
        <div className={("bg-white dark:bg-gray-800 rounded-lg shadow-xl border border-gray-200 dark:border-gray-700 p-6 ") + (leaving ? 'modal-anim-panel-out' : 'modal-anim-panel')}>
          <h3 className="text-lg font-semibold mb-4">Edit task #{editId}</h3>
          <div className="grid grid-cols-1 gap-4">
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Title</span>
              <input
                className="modal-input"
                type="text"
                value={editTask.title ?? ''}
                onChange={e => setEditTask(s => ({ ...s, title: e.target.value }))}
              />
            </label>
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">URL</span>
              <input
                className="modal-input"
                type="url"
                value={editTask.url ?? ''}
                onChange={e => setEditTask(s => ({ ...s, url: e.target.value }))}
              />
            </label>
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Task prompt</span>
              <textarea
                className="modal-input"
                value={editTask.taskPrompt ?? ''}
                onChange={e => setEditTask(s => ({ ...s, taskPrompt: e.target.value }))}
                rows={3}
              />
            </label>
            <div className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Schedule</span>
              <div className="mt-1">
                <ScheduleEditor
                  value={editTask.schedule}
                  onChange={(cron) => setEditTask(s => ({ ...s, schedule: cron }))}
                />
              </div>
            </div>
            <label className="inline-flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={!!editTask.enabled}
                onChange={e => setEditTask(s => ({ ...s, enabled: e.target.checked }))}
              />
              Enabled
            </label>
            <div className="flex justify-end gap-3 pt-2">
              <button className="btn-secondary px-3 py-2" onClick={onCancel} disabled={updating}>Cancel</button>
              <button className="btn-primary px-3 py-2" onClick={async () => { const ok = await onSave(editId); if (ok) onCancel(); }} disabled={updating}>
                {updating ? 'Saving…' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
