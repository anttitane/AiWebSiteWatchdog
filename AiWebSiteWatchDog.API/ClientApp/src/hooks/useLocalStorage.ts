import { useEffect, useState } from 'react'

export function useLocalStorageBoolean(key: string, defaultValue = false) {
  // Read synchronously on first render to avoid flicker
  const [value, setValue] = useState<boolean>(() => {
    try {
      const raw = localStorage.getItem(key)
      if (raw !== null) return raw === '1' || raw === 'true'
    } catch {
      // ignore (e.g., storage disabled)
    }
    return defaultValue
  })

  // Persist on change
  useEffect(() => {
    try {
      localStorage.setItem(key, value ? '1' : '0')
    } catch {
      // ignore
    }
  }, [key, value])

  return [value, setValue] as const
}

export function useLocalStorageString<T extends string>(key: string, defaultValue: T) {
  const [value, setValue] = useState<T>(() => {
    try {
      const raw = localStorage.getItem(key)
      if (raw !== null) return raw as T
    } catch {}
    return defaultValue
  })

  useEffect(() => {
    try {
      localStorage.setItem(key, value)
    } catch {}
  }, [key, value])

  return [value, setValue] as const
}
