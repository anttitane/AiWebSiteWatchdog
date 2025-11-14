import Swal, { SweetAlertIcon } from 'sweetalert2'

export async function confirmDialog(options: {
  title?: string
  text?: string
  confirmText?: string
  cancelText?: string
  icon?: SweetAlertIcon
} = {}): Promise<boolean> {
  const {
    title = 'Are you sure?',
    text = 'This action cannot be undone.',
    confirmText = 'Confirm',
    cancelText = 'Cancel',
    icon = 'warning'
  } = options

  const res = await Swal.fire({
    title,
    text,
    icon,
    showCancelButton: true,
    confirmButtonText: confirmText,
    cancelButtonText: cancelText,
    confirmButtonColor: '#4f46e5', // indigo-600
    cancelButtonColor: '#6b7280',  // gray-500
    reverseButtons: true,
    focusCancel: true
  })
  return res.isConfirmed === true
}
