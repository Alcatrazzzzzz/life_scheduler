import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import {
  createSchedule,
  deleteAttachment,
  deleteSchedule,
  getSchedule,
  listHistory,
  listSchedules,
  uploadAttachment,
} from './api'
import type { ScheduleEntry } from './api'

export const scheduleKeys = {
  all: ['schedule'] as const,
  list: (from?: string, to?: string) => [...scheduleKeys.all, 'list', { from, to }] as const,
  detail: (id: string) => [...scheduleKeys.all, 'detail', id] as const,
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

export function useSchedules(from?: string, to?: string, enabled = true) {
  return useQuery({
    queryKey: scheduleKeys.list(from, to),
    queryFn: () => listSchedules(from, to),
    placeholderData: keepPreviousData,
    enabled,
  })
}

export function useSchedule(id: string, enabled = true) {
  return useQuery({
    queryKey: scheduleKeys.detail(id),
    queryFn: () => getSchedule(id),
    enabled,
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
