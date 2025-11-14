import { useEffect, useState } from 'react'

export function useModalAnimation(open: boolean, durationMs = 180) {
  const [mounted, setMounted] = useState(open)
  const [leaving, setLeaving] = useState(false)

  useEffect(() => {
    if (open) {
      setMounted(true)
      setLeaving(false)
      return
    }
    if (mounted) {
      setLeaving(true)
      const t = setTimeout(() => {
        setMounted(false)
        setLeaving(false)
      }, durationMs)
      return () => clearTimeout(t)
    }
  }, [open, mounted, durationMs])

  return { mounted, leaving }
}
