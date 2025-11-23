import { useState, useEffect, useMemo } from 'react'
import { Settings } from '../../types'

interface Props {
	settings: Settings | null
	onSaveGeminiUrl: (url: string) => Promise<boolean>
	savingGemini: boolean
}

export default function GeminiApiUrlCard({ settings, onSaveGeminiUrl, savingGemini }: Props) {
	const [editing, setEditing] = useState(false)
	const [localUrl, setLocalUrl] = useState(settings?.geminiApiUrl || '')
	const [original, setOriginal] = useState(settings?.geminiApiUrl || '')

	useEffect(() => {
		if (!editing) {
			setLocalUrl(settings?.geminiApiUrl || '')
			setOriginal(settings?.geminiApiUrl || '')
		}
	}, [settings, editing])

	function startEdit() {
		setOriginal(localUrl)
		setEditing(true)
	}
	function cancel() {
		setLocalUrl(original)
		setEditing(false)
	}
	async function save() {
		const ok = await onSaveGeminiUrl(localUrl.trim())
		if (ok) {
			setEditing(false)
		}
	}
	const changed = useMemo(() => editing && original !== localUrl, [editing, original, localUrl])

	return (
		<section className="card">
			<div className="flex items-center justify-between mb-4">
				<h2 className="text-lg font-semibold">Gemini API URL</h2>
				{!editing && (
					<button className="btn-primary px-3 py-2" onClick={startEdit} disabled={savingGemini}>Edit</button>
				)}
			</div>
			<div className="space-y-4 text-sm">
				<p className="text-gray-600 dark:text-gray-300">Base endpoint used when calling Gemini models.</p>
				<label className={"text-sm flex flex-col relative " + (changed ? 'after:absolute after:-right-2 after:top-2 after:w-2 after:h-2 after:rounded-full after:bg-amber-400' : '')}>
					<span className="text-gray-700 dark:text-gray-200 flex items-center gap-1">API base URL {changed && <span className="text-amber-600 dark:text-amber-400 text-[10px] font-medium">Changed</span>}</span>
					<input
						className={
							"modal-input transition-colors " +
							(editing ? '' : 'bg-gray-50 text-gray-600 border border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-500') +
							(changed ? ' ring-2 ring-amber-300 dark:ring-amber-500' : '')
						}
						type="text"
						value={localUrl}
						onChange={e => setLocalUrl(e.target.value)}
						placeholder="https://generativelanguage.googleapis.com"
						disabled={!editing || savingGemini}
					/>
				</label>
				{editing && (
					<div className="flex justify-end gap-3 pt-2">
						<button className="btn-secondary px-3 py-2" onClick={cancel} disabled={savingGemini}>Cancel</button>
						<button className="btn-primary px-3 py-2" onClick={save} disabled={savingGemini || !localUrl.trim() || !changed}>Save</button>
					</div>
				)}
			</div>
		</section>
	)
}
