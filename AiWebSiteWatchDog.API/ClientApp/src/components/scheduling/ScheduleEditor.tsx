import { useEffect, useMemo, useState } from 'react'
import cronstrue from 'cronstrue'
import { getNextRunDate } from '../../utils/scheduling'
import { formatDateTime } from '../../utils/format'

export type ScheduleKind = 'every-n-minutes' | 'every-n-hours' | 'daily' | 'weekly' | 'monthly'

type Props = {
  value: string | undefined
  onChange: (cron: string) => void
  onValidityChange?: (valid: boolean) => void
}

export default function ScheduleEditor({ value, onChange, onValidityChange }: Props) {
  const parsed = useMemo(() => parseSimpleFromCron(value || ''), [value])
  const [mode, setMode] = useState<'simple' | 'advanced'>(() => (parsed ? 'simple' : (value ? 'advanced' : 'simple')))
  const [kind, setKind] = useState<ScheduleKind>(() => parsed?.kind ?? 'every-n-minutes')
  const [nMinutes, setNMinutes] = useState<number>(() => parsed?.nMinutes ?? 15)
  const [nHours, setNHours] = useState<number>(() => parsed?.nHours ?? 1)
  const [atMinute, setAtMinute] = useState<number>(() => parsed?.atMinute ?? 0)
  const [atHour, setAtHour] = useState<number>(() => parsed?.atHour ?? 9)
  const [daysOfWeek, setDaysOfWeek] = useState<number[]>(() => parsed?.daysOfWeek ?? [1,2,3,4,5]) // Mon-Fri
  const [dayOfMonth, setDayOfMonth] = useState<number>(() => parsed?.dayOfMonth ?? 1)

  useEffect(() => {
    if (mode !== 'simple') return
    let cron = '* * * * *'
    switch (kind) {
      case 'every-n-minutes':
        cron = `*/${clamp(nMinutes,1,59)} * * * *`
        break
      case 'every-n-hours':
        cron = `${clamp(atMinute,0,59)} */${clamp(nHours,1,23)} * * *`
        break
      case 'daily':
        cron = `${clamp(atMinute,0,59)} ${clamp(atHour,0,23)} * * *`
        break
      case 'weekly':
        const dows = daysOfWeek.slice().sort((a,b)=>a-b).join(',') || '*'
        cron = `${clamp(atMinute,0,59)} ${clamp(atHour,0,23)} * * ${dows}`
        break
      case 'monthly':
        cron = `${clamp(atMinute,0,59)} ${clamp(atHour,0,23)} ${clamp(dayOfMonth,1,31)} * *`
        break
    }
    if ((value || '') !== cron) {
      onChange(cron)
    }
  }, [mode, kind, nMinutes, nHours, atMinute, atHour, daysOfWeek, dayOfMonth, value])

  const human = useMemo(() => {
    const cron = value || '* * * * *'
    try { return cronstrue.toString(cron, { use24HourTimeFormat: true }) } catch { return 'Invalid cron' }
  }, [value])

  const nextRun = useMemo(() => {
    const cron = value || '* * * * *'
    const nd = getNextRunDate(cron)
    return nd ? nd.toISOString() : null
  }, [value])

  const advancedInvalid = useMemo(() => {
    if (mode !== 'advanced') return false
    const v = (value || '').trim()
    if (!v) return false
    return getNextRunDate(v) === null
  }, [mode, value])

  const isValid = useMemo(() => {
    return mode === 'simple' || !advancedInvalid
  }, [mode, advancedInvalid])

  useEffect(() => {
    if (typeof onValidityChange === 'function') {
      onValidityChange(isValid)
    }
  }, [isValid, onValidityChange])

  function toggleDow(d: number) {
    setDaysOfWeek(prev => prev.includes(d) ? prev.filter(x => x!==d) : [...prev, d])
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-3 text-sm">
        <label className="inline-flex items-center gap-2">
          <input type="radio" name="sched-mode" checked={mode==='simple'} onChange={()=>setMode('simple')} />
          Simple
        </label>
        <label className="inline-flex items-center gap-2">
          <input type="radio" name="sched-mode" checked={mode==='advanced'} onChange={()=>setMode('advanced')} />
          Advanced (cron)
        </label>
      </div>

      {mode==='simple' ? (
        <div className="space-y-3">
          <div className="grid sm:grid-cols-2 gap-3">
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Pattern</span>
              <select className="modal-input" value={kind} onChange={e=>setKind(e.target.value as ScheduleKind)}>
                <option value="every-n-minutes">Every N minutes</option>
                <option value="every-n-hours">Every N hours at minute</option>
                <option value="daily">Daily at time</option>
                <option value="weekly">Weekly on days at time</option>
                <option value="monthly">Monthly on day at time</option>
              </select>
            </label>
            {kind==='every-n-minutes' && (
              <label className="text-sm">
                <span className="text-gray-700 dark:text-gray-200">Every</span>
                <input className="modal-input" type="number" min={1} max={59} value={nMinutes} onChange={e=>setNMinutes(Number(e.target.value)||1)} />
              </label>
            )}
            {kind==='every-n-hours' && (
              <>
                <label className="text-sm">
                  <span className="text-gray-700 dark:text-gray-200">Every hours</span>
                  <input className="modal-input" type="number" min={1} max={23} value={nHours} onChange={e=>setNHours(Number(e.target.value)||1)} />
                </label>
                <label className="text-sm">
                  <span className="text-gray-700 dark:text-gray-200">At minute</span>
                  <input className="modal-input" type="number" min={0} max={59} value={atMinute} onChange={e=>setAtMinute(Number(e.target.value)||0)} />
                </label>
              </>
            )}
            {(kind==='daily' || kind==='weekly' || kind==='monthly') && (
              <>
                <label className="text-sm">
                  <span className="text-gray-700 dark:text-gray-200">Hour (0-23)</span>
                  <input className="modal-input" type="number" min={0} max={23} value={atHour} onChange={e=>setAtHour(Number(e.target.value)||0)} />
                </label>
                <label className="text-sm">
                  <span className="text-gray-700 dark:text-gray-200">Minute (0-59)</span>
                  <input className="modal-input" type="number" min={0} max={59} value={atMinute} onChange={e=>setAtMinute(Number(e.target.value)||0)} />
                </label>
              </>
            )}
          </div>

          {kind==='weekly' && (
            <div className="text-sm">
              <div className="text-gray-700 dark:text-gray-200 mb-1">Days of week</div>
              <div className="flex flex-wrap gap-2">
                {[
                  { label: 'Mon', value: 1 },
                  { label: 'Tue', value: 2 },
                  { label: 'Wed', value: 3 },
                  { label: 'Thu', value: 4 },
                  { label: 'Fri', value: 5 },
                  { label: 'Sat', value: 6 },
                  { label: 'Sun', value: 0 },
                ].map((d)=>(
                  <label key={d.value} className={"inline-flex items-center gap-2 px-2 py-1 rounded-md border cursor-pointer " + (daysOfWeek.includes(d.value)?'bg-indigo-50 dark:bg-indigo-500/20 border-indigo-300 dark:border-indigo-400':'border-gray-300 dark:border-gray-600') }>
                    <input type="checkbox" checked={daysOfWeek.includes(d.value)} onChange={()=>toggleDow(d.value)} />
                    {d.label}
                  </label>
                ))}
              </div>
            </div>
          )}

          {kind==='monthly' && (
            <label className="text-sm">
              <span className="text-gray-700 dark:text-gray-200">Day of month (1-31)</span>
              <input className="modal-input" type="number" min={1} max={31} value={dayOfMonth} onChange={e=>setDayOfMonth(Number(e.target.value)||1)} />
            </label>
          )}
        </div>
      ) : (
        <div className="text-sm">
          <label className="block">
            <span className="text-gray-700 dark:text-gray-200">Cron expression</span>
            <input
              className={("modal-input font-mono text-xs ") + (advancedInvalid ? 'border-red-500 focus:ring-red-500 focus:border-red-500' : '')}
              type="text"
              value={value || ''}
              onChange={e=>onChange(e.target.value)}
              placeholder="*/15 * * * *"
              aria-invalid={advancedInvalid || undefined}
            />
          </label>
          {advancedInvalid && (
            <p className="mt-1 text-xs text-red-600 dark:text-red-400">Invalid cron expression. Use 5 fields (min hour day month dow) or switch to Simple.</p>
          )}
        </div>
      )}

      <div className="text-xs text-gray-600 dark:text-gray-300">
        <div>Result: <span className="font-mono">{value || ''}</span></div>
        <div>Description: {human}</div>
        <div>Next run: {nextRun ? formatDateTime(nextRun) : (mode==='advanced' && advancedInvalid ? 'invalid' : 'n/a')}</div>
      </div>
    </div>
  )
}

function clamp(v: number, min: number, max: number) {
  return Math.max(min, Math.min(max, Math.floor(v)))
}

// Try to map a 5-field (or 6-field with seconds) cron into one of our simple presets
function parseSimpleFromCron(cron: string): (Partial<{
  kind: ScheduleKind
  nMinutes: number
  nHours: number
  atMinute: number
  atHour: number
  daysOfWeek: number[]
  dayOfMonth: number
}>) | null {
  if (!cron) return null
  const parts = cron.trim().split(/\s+/)
  const tokens = parts.length === 6 ? parts.slice(1) : parts // drop seconds if present
  if (tokens.length !== 5) return null
  const [minF, hourF, domF, monF, dowF] = tokens

  // Every N minutes: */N * * * *
  const everyMin = minF.match(/^\*\/(\d{1,2})$/) && hourF==='*' && domF==='*' && monF==='*' && dowF==='*'
  if (everyMin) {
    const n = Number((minF.match(/^\*\/(\d{1,2})$/) as RegExpMatchArray)[1])
    if (n>=1 && n<=59) return { kind: 'every-n-minutes', nMinutes: n }
  }

  // Every N hours at M minutes: M */N * * *
  const everyHours = hourF.match(/^\*\/(\d{1,2})$/) && domF==='*' && monF==='*' && dowF==='*' && minF.match(/^\d{1,2}$/)
  if (everyHours) {
    const n = Number((hourF.match(/^\*\/(\d{1,2})$/) as RegExpMatchArray)[1])
    const m = Number(minF)
    if (n>=1 && n<=23 && m>=0 && m<=59) return { kind: 'every-n-hours', nHours: n, atMinute: m }
  }

  // Daily at time: M H * * *
  if (domF==='*' && monF==='*' && dowF==='*' && minF.match(/^\d{1,2}$/) && hourF.match(/^\d{1,2}$/)) {
    const m = Number(minF); const h = Number(hourF)
    if (m>=0&&m<=59&&h>=0&&h<=23) return { kind: 'daily', atMinute: m, atHour: h }
  }

  // Weekly on days at time: M H * * d1[,d2]
  if (domF==='*' && monF==='*' && minF.match(/^\d{1,2}$/) && hourF.match(/^\d{1,2}$/)) {
    if (/^([0-6])(,([0-6]))*$/.test(dowF)) {
      const m = Number(minF); const h = Number(hourF)
      if (m>=0&&m<=59&&h>=0&&h<=23) {
        const dows = dowF.split(',').map(x=>Number(x))
        return { kind: 'weekly', atMinute: m, atHour: h, daysOfWeek: dows }
      }
    }
  }

  // Monthly on day at time: M H D * *
  if (monF==='*' && dowF==='*' && minF.match(/^\d{1,2}$/) && hourF.match(/^\d{1,2}$/) && domF.match(/^\d{1,2}$/)) {
    const m = Number(minF); const h = Number(hourF); const d = Number(domF)
    if (m>=0&&m<=59&&h>=0&&h<=23&&d>=1&&d<=31) return { kind: 'monthly', atMinute: m, atHour: h, dayOfMonth: d }
  }

  return null
}
