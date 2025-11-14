import React from 'react'
import { useModalAnimation } from '../../hooks/useModalAnimation'

type Props = {
  open: boolean
  title?: string
  message?: string
  confirmText?: string
  cancelText?: string
  onConfirm: () => void
  onCancel: () => void
}

export default function ConfirmDialog({ open, title = 'Are you sure?', message = 'This action cannot be undone.', confirmText = 'Confirm', cancelText = 'Cancel', onConfirm, onCancel }: Props) {
  const { mounted, leaving } = useModalAnimation(open)
  if (!mounted) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className={"absolute inset-0 bg-black/40 " + (leaving ? 'modal-anim-overlay-out' : 'modal-anim-overlay')} onClick={onCancel} />
      <div className="relative z-10 w-full max-w-md mx-auto">
        <div className={("bg-white dark:bg-gray-800 rounded-lg shadow-xl border border-gray-200 dark:border-gray-700 p-6 ") + (leaving ? 'modal-anim-panel-out' : 'modal-anim-panel')}>
          <h3 className="text-lg font-semibold mb-2">{title}</h3>
          <p className="text-sm text-gray-600 dark:text-gray-300 mb-6">{message}</p>
          <div className="flex justify-end gap-3">
            <button className="btn-secondary px-3 py-2" onClick={onCancel}>{cancelText}</button>
            <button className="btn-primary px-3 py-2" onClick={onConfirm}>{confirmText}</button>
          </div>
        </div>
      </div>
    </div>
  )
}
