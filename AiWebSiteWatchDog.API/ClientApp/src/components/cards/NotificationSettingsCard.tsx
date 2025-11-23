import { Settings, SettingsForm, GmailStatus } from '../../types'
import { useState, useEffect, useMemo } from 'react'
import { sendTestNotification } from '../../services/notifications'

interface Props {
  settings: Settings | null
  loaded: boolean
  form: SettingsForm
  setForm: React.Dispatch<React.SetStateAction<SettingsForm>>
  saving: boolean
  onSave: () => Promise<boolean>
  gmailStatus: GmailStatus | null
}

export default function NotificationSettingsCard({ settings, loaded, form, setForm, saving, onSave, gmailStatus }: Props) {
  const [editing, setEditing] = useState(false)
  const [original, setOriginal] = useState<SettingsForm | null>(null)

  useEffect(() => {
    if (!editing && settings) {
      setForm({
        userEmail: settings.userEmail || '',
        senderEmail: settings.senderEmail || '',
        senderName: settings.senderName || '',
        geminiApiUrl: settings.geminiApiUrl || '',
        notificationChannel: settings.notificationChannel || 'Email',
        telegramBotToken: settings.telegramBotToken || '',
        telegramChatId: settings.telegramChatId || ''
      })
    }
  }, [settings, editing, setForm])

  function startEdit() {
    setOriginal({ ...form })
    setEditing(true)
  }
  function cancelEdit() {
    if (original) {
      setForm(original)
    } else if (settings) {
      setForm({
        userEmail: settings.userEmail || '',
        senderEmail: settings.senderEmail || '',
        senderName: settings.senderName || '',
        geminiApiUrl: settings.geminiApiUrl || '',
        notificationChannel: settings.notificationChannel || 'Email',
        telegramBotToken: settings.telegramBotToken || '',
        telegramChatId: settings.telegramChatId || ''
      })
    }
    setEditing(false)
    setOriginal(null)
  }
  async function saveEdit() {
    const ok = await onSave()
    if (ok) {
      setEditing(false)
      setOriginal(null)
    }
  }

  const changed = useMemo(() => {
    if (!editing || !original) return {
      userEmail: false,
      senderName: false,
      notificationChannel: false,
      telegramBotToken: false,
      telegramChatId: false
    }
    return {
      userEmail: form.userEmail !== original.userEmail,
      senderName: form.senderName !== original.senderName,
      notificationChannel: form.notificationChannel !== original.notificationChannel,
      telegramBotToken: (form.telegramBotToken || '') !== (original.telegramBotToken || ''),
      telegramChatId: (form.telegramChatId || '') !== (original.telegramChatId || '')
    }
  }, [editing, original, form])

  const channelIsTelegram = form.notificationChannel === 'Telegram'
  const channelIsEmail = form.notificationChannel === 'Email'
  const emailValid = channelIsEmail ? !!form.userEmail && !!form.senderEmail : true
  const telegramValid = channelIsTelegram ? !!form.telegramBotToken && !!form.telegramChatId : true
  const canSave = emailValid && telegramValid

  const [testSending, setTestSending] = useState(false)
  const [testMsg, setTestMsg] = useState<string | null>(null)
  async function handleTestNotification() {
    setTestSending(true)
    setTestMsg(null)
    try {
      await sendTestNotification({
        subject: 'Test Notification',
        message: `This is a test via ${form.notificationChannel}`
      })
      setTestMsg('Test notification sent')
      setTimeout(() => setTestMsg(null), 2500)
    } catch (e) {
      setTestMsg(String(e))
    } finally {
      setTestSending(false)
    }
  }

  return (
    <section className="card">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold">Notification Settings</h2>
        <div className="flex items-center gap-3">
          {!editing && (
            <button className="btn-primary px-3 py-2" onClick={startEdit} disabled={saving || !loaded}>
              Edit
            </button>
          )}
        </div>
      </div>
      <div className="text-xs mb-3 text-gray-600 dark:text-gray-300 space-y-1">
        <p>Select how notifications are delivered.</p>
        <p><strong>Email</strong>: requires Google authorization with Gmail send scope (see banner above if missing).</p>
        <p><strong>Telegram</strong>: uses your bot token & chat ID; no Google Gmail permission needed.</p>
        {channelIsTelegram && (
          <p className="text-[11px] text-gray-500 dark:text-gray-400">Note: Google account email is still required for Gemini API authorization even when using Telegram for notifications.</p>
        )}
      </div>
      <div className={"space-y-4 text-sm " + (editing ? '' : 'pointer-events-none')}>
        <fieldset className={"text-sm flex flex-col gap-2 relative " + (changed.notificationChannel ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
          <legend className="text-gray-700 dark:text-gray-200 flex items-center gap-1">
            Notification Channel {changed.notificationChannel && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}
          </legend>
          <ul className="rounded-md border border-gray-200 dark:border-gray-600 divide-y divide-gray-200 dark:divide-gray-700 overflow-hidden">
            {['Email','Telegram'].map(ch => (
              <li key={ch}>
                <label className={
                  "flex items-center gap-3 px-3 py-2 cursor-pointer " +
                  (form.notificationChannel === ch
                    ? 'bg-indigo-50 dark:bg-indigo-900/30'
                    : (editing ? 'hover:bg-gray-50 dark:hover:bg-gray-700' : ''))
                }>
                  <input
                    type="radio"
                    name="notificationChannel"
                    value={ch}
                    className="shrink-0"
                    disabled={saving || !editing}
                    checked={form.notificationChannel === ch}
                    onChange={e => setForm(f => ({ ...f, notificationChannel: e.target.value as any }))}
                  />
                  <span className="text-gray-700 dark:text-gray-200 text-xs font-medium">{ch}</span>
                </label>
              </li>
            ))}
          </ul>
        </fieldset>
        {editing && channelIsEmail && (
          <div className="space-y-4">
            <label className={"text-sm flex flex-col relative " + (changed.senderName ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
              <span className="text-gray-700 dark:text-gray-200 flex items-center gap-1">
                Sender name {changed.senderName && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}
              </span>
              <input
                className={
                  "modal-input transition-colors " +
                  (editing ? '' : 'bg-gray-50 text-gray-600 border border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-500') +
                  (changed.senderName ? ' ring-2 ring-amber-300 dark:ring-amber-500' : '')
                }
                type="text"
                value={form.senderName}
                onChange={e => setForm(f => ({ ...f, senderName: e.target.value }))}
                placeholder="Ai Watchdog"
                disabled={saving || !editing}
              />
              <span className="text-[11px] text-gray-500 dark:text-gray-400 mt-1">Sender name in the notification email.</span>
            </label>
            <label className={"text-sm flex flex-col relative " + (changed.userEmail ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
              <span className="text-gray-700 dark:text-gray-200 flex items-center gap-1">
                Recipient email {changed.userEmail && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}
              </span>
              <input
                className={
                  "modal-input transition-colors " +
                  (editing ? '' : 'bg-gray-50 text-gray-600 border border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-500') +
                  (changed.userEmail ? ' ring-2 ring-amber-300 dark:ring-amber-500' : '')
                }
                type="email"
                value={form.userEmail}
                onChange={e => setForm(f => ({ ...f, userEmail: e.target.value }))}
                placeholder="recipient@example.com"
                disabled={saving || !editing}
              />
              <span className="text-[11px] text-gray-500 dark:text-gray-400 mt-1">Used only when sending Email notifications.</span>
            </label>
          </div>
        )}
        {!editing && channelIsEmail && (
          <div className="space-y-3">
            <div className="flex flex-col">
              <span className="text-xs font-medium text-gray-600 dark:text-gray-300">Sender name</span>
              <div className="mt-1 px-3 py-2 rounded-md border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 text-sm text-gray-700 dark:text-gray-200">
                {form.senderName || <span className="italic text-gray-400">(none)</span>}
              </div>
            </div>
            <div className="flex flex-col">
              <span className="text-xs font-medium text-gray-600 dark:text-gray-300">Recipient email</span>
              <div className="mt-1 px-3 py-2 rounded-md border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 text-sm text-gray-700 dark:text-gray-200">
                {form.userEmail || <span className="italic text-gray-400">(none)</span>}
              </div>
            </div>
          </div>
        )}
        {channelIsTelegram && (
          <>
            <label className={"text-sm flex flex-col relative " + (changed.telegramBotToken ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
              <span className="text-gray-700 dark:text-gray-200 flex items-center gap-1">
                Telegram Bot Token {changed.telegramBotToken && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}
              </span>
              <input
                className={
                  "modal-input font-mono text-xs transition-colors " +
                  (editing ? '' : 'bg-gray-50 text-gray-600 border border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-500') +
                  (changed.telegramBotToken ? ' ring-2 ring-amber-300 dark:ring-amber-500' : '')
                }
                type="text"
                value={form.telegramBotToken || ''}
                onChange={e => setForm(f => ({ ...f, telegramBotToken: e.target.value }))}
                placeholder="123456789:ABC-DEF1234ghIkl-zyx57W2v1u123ew11"
                disabled={saving || !editing}
              />
            </label>
            <label className={"text-sm flex flex-col relative " + (changed.telegramChatId ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
              <span className="text-gray-700 dark:text-gray-200 flex items-center gap-1">
                Telegram Chat ID {changed.telegramChatId && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}
              </span>
              <input
                className={
                  "modal-input font-mono text-xs transition-colors " +
                  (editing ? '' : 'bg-gray-50 text-gray-600 border border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-500') +
                  (changed.telegramChatId ? ' ring-2 ring-amber-300 dark:ring-amber-500' : '')
                }
                type="text"
                value={form.telegramChatId || ''}
                onChange={e => setForm(f => ({ ...f, telegramChatId: e.target.value }))}
                placeholder="123456789"
                disabled={saving || !editing}
              />
            </label>
          </>
        )}
        {!canSave && editing && (
          <p className="text-xs text-amber-600 dark:text-amber-400">
            {channelIsEmail && !emailValid && 'Recipient email and Google account are required for Email channel.'}
            {channelIsTelegram && !telegramValid && 'Bot Token and Chat ID are required for Telegram channel.'}
          </p>
        )}
        {editing && (
          <div className="flex justify-end gap-3 pt-2">
            <button className="btn-secondary px-3 py-2" onClick={cancelEdit} disabled={saving}>Cancel</button>
            <button className="btn-primary px-3 py-2" onClick={saveEdit} disabled={saving || !canSave}>Save</button>
          </div>
        )}
      </div>
      {!editing && settings && (
        <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
          <div className="flex items-center gap-3 flex-wrap">
            <button
              className="btn-secondary px-3 py-2"
              disabled={(() => {
                if (testSending || !settings) return true
                if (settings.notificationChannel === 'Email') {
                  const gmailAuthorized = gmailStatus && gmailStatus.configured && gmailStatus.hasGmailScope && !gmailStatus.needsReauth
                  return !(settings.senderEmail && settings.userEmail && gmailAuthorized)
                } else if (settings.notificationChannel === 'Telegram') {
                  return !(settings.telegramBotToken && settings.telegramChatId)
                }
                return true
              })()}
              onClick={handleTestNotification}
            >
              {testSending ? 'Sending…' : 'Send Test Notification'}
            </button>
            {settings.notificationChannel === 'Email' && gmailStatus && (!gmailStatus.hasGmailScope || gmailStatus.needsReauth) && (
              <span className="text-[10px] text-amber-600 dark:text-amber-400">Gmail authorization required.</span>
            )}
            {testMsg && <span className="text-xs font-medium text-gray-700 dark:text-gray-200">{testMsg}</span>}
          </div>
        </div>
      )}
    </section>
  )
}
