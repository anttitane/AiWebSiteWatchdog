import { Settings } from '../../types'
import { useLocalStorageBoolean } from '../../hooks/useLocalStorage'

type Props = {
  settings: Settings | null
  loaded: boolean
  onEditSettings: () => void
  onAuthorizeGoogle: () => void
}

export default function SettingsSection({ settings, loaded, onEditSettings, onAuthorizeGoogle }: Props) {
  const [collapsed, setCollapsed] = useLocalStorageBoolean('ui.settings.collapsed', false)
  return (
    <section className="card">
      <div className={"flex items-center justify-between " + (collapsed ? 'mb-0' : 'mb-4')}>
        <div className="flex items-center gap-2">
          <button
            className="btn-secondary w-8 h-8 p-0 leading-none"
            onClick={() => setCollapsed(v => !v)}
            aria-expanded={!collapsed}
            title={collapsed ? 'Expand' : 'Collapse'}
          >
            {collapsed ? '+' : '−'}
          </button>
          <h2 className="text-lg font-semibold">Settings</h2>
        </div>
        <div className="flex items-center gap-3">
          <button className="btn-secondary px-3 py-2" onClick={onAuthorizeGoogle}>Authorize Google</button>
          <button className="btn-primary px-3 py-2" onClick={onEditSettings}>Edit settings</button>
        </div>
      </div>
      {!collapsed && (settings ? (
        <div className="grid sm:grid-cols-2 gap-3 text-sm">
          <div>
            <div className="text-gray-600 dark:text-gray-300">User email</div>
            <div className="font-medium">{settings.userEmail || <span className="text-gray-500">(not set)</span>}</div>
          </div>
          <div>
            <div className="text-gray-600 dark:text-gray-300">Sender email</div>
            <div className="font-medium">{settings.senderEmail || <span className="text-gray-500">(not set)</span>}</div>
          </div>
          <div>
            <div className="text-gray-600 dark:text-gray-300">Sender name</div>
            <div className="font-medium">{settings.senderName || <span className="text-gray-500">(not set)</span>}</div>
          </div>
          <div className="sm:col-span-2">
            <div className="text-gray-600 dark:text-gray-300">Gemini API URL</div>
            <div className="font-mono text-xs inline-flex items-center gap-2 max-w-full">
              <span className="truncate block max-w-[640px]" title={settings.geminiApiUrl || undefined}>{settings.geminiApiUrl || '(default)'}</span>
            </div>
          </div>
        </div>
      ) : loaded ? (
        <p className="text-gray-600 dark:text-gray-300">No saved settings yet. Use "Edit settings" to create them.</p>
      ) : null)}
    </section>
  )
}
