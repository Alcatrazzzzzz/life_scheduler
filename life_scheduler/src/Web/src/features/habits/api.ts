import { toApiError } from '../../lib/apiError'

export type HabitRecurrence = 'daily' | 'weekly'

export interface HabitDayStatus {
  date: string
  scheduled: boolean
  completed: boolean
}

export interface HabitSummary {
  id: string
  title: string
  date: string
  start: string | null
  end: string | null
  notes: string | null
  recurrence: HabitRecurrence
  reward: string | null
  stackAfterId: string | null
  stackAfterTitle: string | null
  currentStreak: number
  longestStreak: number
  missedTwice: boolean
  lastOccurrenceMissed: boolean
  lastMissedDate: string | null
  nextOccurrenceDate: string | null
  todayScheduled: boolean
  todayDone: boolean
  completionRate: number | null
  days: HabitDayStatus[]
}

export interface HabitsResponse {
  today: string
  habits: HabitSummary[]
}

export interface HabitInput {
  title: string
  date: string
  start: string | null
  end: string | null
  notes?: string | null
  recurrence: HabitRecurrence
  reward?: string | null
  stackAfterId?: string | null
}

export async function listHabits(days = 91): Promise<HabitsResponse> {
  const res = await fetch(`/api/habits?days=${days}`)
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as HabitsResponse
}

export async function createHabit(input: HabitInput): Promise<{ id: string }> {
  const res = await fetch('/api/schedules', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ...input, kind: 'habit' }),
  })
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as { id: string }
}

export async function updateHabit(id: string, input: HabitInput): Promise<void> {
  const res = await fetch(`/api/schedules/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ...input, kind: 'habit' }),
  })
  if (!res.ok) throw await toApiError(res)
}

export async function deleteHabit(id: string): Promise<void> {
  const res = await fetch(`/api/schedules/${id}`, { method: 'DELETE' })
  if (!res.ok) throw await toApiError(res)
}

export async function completeHabit(id: string, date: string): Promise<void> {
  const res = await fetch(`/api/schedules/${id}/completion`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ date }),
  })
  if (!res.ok) throw await toApiError(res)
}

export async function uncompleteHabit(id: string, date: string): Promise<void> {
  const params = new URLSearchParams({ date })
  const res = await fetch(`/api/schedules/${id}/completion?${params.toString()}`, {
    method: 'DELETE',
  })
  if (!res.ok) throw await toApiError(res)
}
