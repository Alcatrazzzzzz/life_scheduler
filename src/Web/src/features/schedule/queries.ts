import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { createSchedule, deleteSchedule, getSchedule, listSchedules } from './api'
import type { ScheduleEntry } from './api'

export const scheduleKeys = {
  all: ['schedule'] as const,
  list: (from?: string, to?: string) => [...scheduleKeys.all, 'list', { from, to }] as const,
  detail: (id: string) => [...scheduleKeys.all, 'detail', id] as const,
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

export function useSchedules(from?: string, to?: string) {
  return useQuery({
    queryKey: scheduleKeys.list(from, to),
    queryFn: () => listSchedules(from, to),
    placeholderData: keepPreviousData,
  })
}

export function useSchedule(id: string) {
  return useQuery({
    queryKey: scheduleKeys.detail(id),
    queryFn: () => getSchedule(id),
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
