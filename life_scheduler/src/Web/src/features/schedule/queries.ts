import { useEffect } from 'react'
import {
  useMutation,
  useQueries,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import {
  completeSchedule,
  createSchedule,
  deleteAttachment,
  deleteSchedule,
  fetchAttachmentBlob,
  getSchedule,
  listHistory,
  listSchedules,
  uncompleteSchedule,
  uploadAttachment,
} from './api'
import type { ScheduleEntry } from './api'
import { monthRange, monthsCovering, startOfToday } from './calendar'

const PREFETCH_MONTHS_AHEAD = 12
const PREFETCH_MONTHS_BEHIND = 3
const CHUNK_STALE_MS = 5 * 60_000

export const scheduleKeys = {
  all: ['schedule'] as const,
  list: (from?: string, to?: string) => [...scheduleKeys.all, 'list', { from, to }] as const,
  detail: (id: string) => [...scheduleKeys.all, 'detail', id] as const,
  attachment: (scheduleId: string, attachmentId: string) =>
    [...scheduleKeys.all, 'attachment', scheduleId, attachmentId] as const,
  history: ['schedule', 'history'] as const,
}

interface ListFilter {
  from?: string
  to?: string
}

function readListFilter(queryKey: readonly unknown[]): ListFilter | null {
  const params = queryKey[2]
  if (typeof params !== 'object' || params === null) return null

  const { from, to } = params as { from?: unknown; to?: unknown }
  return {
    from: typeof from === 'string' ? from : undefined,
    to: typeof to === 'string' ? to : undefined,
  }
}

function isWithinFilter(entry: ScheduleEntry, filter: ListFilter): boolean {
  if (filter.from && entry.date < filter.from) return false
  if (filter.to && entry.date > filter.to) return false
  return true
}

function sortLikeEndpoint(entries: ScheduleEntry[]): ScheduleEntry[] {
  return [...entries].sort((a, b) => {
    const byDate = a.date.localeCompare(b.date)
    return byDate !== 0 ? byDate : (a.start ?? '').localeCompare(b.start ?? '')
  })
}

function addToListCaches(queryClient: QueryClient, entry: ScheduleEntry) {
  const listPrefix = [...scheduleKeys.all, 'list']

  for (const query of queryClient.getQueryCache().findAll({ queryKey: listPrefix })) {
    const filter = readListFilter(query.queryKey)
    if (filter === null || !isWithinFilter(entry, filter)) continue

    queryClient.setQueryData<ScheduleEntry[]>(query.queryKey, (entries) =>
      entries
        ? sortLikeEndpoint([...entries.filter((current) => current.id !== entry.id), entry])
        : entries,
    )
  }
}

function scheduleChunkOptions(month: Date) {
  const range = monthRange(month)

  return {
    queryKey: scheduleKeys.list(range.from, range.to),
    queryFn: () => listSchedules(range.from, range.to),
    staleTime: CHUNK_STALE_MS,
  }
}

/**
 * Сливает месячные батчи в один список. Соседние месяцы не пересекаются, но
 * дедупликация по id+date страхует от дублей повторяющихся вхождений.
 */
export function mergeWindowEntries(
  chunks: readonly ScheduleEntry[][],
  from: string,
  to: string,
): ScheduleEntry[] {
  const seen = new Set<string>()
  const merged: ScheduleEntry[] = []

  for (const entries of chunks) {
    for (const entry of entries) {
      if (entry.date < from || entry.date > to) continue

      // Повторяющаяся запись приходит по одному вхождению на дату — ключ id+date.
      const key = `${entry.id}-${entry.date}`
      if (seen.has(key)) continue

      seen.add(key)
      merged.push(entry)
    }
  }

  return sortLikeEndpoint(merged)
}

/**
 * Данные для видимого периода собираются из месячных батчей, которые уже
 * лежат в кэше. Недостающие батчи запрашиваются сразу при навигации.
 */
export function useScheduleWindow(from: string, to: string, enabled = true) {
  const results = useQueries({
    queries: monthsCovering(from, to).map((month) => ({
      ...scheduleChunkOptions(month),
      enabled,
    })),
  })

  const entries = mergeWindowEntries(
    results.map((result) => result.data ?? []),
    from,
    to,
  )
  const error = results.find((result) => result.error)?.error ?? null

  return {
    entries,
    error,
    // Лоадер нужен только пока рисовать нечего; частично собранное окно показываем как есть.
    isLoading: enabled && entries.length === 0 && results.every((result) => result.isPending),
  }
}

let prefetchStarted = false

function monthsToPrefetch(today: Date): Date[] {
  // Ближние месяцы нужны первыми — и вперёд, и назад.
  const offsets: number[] = []
  for (let distance = 1; distance <= PREFETCH_MONTHS_AHEAD; distance += 1) {
    offsets.push(distance)
    if (distance <= PREFETCH_MONTHS_BEHIND) offsets.push(-distance)
  }

  return offsets.map((offset) => new Date(today.getFullYear(), today.getMonth() + offset, 1))
}

/** После первой отрисовки догружает соседние месяцы последовательными батчами. */
export function useSchedulePrefetch(enabled: boolean) {
  const queryClient = useQueryClient()

  useEffect(() => {
    if (!enabled || prefetchStarted) return
    prefetchStarted = true

    void (async () => {
      for (const month of monthsToPrefetch(startOfToday())) {
        try {
          // fetchQuery, а не prefetchQuery: последний глотает ошибки и не даёт понять,
          // что батч не загрузился, — очередь бы молча оборвалась.
          await queryClient.fetchQuery(scheduleChunkOptions(month))
        } catch {
          // API недоступен — не долбим оставшиеся батчи.
          return
        }
      }
    })()
  }, [enabled, queryClient])
}

export function useSchedule(id: string, enabled = true) {
  return useQuery({
    queryKey: scheduleKeys.detail(id),
    queryFn: () => getSchedule(id),
    enabled,
  })
}

export function useAttachmentBlob(
  scheduleId: string,
  attachmentId: string,
  enabled = true,
) {
  return useQuery({
    queryKey: scheduleKeys.attachment(scheduleId, attachmentId),
    queryFn: () => fetchAttachmentBlob(scheduleId, attachmentId),
    enabled,
    // Файлы до 25 МБ: не держим их в кэше после закрытия предпросмотра.
    gcTime: 0,
  })
}

export function useScheduleHistory() {
  return useQuery({
    queryKey: scheduleKeys.history,
    queryFn: listHistory,
    // Архиватор переводит записи раз в минуту — обновляем открытую вкладку истории.
    refetchInterval: 60_000,
  })
}

export function useCreateSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createSchedule,
    onSuccess: (created) => {
      addToListCaches(queryClient, created)
      void queryClient.invalidateQueries({ queryKey: scheduleKeys.all })
    },
  })
}

export function useDeleteSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteSchedule,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scheduleKeys.all }),
  })
}

export function useCompleteSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, date }: { id: string; date: string }) => completeSchedule(id, date),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scheduleKeys.all }),
  })
}

export function useUncompleteSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, date }: { id: string; date: string }) => uncompleteSchedule(id, date),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scheduleKeys.all }),
  })
}

export function useAddAttachment(scheduleId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (file: File) => uploadAttachment(scheduleId, file),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scheduleKeys.all }),
  })
}

export function useAddAttachments() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ scheduleId, files }: { scheduleId: string; files: File[] }) => {
      for (const file of files) {
        await uploadAttachment(scheduleId, file)
      }
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scheduleKeys.all }),
  })
}

export function useDeleteAttachment(scheduleId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (attachmentId: string) => deleteAttachment(scheduleId, attachmentId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scheduleKeys.all }),
  })
}
