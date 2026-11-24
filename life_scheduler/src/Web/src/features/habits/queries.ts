import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { invalidatePlanningData } from '../../lib/invalidatePlanningData'
import {
  completeHabit,
  createHabit,
  deleteHabit,
  listHabits,
  uncompleteHabit,
  updateHabit,
  type HabitInput,
} from './api'

export const habitKeys = {
  all: ['habit'] as const,
  list: () => [...habitKeys.all, 'list'] as const,
}

export function useHabits() {
  return useQuery({
    queryKey: habitKeys.list(),
    queryFn: () => listHabits(),
    staleTime: 60_000,
  })
}

export function useCreateHabit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: HabitInput) => createHabit(input),
    onSuccess: () => invalidatePlanningData(queryClient),
  })
}

export function useUpdateHabit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: HabitInput }) => updateHabit(id, input),
    onSuccess: () => invalidatePlanningData(queryClient),
  })
}

export function useDeleteHabit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => deleteHabit(id),
    onSuccess: () => invalidatePlanningData(queryClient),
  })
}

export function useCompleteHabit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, date }: { id: string; date: string }) => completeHabit(id, date),
    onSuccess: () => invalidatePlanningData(queryClient),
  })
}

export function useUncompleteHabit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, date }: { id: string; date: string }) => uncompleteHabit(id, date),
    onSuccess: () => invalidatePlanningData(queryClient),
  })
}
