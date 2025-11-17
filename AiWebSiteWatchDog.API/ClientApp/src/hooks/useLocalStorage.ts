import { useEffect, useState } from 'react'

export function useLocalStorageBoolean(key: string, defaultValue = false) {
  // Read synchronously on first render to avoid flicker
  const [value, setValue] = useState<boolean>(() => {
    try {
      const raw = localStorage.getItem(key)
      if (raw !== null) {
        // Prefer 'true'/'false'; keep backward compatibility for '1'/'0'
        if (raw === 'true') return true
        if (raw === 'false') return false
        if (raw === '1') return true
        if (raw === '0') return false
      }
    } catch {
      // ignore (e.g., storage disabled)
    }
    return defaultValue
  })

  // Persist on change
  useEffect(() => {
    try {
      // Standardize to 'true'/'false' for readability
      localStorage.setItem(key, value ? 'true' : 'false')
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
