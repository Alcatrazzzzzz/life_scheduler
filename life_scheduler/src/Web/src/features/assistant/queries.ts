import { useMutation, useQueryClient } from '@tanstack/react-query'
import { invalidatePlanningData } from '../../lib/invalidatePlanningData'
import { processCommand } from './api'

export const assistantKeys = {
  all: ['assistant'] as const,
}

export function useProcessCommand() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: processCommand,
    onSuccess: () => invalidatePlanningData(queryClient),
  })
}
