import { NewTaskForm } from '../../types'
import { useModalAnimation } from '../../hooks/useModalAnimation'
import ScheduleEditor from '../scheduling/ScheduleEditor'

type Props = {
  open: boolean
  newTask: NewTaskForm
  setNewTask: React.Dispatch<React.SetStateAction<NewTaskForm>>
  creating: boolean
  onCreate: () => Promise<boolean>
  onClose: () => void
}

export default function CreateTaskModal({ open, newTask, setNewTask, creating, onCreate, onClose }: Props) {
  const { mounted, leaving } = useModalAnimation(open)
  if (!mounted) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className={"absolute inset-0 bg-black/40 " + (leaving ? 'modal-anim-overlay-out' : 'modal-anim-overlay')} onClick={onClose} />
      <div className="relative z-10 w-full max-w-3xl mx-auto">
        <div className={("bg-white dark:bg-gray-800 rounded-lg shadow-xl border border-gray-200 dark:border-gray-700 p-6 ") + (leaving ? 'modal-anim-panel-out' : 'modal-anim-panel')}>
          <h3 className="text-lg font-semibold mb-4">Create new task</h3>
          <div className="grid grid-cols-1 gap-4">
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Title</span>
              <input
                className="modal-input"
                type="text"
                value={newTask.title}
                onChange={e => setNewTask(s => ({ ...s, title: e.target.value }))}
                placeholder="Describe what to check"
              />
            </label>
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">URL (remember to include https:// or http://)</span>
              <input
                className="modal-input"
                type="url"
                value={newTask.url}
                onChange={e => setNewTask(s => ({ ...s, url: e.target.value }))}
                placeholder="https://example.com"
              />
            </label>
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Task prompt</span>
              <textarea
                className="modal-input"
                value={newTask.taskPrompt}
                onChange={e => setNewTask(s => ({ ...s, taskPrompt: e.target.value }))}
                placeholder="Explain what AI should look from the page"
                rows={3}
              />
            </label>
            <div className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Schedule</span>
              <div className="mt-1">
                <ScheduleEditor
                  value={newTask.schedule}
                  onChange={(cron) => setNewTask(s => ({ ...s, schedule: cron }))}
                />
              </div>
            </div>
            <label className="inline-flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={newTask.enabled}
                onChange={e => setNewTask(s => ({ ...s, enabled: e.target.checked }))}
              />
              Task enabled
            </label>
            <div className="flex justify-end gap-3 pt-2">
              <button className="btn-secondary px-3 py-2" onClick={onClose} disabled={creating}>Cancel</button>
              <button className="btn-primary px-3 py-2" onClick={async () => { const ok = await onCreate(); if (ok) onClose(); }} disabled={creating}>
                {creating ? 'Creating…' : 'Create task'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
