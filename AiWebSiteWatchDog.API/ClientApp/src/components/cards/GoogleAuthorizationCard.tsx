import { useState, useEffect, useMemo } from 'react'
import { Settings, SettingsForm, GmailStatus, GeminiStatus } from '../../types'

interface Props {
	settings: Settings | null
	form: SettingsForm
	setForm: React.Dispatch<React.SetStateAction<SettingsForm>>
	gmailStatus: GmailStatus | null
	geminiStatus: GeminiStatus | null
	onSaveGoogleAccount: (email: string) => Promise<boolean>
	savingGoogle: boolean
}

export default function GoogleAuthorizationCard({
	settings,
	form,
	setForm,
	gmailStatus,
	geminiStatus,
	onSaveGoogleAccount,
	savingGoogle
}: Props) {
	const [editing, setEditing] = useState(false)
	const [localEmail, setLocalEmail] = useState(form.senderEmail || '')
	const [originalEmail, setOriginalEmail] = useState<string>('')
	const channelIsEmail = form.notificationChannel === 'Email'

	useEffect(() => {
		if (!editing && settings?.senderEmail) {
			setLocalEmail(settings.senderEmail)
		}
	}, [settings, editing])

	function startEdit() {
		setOriginalEmail(localEmail)
		setEditing(true)
	}
	function cancelEdit() {
		setLocalEmail(originalEmail)
		setEditing(false)
	}
	async function saveEdit() {
		const ok = await onSaveGoogleAccount(localEmail.trim())
		if (ok) {
			setForm(f => ({ ...f, senderEmail: localEmail.trim() }))
			setEditing(false)
		}
	}

	function openConsent() {
		const em = (localEmail || '').trim()
		if (!em) return
		window.open(`/auth/start?senderEmail=${encodeURIComponent(em)}`, '_blank')
	}

	const changed = useMemo(() => editing && originalEmail !== localEmail, [editing, originalEmail, localEmail])

	return (
		<section className="card">
			<div className="flex items-center justify-between mb-4">
				<h2 className="text-lg font-semibold flex items-center gap-2">Google Authorization</h2>
				{!editing && (
					<button className="btn-primary px-3 py-2" onClick={startEdit} disabled={savingGoogle}>
						Edit
					</button>
				)}
			</div>
			<div className="space-y-4 text-sm">
				<p className="text-gray-600 dark:text-gray-300">
					{channelIsEmail ? (
						<>Manage Google account for sending emails and Gemini access. Authorize after saving to grant required scopes.</>
					) : (
						<>Manage Google account for Gemini access. (Email sending scope requested only if you switch to Email channel.)</>
					)}
				</p>
				<label className={"text-sm flex flex-col relative " + (changed ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
					<span className="text-gray-700 dark:text-gray-200 flex items-center gap-1">
						Google account (email) {changed && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}
					</span>
					<input
						className={
							"modal-input transition-colors " +
							(editing ? '' : 'bg-gray-50 text-gray-600 border border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-500') +
							(changed ? ' ring-2 ring-amber-300 dark:ring-amber-500' : '')
						}
						type="email"
						value={localEmail}
						onChange={e => setLocalEmail(e.target.value)}
						placeholder="you@example.com"
						disabled={!editing || savingGoogle}
					/>
				</label>

				{channelIsEmail && gmailStatus && (
					<>
						{!gmailStatus.configured && (
							<div className="rounded-md bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-700 p-3 text-xs text-amber-800 dark:text-amber-200 flex flex-wrap items-center gap-3">
								<span>Email sending not yet authorized with Google.</span>
								<button className="btn-secondary px-2 py-1" onClick={openConsent} disabled={!localEmail.trim()}>Authorize</button>
							</div>
						)}
						{gmailStatus.configured && gmailStatus.needsReauth && (
							<div className="rounded-md bg-amber-50 dark:bg-amber-900/30 border border-amber-300 dark:border-amber-700 p-3 text-xs text-amber-800 dark:text-amber-200 flex flex-wrap items-center gap-3">
								<span>Authorization required to enable sending emails (missing Gmail scope).</span>
								<button className="btn-secondary px-2 py-1" onClick={openConsent} disabled={!localEmail.trim()}>Authorize</button>
							</div>
						)}
						{gmailStatus.configured && !gmailStatus.needsReauth && gmailStatus.hasGmailScope && (
							<div className="rounded-md bg-green-50 dark:bg-green-900/30 border border-green-200 dark:border-green-700 p-3 text-xs text-green-800 dark:text-green-200">Email sending authorized.</div>
						)}
					</>
				)}

				{geminiStatus && (
					<>
						{!geminiStatus.hasGeminiScope && (
							<div className="rounded-md bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-700 p-3 text-xs text-amber-800 dark:text-amber-200 flex flex-wrap items-center gap-3">
								<span>Gemini API access not authorized yet.</span>
								<button className="btn-secondary px-2 py-1" onClick={openConsent} disabled={!localEmail.trim()}>Authorize</button>
							</div>
						)}
						{geminiStatus.hasGeminiScope && (
							<div className="rounded-md bg-green-50 dark:bg-green-900/30 border border-green-200 dark:border-green-700 p-3 text-xs text-green-800 dark:text-green-200">Gemini API access authorized.</div>
						)}
					</>
				)}

				{editing && (
					<div className="flex justify-end gap-3 pt-2">
						<button className="btn-secondary px-3 py-2" onClick={cancelEdit} disabled={savingGoogle}>Cancel</button>
						<button className="btn-primary px-3 py-2" onClick={saveEdit} disabled={savingGoogle || !localEmail.trim() || !changed}>Save</button>
					</div>
				)}
				{!editing && (
					<div className="flex justify-end gap-3 pt-2">
						<button
							className="btn-secondary px-3 py-2 disabled:opacity-50"
							onClick={openConsent}
							disabled={!localEmail.trim()}
						>
							Open Google consent
						</button>
					</div>
				)}
			</div>
		</section>
	)
}
