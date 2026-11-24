import { toApiError } from '../../lib/apiError'

export type ScheduleRecurrence = 'none' | 'daily' | 'weekly'
export type ScheduleKind = 'event' | 'task' | 'habit'
export type ScheduleResult = 'none' | 'completed' | 'missed'

export interface ScheduleAttachment {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  createdAt: string
}

export interface ScheduleEntry {
  id: string
  title: string
  date: string
  start: string | null
  end: string | null
  notes: string | null
  createdAt: string
  recurrence: ScheduleRecurrence
  kind: ScheduleKind
  attachmentCount: number
  // Приходит только в истории: итог выполнения задачи или дня привычки.
  result?: ScheduleResult
  // Текущая серия привычки; приходит только на сегодняшнем вхождении.
  streak?: number | null
}

export interface HabitDay {
  date: string
  scheduled: boolean
  completed: boolean
}

export interface ScheduleDetail extends ScheduleEntry {
  today: string
  completedAt: string | null
  reward: string | null
  stackAfterId: string | null
  stackAfterTitle: string | null
  currentStreak: number | null
  longestStreak: number | null
  missedTwice: boolean
  lastOccurrenceMissed: boolean
  lastMissedDate: string | null
  completionRate: number | null
  habitDays: HabitDay[] | null
  attachments: ScheduleAttachment[]
}

export interface AttachmentUploadTicket {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  expiresAt: string
  upload: {
    url: string
    method: string
    headers: Record<string, string>
  }
}

export interface HistoryFilters {
  from?: string
  to?: string
  query?: string
  kind?: ScheduleKind
  result?: ScheduleResult
  hasAttachments?: boolean
}

export interface CreateScheduleInput {
  title: string
  date: string
  start: string | null
  end: string | null
  notes?: string
  recurrence: ScheduleRecurrence
  kind: ScheduleKind
  reward?: string | null
  stackAfterId?: string | null
}

export interface UpdateScheduleInput {
  title: string
  date: string
  start: string | null
  end: string | null
  notes?: string
  recurrence: ScheduleRecurrence
  kind: ScheduleKind
  reward?: string | null
  stackAfterId?: string | null
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

export async function getSchedule(id: string, date?: string): Promise<ScheduleDetail> {
  const query = date ? `?date=${encodeURIComponent(date)}` : ''
  const res = await fetch(`/api/schedules/${id}${query}`)
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as ScheduleDetail
}

export async function listHistory(filters: HistoryFilters = {}): Promise<ScheduleEntry[]> {
  const params = new URLSearchParams()
  if (filters.from) params.set('from', filters.from)
  if (filters.to) params.set('to', filters.to)
  if (filters.query) params.set('q', filters.query)
  if (filters.kind) params.set('kind', filters.kind)
  if (filters.result) params.set('result', filters.result)
  if (filters.hasAttachments) params.set('hasAttachments', 'true')

  const query = params.toString()
  const res = await fetch(`/api/schedules/history${query ? `?${query}` : ''}`)
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as ScheduleEntry[]
}

export async function createAttachment(
  scheduleId: string,
  file: File,
): Promise<AttachmentUploadTicket> {
  const res = await fetch(`/api/schedules/${scheduleId}/attachments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      fileName: file.name,
      contentType: file.type || 'application/octet-stream',
      sizeBytes: file.size,
    }),
  })
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as AttachmentUploadTicket
}

export async function uploadAttachmentFile(
  ticket: AttachmentUploadTicket,
  file: File,
): Promise<void> {
  const res = await fetch(ticket.upload.url, {
    method: ticket.upload.method,
    headers: ticket.upload.headers,
    body: file,
  })
  if (!res.ok) {
    throw new Error(`Не удалось загрузить файл: HTTP ${res.status}`)
  }
}

export async function confirmAttachment(
  scheduleId: string,
  attachmentId: string,
): Promise<void> {
  const res = await fetch(
    `/api/schedules/${scheduleId}/attachments/${attachmentId}/confirm`,
    { method: 'POST' },
  )
  if (!res.ok) throw await toApiError(res)
}

export async function uploadAttachment(scheduleId: string, file: File): Promise<void> {
  const ticket = await createAttachment(scheduleId, file)

  try {
    await uploadAttachmentFile(ticket, file)
    await confirmAttachment(scheduleId, ticket.id)
  } catch (error) {
    // Не оставляем висящую Pending-строку, если загрузка или подтверждение не удались.
    await deleteAttachment(scheduleId, ticket.id).catch(() => undefined)
    throw error
  }
}

export async function deleteAttachment(
  scheduleId: string,
  attachmentId: string,
): Promise<void> {
  const res = await fetch(`/api/schedules/${scheduleId}/attachments/${attachmentId}`, {
    method: 'DELETE',
  })
  if (!res.ok) throw await toApiError(res)
}

export function attachmentDownloadUrl(scheduleId: string, attachmentId: string): string {
  return `/api/schedules/${scheduleId}/attachments/${attachmentId}/download`
}

export async function fetchAttachmentBlob(
  scheduleId: string,
  attachmentId: string,
): Promise<Blob> {
  // Редирект на хранилище следует автоматически; blob нужен, чтобы показать
  // содержимое в предпросмотре несмотря на Content-Disposition: attachment.
  const res = await fetch(attachmentDownloadUrl(scheduleId, attachmentId))
  if (!res.ok) throw await toApiError(res)

  return res.blob()
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

export async function completeSchedule(id: string, date: string): Promise<void> {
  const res = await fetch(`/api/schedules/${id}/completion`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ date }),
  })
  if (!res.ok) throw await toApiError(res)
}

export async function uncompleteSchedule(id: string, date: string): Promise<void> {
  const params = new URLSearchParams({ date })
  const res = await fetch(`/api/schedules/${id}/completion?${params.toString()}`, {
    method: 'DELETE',
  })
  if (!res.ok) throw await toApiError(res)
}
