import type { ScheduleEntry } from './api'

export const HOURS_IN_DAY = 24
export const HOUR_HEIGHT = 56
export const DEFAULT_EVENT_MINUTES = 60
export const MIN_EVENT_MINUTES = 15

const WEEKDAYS_SHORT = ['вс', 'пн', 'вт', 'ср', 'чт', 'пт', 'сб']
export const WEEKDAY_LABELS = ['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'вс']
const WEEKDAYS_LONG = [
  'воскресенье',
  'понедельник',
  'вторник',
  'среда',
  'четверг',
  'пятница',
  'суббота',
]
const MONTHS_GENITIVE = [
  'января',
  'февраля',
  'марта',
  'апреля',
  'мая',
  'июня',
  'июля',
  'августа',
  'сентября',
  'октября',
  'ноября',
  'декабря',
]
const MONTHS_NOMINATIVE = [
  'январь',
  'февраль',
  'март',
  'апрель',
  'май',
  'июнь',
  'июль',
  'август',
  'сентябрь',
  'октябрь',
  'ноябрь',
  'декабрь',
]

export interface DateRange {
  from: string
  to: string
}

function pad2(value: number): string {
  return String(value).padStart(2, '0')
}

export function toDateString(date: Date): string {
  return `${date.getFullYear()}-${pad2(date.getMonth() + 1)}-${pad2(date.getDate())}`
}

export function parseDateString(value: string): Date {
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year, month - 1, day)
}

export function startOfToday(): Date {
  const now = new Date()
  return new Date(now.getFullYear(), now.getMonth(), now.getDate())
}

export function addDays(date: Date, days: number): Date {
  const next = new Date(date)
  next.setDate(next.getDate() + days)
  return next
}

export function addMonths(date: Date, months: number): Date {
  return new Date(date.getFullYear(), date.getMonth() + months, 1)
}

/** Неделя начинается с понедельника. */
export function startOfWeek(date: Date): Date {
  const offset = (date.getDay() + 6) % 7
  return addDays(new Date(date.getFullYear(), date.getMonth(), date.getDate()), -offset)
}

export function weekDays(anchor: Date): Date[] {
  const start = startOfWeek(anchor)
  return Array.from({ length: 7 }, (_, index) => addDays(start, index))
}

export function weekRange(anchor: Date): DateRange {
  const start = startOfWeek(anchor)
  return { from: toDateString(start), to: toDateString(addDays(start, 6)) }
}

/** Сетка месяца: 6 недель по 7 дней, начиная с понедельника. */
export function monthGridDays(anchor: Date): Date[] {
  const start = startOfWeek(new Date(anchor.getFullYear(), anchor.getMonth(), 1))
  return Array.from({ length: 42 }, (_, index) => addDays(start, index))
}

export function monthGridRange(anchor: Date): DateRange {
  const days = monthGridDays(anchor)
  return { from: toDateString(days[0]), to: toDateString(days[days.length - 1]) }
}

/** Границы календарного месяца — батч для фоновой загрузки. */
export function monthRange(anchor: Date): DateRange {
  return {
    from: toDateString(new Date(anchor.getFullYear(), anchor.getMonth(), 1)),
    to: toDateString(new Date(anchor.getFullYear(), anchor.getMonth() + 1, 0)),
  }
}

/** Календарные месяцы, в которые попадает диапазон дат (граничные месяцы включительно). */
export function monthsCovering(from: string, to: string): Date[] {
  const start = parseDateString(from)
  const end = parseDateString(to)
  const last = new Date(end.getFullYear(), end.getMonth(), 1)
  const months: Date[] = []

  for (
    let month = new Date(start.getFullYear(), start.getMonth(), 1);
    month <= last;
    month = new Date(month.getFullYear(), month.getMonth() + 1, 1)
  ) {
    months.push(month)
  }

  return months
}

export function isSameDay(a: Date, b: Date): boolean {
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  )
}

export function isToday(date: Date): boolean {
  return isSameDay(date, new Date())
}

export function toMinutes(time: string): number {
  const [hours, minutes] = time.split(':')
  return Number(hours) * 60 + Number(minutes)
}

export function toTimeString(minutes: number): string {
  return `${pad2(Math.floor(minutes / 60))}:${pad2(minutes % 60)}`
}

/** Ближайшая будущая получасовая отметка, в пределах дня. */
export function nextHalfHour(date: Date): number {
  const minutes = date.getHours() * 60 + date.getMinutes()
  return Math.min(Math.ceil(minutes / 30) * 30, HOURS_IN_DAY * 60 - 30)
}

export function formatTimeRange(start: string | null, end: string | null): string {
  if (start === null || end === null) return 'Без времени'
  return `${start.slice(0, 5)}–${end.slice(0, 5)}`
}

/**
 * Прошедшая только неповторяющаяся запись: без времени — со следующего дня,
 * со временем — после окончания. Повторяющиеся серии не считаются прошедшими.
 */
export function isEntryPast(entry: ScheduleEntry, now: Date): boolean {
  if (entry.recurrence !== 'none') return false

  const date = parseDateString(entry.date)
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())

  if (date.getTime() !== today.getTime()) return date < today
  if (entry.start === null) return false

  const endMinutes = toMinutes(entry.end ?? entry.start)
  return endMinutes <= now.getHours() * 60 + now.getMinutes()
}

export function formatClock(date: Date): string {
  return `${pad2(date.getHours())}:${pad2(date.getMinutes())}`
}

export function formatWeekdayShort(date: Date): string {
  return WEEKDAYS_SHORT[date.getDay()]
}

export function formatDayMonth(date: Date): string {
  return `${date.getDate()} ${MONTHS_GENITIVE[date.getMonth()]}`
}

export function formatFullDate(date: Date): string {
  return `${WEEKDAYS_LONG[date.getDay()]}, ${formatDayMonth(date)} ${date.getFullYear()}`
}

export function formatWeekTitle(anchor: Date): string {
  const days = weekDays(anchor)
  const start = days[0]
  const end = days[days.length - 1]
  const years =
    start.getFullYear() === end.getFullYear()
      ? String(end.getFullYear())
      : `${start.getFullYear()}–${end.getFullYear()}`

  return `${formatDayMonth(start)} — ${formatDayMonth(end)} ${years}`
}

export function formatMonthTitle(anchor: Date): string {
  const month = MONTHS_NOMINATIVE[anchor.getMonth()]
  return `${month.charAt(0).toUpperCase()}${month.slice(1)} ${anchor.getFullYear()}`
}

export function groupByDate(entries: ScheduleEntry[]): Map<string, ScheduleEntry[]> {
  const grouped = new Map<string, ScheduleEntry[]>()

  for (const entry of entries) {
    const list = grouped.get(entry.date)
    if (list) list.push(entry)
    else grouped.set(entry.date, [entry])
  }

  return grouped
}

export interface PositionedEntry {
  entry: ScheduleEntry
  column: number
  columns: number
}

type TimedEntry = ScheduleEntry & { start: string }

/**
 * Раскладывает события дня по колонкам так, чтобы пересекающиеся по времени
 * записи не накладывались друг на друга.
 */
export function layoutTimedEntries(entries: ScheduleEntry[]): PositionedEntry[] {
  const items = entries
    .filter((entry): entry is TimedEntry => entry.start !== null)
    .map((entry) => {
      const start = toMinutes(entry.start)
      const rawEnd = entry.end === null ? start : toMinutes(entry.end)
      return { entry, start, end: Math.max(rawEnd, start + MIN_EVENT_MINUTES), column: 0 }
    })
    .sort((a, b) => a.start - b.start || b.end - a.end)

  const result: PositionedEntry[] = []
  let cluster: typeof items = []
  let clusterEnd = 0

  const flush = () => {
    if (cluster.length === 0) return

    const columns = cluster.reduce((max, item) => Math.max(max, item.column + 1), 0)
    for (const item of cluster) {
      result.push({ entry: item.entry, column: item.column, columns })
    }
    cluster = []
  }

  for (const item of items) {
    if (cluster.length > 0 && item.start >= clusterEnd) flush()

    if (cluster.length === 0) clusterEnd = item.end
    else clusterEnd = Math.max(clusterEnd, item.end)

    const used = new Set(
      cluster.filter((other) => other.end > item.start).map((other) => other.column),
    )
    let column = 0
    while (used.has(column)) column += 1

    item.column = column
    cluster.push(item)
  }

  flush()
  return result
}
