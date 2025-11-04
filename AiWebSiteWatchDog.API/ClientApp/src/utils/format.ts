export function formatDateTime(value: string | number | Date): string {
  const d = new Date(value)
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
