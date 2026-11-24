import type { HabitDayStatus, HabitSummary } from './api'

export interface DayAggregate {
  date: string
  scheduled: number
  completed: number
}

export interface WeekBucket {
  start: string
  scheduled: number
  completed: number
}

export interface StatPair {
  scheduled: number
  completed: number
}

export type DayState = 'off' | 'planned' | 'done' | 'missed' | 'future'

const WEEKDAYS_SHORT = ['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'вс']

function pad2(value: number): string {
  return String(value).padStart(2, '0')
}

function parseDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year, month - 1, day)
}

function formatDate(date: Date): string {
  return `${date.getFullYear()}-${pad2(date.getMonth() + 1)}-${pad2(date.getDate())}`
}

export function addDays(value: string, days: number): string {
  const date = parseDate(value)
  date.setDate(date.getDate() + days)
  return formatDate(date)
}

/** Понедельник недели, содержащей дату. */
export function startOfWeek(value: string): string {
  const date = parseDate(value)
  return addDays(value, -((date.getDay() + 6) % 7))
}

export function weekdayShort(value: string): string {
  return WEEKDAYS_SHORT[(parseDate(value).getDay() + 6) % 7]
}

function weekStarts(today: string, weeks: number): string[] {
  const current = startOfWeek(today)
  return Array.from({ length: weeks }, (_, index) => addDays(current, -7 * (weeks - 1 - index)))
}

export function aggregateDays(habits: HabitSummary[]): DayAggregate[] {
  const byDate = new Map<string, DayAggregate>()

  for (const habit of habits) {
    for (const day of habit.days) {
      const current = byDate.get(day.date) ?? { date: day.date, scheduled: 0, completed: 0 }
      if (day.scheduled) current.scheduled += 1
      if (day.completed) current.completed += 1
      byDate.set(day.date, current)
    }
  }

  return [...byDate.values()].sort((a, b) => a.date.localeCompare(b.date))
}

export function weekBuckets(
  aggregates: DayAggregate[],
  today: string,
  weeks: number,
): WeekBucket[] {
  const byStart = new Map<string, WeekBucket>()

  for (const day of aggregates) {
    if (day.date > today) continue

    const start = startOfWeek(day.date)
    const bucket = byStart.get(start) ?? { start, scheduled: 0, completed: 0 }
    bucket.scheduled += day.scheduled
    bucket.completed += day.completed
    byStart.set(start, bucket)
  }

  return weekStarts(today, weeks).map(
    (start) => byStart.get(start) ?? { start, scheduled: 0, completed: 0 },
  )
}

export function statsForRange(
  aggregates: DayAggregate[],
  from: string,
  to: string,
): StatPair {
  let scheduled = 0
  let completed = 0

  for (const day of aggregates) {
    if (day.date < from || day.date > to) continue
    scheduled += day.scheduled
    completed += day.completed
  }

  return { scheduled, completed }
}

export function currentWeekStats(aggregates: DayAggregate[], today: string): StatPair {
  return statsForRange(aggregates, startOfWeek(today), today)
}

export function previousWeekStats(aggregates: DayAggregate[], today: string): StatPair {
  const current = startOfWeek(today)
  return statsForRange(aggregates, addDays(current, -7), addDays(current, -1))
}

export function rate(stats: StatPair): number | null {
  return stats.scheduled === 0 ? null : stats.completed / stats.scheduled
}

/** Разница в процентных пунктах или null, если не с чем сравнивать. */
export function trend(current: StatPair, previous: StatPair): number | null {
  const currentRate = rate(current)
  const previousRate = rate(previous)
  if (currentRate === null || previousRate === null) return null

  return Math.round((currentRate - previousRate) * 100)
}

export function totalCompleted(aggregates: DayAggregate[], today: string): number {
  return aggregates.reduce(
    (sum, day) => (day.date <= today ? sum + day.completed : sum),
    0,
  )
}

export function heatmapWeeks(
  aggregates: DayAggregate[],
  today: string,
  weeks: number,
): DayAggregate[][] {
  const byDate = new Map(aggregates.map((day) => [day.date, day]))

  return weekStarts(today, weeks).map((start) =>
    Array.from({ length: 7 }, (_, index) => {
      const date = addDays(start, index)
      return byDate.get(date) ?? { date, scheduled: 0, completed: 0 }
    }),
  )
}

export function habitWeekBuckets(
  habit: HabitSummary,
  today: string,
  weeks: number,
): WeekBucket[] {
  return weekBuckets(aggregateDays([habit]), today, weeks)
}

export function habitSparkline(
  habit: HabitSummary,
  today: string,
  weeks: number,
): Array<number | null> {
  return habitWeekBuckets(habit, today, weeks).map((bucket) => rate(bucket))
}

export function habitHeatmapWeeks(
  habit: HabitSummary,
  today: string,
  weeks: number,
): DayAggregate[][] {
  return heatmapWeeks(
    habit.days.map((day) => ({
      date: day.date,
      scheduled: day.scheduled ? 1 : 0,
      completed: day.completed ? 1 : 0,
    })),
    today,
    weeks,
  )
}

export function habitWeekStats(habit: HabitSummary, today: string): StatPair {
  return statsForRange(aggregateDays([habit]), startOfWeek(today), today)
}

export function bestCurrentStreak(habits: HabitSummary[]): number {
  return habits.reduce((max, habit) => Math.max(max, habit.currentStreak), 0)
}

export function isAtRisk(habit: HabitSummary): boolean {
  return (
    habit.missedTwice ||
    (habit.lastOccurrenceMissed && habit.todayScheduled && !habit.todayDone)
  )
}

export function atRiskHabits(habits: HabitSummary[]): HabitSummary[] {
  return habits.filter(isAtRisk)
}

export function dayState(day: HabitDayStatus, today: string): DayState {
  if (!day.scheduled) return 'off'
  if (day.date > today) return 'future'
  if (day.completed) return 'done'
  return day.date < today ? 'missed' : 'planned'
}

/** Последние 7 статусов — текущая неделя Mon–Sun. */
export function currentWeekDays(habit: HabitSummary): HabitDayStatus[] {
  return habit.days.slice(-7)
}

export function orderByChain(habits: HabitSummary[]): HabitSummary[] {
  const byId = new Map(habits.map((habit) => [habit.id, habit]))
  const children = new Map<string, HabitSummary[]>()
  const roots: HabitSummary[] = []

  for (const habit of habits) {
    if (habit.stackAfterId && byId.has(habit.stackAfterId)) {
      const list = children.get(habit.stackAfterId) ?? []
      list.push(habit)
      children.set(habit.stackAfterId, list)
    } else {
      roots.push(habit)
    }
  }

  const byTitle = (a: HabitSummary, b: HabitSummary) => a.title.localeCompare(b.title)
  const ordered: HabitSummary[] = []
  const seen = new Set<string>()

  for (const root of roots.sort(byTitle)) {
    const stack = [root]

    while (stack.length > 0) {
      const habit = stack.pop()
      if (!habit || seen.has(habit.id)) continue

      seen.add(habit.id)
      ordered.push(habit)

      const kids = [...(children.get(habit.id) ?? [])].sort(byTitle)
      stack.push(...kids.reverse())
    }
  }

  // Данные с циклом (не должны появляться) не теряем.
  ordered.push(...habits.filter((habit) => !seen.has(habit.id)).sort(byTitle))
  return ordered
}

/** Цепочки из привычек, запланированных на сегодня. */
export function todayChains(habits: HabitSummary[]): {
  chains: HabitSummary[][]
  singles: HabitSummary[]
} {
  const due = habits.filter((habit) => habit.todayScheduled)
  const byId = new Map(due.map((habit) => [habit.id, habit]))
  const children = new Map<string, HabitSummary[]>()
  const roots: HabitSummary[] = []

  for (const habit of due) {
    if (habit.stackAfterId && byId.has(habit.stackAfterId)) {
      const list = children.get(habit.stackAfterId) ?? []
      list.push(habit)
      children.set(habit.stackAfterId, list)
    } else {
      roots.push(habit)
    }
  }

  const byTitle = (a: HabitSummary, b: HabitSummary) => a.title.localeCompare(b.title)
  const chains: HabitSummary[][] = []
  const seen = new Set<string>()

  for (const root of roots.sort(byTitle)) {
    const chain: HabitSummary[] = []
    const stack = [root]

    while (stack.length > 0) {
      const habit = stack.pop()
      if (!habit || seen.has(habit.id)) continue

      seen.add(habit.id)
      chain.push(habit)

      const kids = [...(children.get(habit.id) ?? [])].sort(byTitle)
      stack.push(...kids.reverse())
    }

    chains.push(chain)
  }

  for (const habit of due.filter((item) => !seen.has(item.id)).sort(byTitle)) {
    chains.push([habit])
  }

  return {
    chains: chains.filter((chain) => chain.length > 1),
    singles: chains.filter((chain) => chain.length === 1).flat(),
  }
}
