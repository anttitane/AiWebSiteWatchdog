import { Settings, SettingsForm, GmailStatus, GeminiStatus } from '../../types'
import GoogleAuthorizationCard from '../cards/GoogleAuthorizationCard'
import NotificationSettingsCard from '../cards/NotificationSettingsCard'
import GeminiApiUrlCard from '../cards/GeminiApiUrlCard'
import { toast } from 'react-hot-toast'
import { updateSettings as svcUpdateSettings, getSettings as svcGetSettings } from '../../services/settings'
import { useCallback } from 'react'

interface Props {
  settings: Settings | null
  loaded: boolean
  form: SettingsForm
  setForm: React.Dispatch<React.SetStateAction<SettingsForm>>
  saving: boolean
  gmailStatus: GmailStatus | null
  geminiStatus: GeminiStatus | null
  refreshAuthStatuses: () => Promise<void>
  onSaveAll: () => Promise<boolean>
  onPartialSaved?: () => Promise<void>
}

export default function SettingsSection({
  settings,
  loaded,
  form,
  setForm,
  saving,
  gmailStatus,
  geminiStatus,
  refreshAuthStatuses,
  onSaveAll,
  onPartialSaved
}: Props) {
  // Partial save for Google account email
  const saveGoogleAccount = useCallback(async (email: string): Promise<boolean> => {
    try {
      const base = settings
      const payload = {
        userEmail: base?.userEmail || '',
        senderEmail: email,
        senderName: base?.senderName || '',
        geminiApiUrl: base?.geminiApiUrl || '',
        notificationChannel: base?.notificationChannel || 'Email',
        telegramBotToken: base?.telegramBotToken || null,
        telegramChatId: base?.telegramChatId || null
      }
      await svcUpdateSettings(payload)
      const latest = await svcGetSettings()
      setForm({
        userEmail: latest.userEmail || '',
        senderEmail: latest.senderEmail || '',
        senderName: latest.senderName || '',
        geminiApiUrl: latest.geminiApiUrl || '',
        notificationChannel: latest.notificationChannel || 'Email',
        telegramBotToken: latest.telegramBotToken || '',
        telegramChatId: latest.telegramChatId || ''
      })
      await refreshAuthStatuses()
      if (onPartialSaved) await onPartialSaved()
      return true
    } catch (e) {
      toast.error(String(e))
      return false
    }
  }, [settings, setForm, refreshAuthStatuses])

  // Partial save for Gemini API URL
  const saveGeminiUrl = useCallback(async (url: string): Promise<boolean> => {
    try {
      const base = settings
      const payload = {
        userEmail: base?.userEmail || '',
        senderEmail: base?.senderEmail || '',
        senderName: base?.senderName || '',
        geminiApiUrl: url,
        notificationChannel: base?.notificationChannel || 'Email',
        telegramBotToken: base?.telegramBotToken || null,
        telegramChatId: base?.telegramChatId || null
      }
      await svcUpdateSettings(payload)
      const latest = await svcGetSettings()
      setForm({
        userEmail: latest.userEmail || '',
        senderEmail: latest.senderEmail || '',
        senderName: latest.senderName || '',
        geminiApiUrl: latest.geminiApiUrl || '',
        notificationChannel: latest.notificationChannel || 'Email',
        telegramBotToken: latest.telegramBotToken || '',
        telegramChatId: latest.telegramChatId || ''
      })
      if (onPartialSaved) await onPartialSaved()
      return true
    } catch (e) {
      toast.error(String(e))
      return false
    }
  }, [settings, setForm])

  return (
    <div className="space-y-6">
      <GoogleAuthorizationCard
        settings={settings}
        form={form}
        setForm={setForm}
        gmailStatus={gmailStatus}
        geminiStatus={geminiStatus}
        savingGoogle={saving}
        onSaveGoogleAccount={saveGoogleAccount}
      />
      <NotificationSettingsCard
        settings={settings}
        loaded={loaded}
        form={form}
        setForm={setForm}
        saving={saving}
        onSave={onSaveAll}
        gmailStatus={gmailStatus}
      />
      <GeminiApiUrlCard
        settings={settings}
        savingGemini={saving}
        onSaveGeminiUrl={saveGeminiUrl}
      />
    </div>
  )
}
