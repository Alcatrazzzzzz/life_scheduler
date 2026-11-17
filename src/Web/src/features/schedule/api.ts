export type ScheduleRecurrence = 'none' | 'daily' | 'weekly'

export interface ScheduleEntry {
  id: string
  title: string
  date: string
  start: string | null
  end: string | null
  notes: string | null
  createdAt: string
  recurrence: ScheduleRecurrence
}

export interface CreateScheduleInput {
  title: string
  date: string
  start: string | null
  end: string | null
  notes?: string
  recurrence: ScheduleRecurrence
}

export interface UpdateScheduleInput {
  title: string
  date: string
  start: string | null
  end: string | null
  notes?: string
  recurrence: ScheduleRecurrence
}

async function toApiError(res: Response): Promise<Error> {
  try {
    const payload: unknown = await res.json()
    const messages = collectProblemMessages(payload)
    if (messages.length > 0) {
      return new Error(messages.join(' '))
    }
  } catch {
    // тело не JSON — остаётся только статус
  }

  return new Error(`Запрос не выполнен: HTTP ${res.status}`)
}

function collectProblemMessages(payload: unknown): string[] {
  if (typeof payload !== 'object' || payload === null) {
    return []
  }

  const problem = payload as { title?: unknown; errors?: unknown }
  const errors = problem.errors
  const details: string[] = []

  if (typeof errors === 'object' && errors !== null) {
    // TODO: фильтровать errors по известным ключам, не Object.values — иначе служебные поля ProblemDetails попадут в сообщения
    for (const value of Object.values(errors)) {
      if (!Array.isArray(value)) continue

      for (const item of value) {
        if (typeof item === 'string') details.push(item)
      }
    }
  }

  if (details.length > 0) return details
  return typeof problem.title === 'string' ? [problem.title] : []
}

export async function listSchedules(from?: string, to?: string): Promise<ScheduleEntry[]> {
  const params = new URLSearchParams()
  if (from) params.set('from', from)
  if (to) params.set('to', to)

  const query = params.toString()
  const res = await fetch(`/api/schedules${query ? `?${query}` : ''}`)
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as ScheduleEntry[]
}

export async function getSchedule(id: string): Promise<ScheduleEntry> {
  const res = await fetch(`/api/schedules/${id}`)
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as ScheduleEntry
}

export async function createSchedule(input: CreateScheduleInput): Promise<ScheduleEntry> {
  const res = await fetch('/api/schedules', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as ScheduleEntry
}

export async function updateSchedule(id: string, input: UpdateScheduleInput): Promise<void> {
  const res = await fetch(`/api/schedules/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!res.ok) throw await toApiError(res)
}

export async function deleteSchedule(id: string): Promise<void> {
  const res = await fetch(`/api/schedules/${id}`, { method: 'DELETE' })
  if (!res.ok) throw await toApiError(res)
}
