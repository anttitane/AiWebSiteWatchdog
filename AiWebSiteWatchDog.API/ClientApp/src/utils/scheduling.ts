import parser from 'cron-parser'

export function getNextRunDate(cronExpr: string, from: Date = new Date()): Date | null {
  try {
    const interval = parser.parseExpression(cronExpr, { currentDate: from })
    const next = interval.next()
    return next.toDate()
  } catch {
    return null
  }
}

export function findNextScheduledTask<T extends { schedule?: string; title?: string }>(tasks: T[]): { task: T; nextDate: Date } | null {
  let best: { task: T; nextDate: Date } | null = null
  const now = new Date()
  for (const t of tasks) {
    if (!t.schedule) continue
    const nd = getNextRunDate(t.schedule, now)
    if (!nd) continue
    if (!best || nd < best.nextDate) best = { task: t, nextDate: nd }
  }
  return best
}
