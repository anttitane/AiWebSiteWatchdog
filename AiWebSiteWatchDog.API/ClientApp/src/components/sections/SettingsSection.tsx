import { Settings } from '../../types'

type Props = {
  settings: Settings | null
  loaded: boolean
  onEditSettings: () => void
  onAuthorizeGoogle: () => void
}

export default function SettingsSection({ settings, loaded, onEditSettings, onAuthorizeGoogle }: Props) {
  return (
    <section>
      <h2>Settings</h2>
      {settings ? (
        <>
          <ul>
            <li>User email: <strong>{settings.userEmail || '(not set)'}</strong></li>
            <li>Sender email: <strong>{settings.senderEmail || '(not set)'}</strong></li>
            <li>Sender name: <strong>{settings.senderName || '(not set)'}</strong></li>
            <li>Gemini API URL: <code>{settings.geminiApiUrl || '(default)'}</code></li>
          </ul>
          <div>
            <button onClick={onEditSettings}>Edit settings</button>
            <button onClick={onAuthorizeGoogle} style={{ marginLeft: 8 }}>Authorize Google</button>
          </div>
        </>
      ) : loaded ? (
        <p>No saved settings yet. Use "Edit settings" to create them.</p>
      ) : null}
    </section>
  )
}
