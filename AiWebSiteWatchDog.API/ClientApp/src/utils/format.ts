function parseDate(value: string | number | Date): Date {
  if (typeof value === 'string') {
    const hasTz = /[zZ]$|[+-]\d{2}:?\d{2}$/.test(value)
    if (!hasTz && /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(value)) {
      return new Date(value + 'Z')
    }
  }
  return new Date(value)
}

export function formatDateTime(value: string | number | Date): string {
  const d = parseDate(value)
  const pad = (n: number) => String(n).padStart(2, '0')
  const day = pad(d.getDate())
  const month = pad(d.getMonth() + 1)
  const year = d.getFullYear()
  const hour = pad(d.getHours())
  const minute = pad(d.getMinutes())
  const second = pad(d.getSeconds())
  // DD.MM.YYYY HH:MM:SS (24h)
  return `${day}.${month}.${year} ${hour}:${minute}:${second}`
}

export function formatRelative(value: string | number | Date): string {
  const now = Date.now()
  const t = parseDate(value).getTime()
  if (isNaN(t)) return ''
  const deltaMs = t - now
  const future = deltaMs > 0
  const diffMs = Math.abs(deltaMs)
  const s = Math.floor(diffMs / 1000)
  if (s < 30) return 'just now'
  if (s < 60) return future ? `in ${s}s` : `${s}s ago`
  const m = Math.floor(s / 60)
  if (m < 60) return future ? `in ${m}m` : `${m}m ago`
  const h = Math.floor(m / 60)
  if (h < 24) return future ? `in ${h}h` : `${h}h ago`
  const d = Math.floor(h / 24)
  if (d < 7) return future ? `in ${d}d` : `${d}d ago`
  // For more than a week, fall back to date/time
  return formatDateTime(value)
}
