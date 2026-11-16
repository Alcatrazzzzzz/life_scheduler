import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createSchedule, deleteSchedule, getSchedule, listSchedules } from './api'

export const scheduleKeys = {
  all: ['schedule'] as const,
  list: (from?: string, to?: string) => [...scheduleKeys.all, 'list', { from, to }] as const,
  detail: (id: string) => [...scheduleKeys.all, 'detail', id] as const,
}

export function useSchedules(from?: string, to?: string) {
  return useQuery({
    queryKey: scheduleKeys.list(from, to),
    queryFn: () => listSchedules(from, to),
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
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scheduleKeys.all }),
  })
}

export function useDeleteSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteSchedule,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scheduleKeys.all }),
  })
}
