import type { QueryClient } from '@tanstack/react-query'

/**
 * Отметки и правки привычек влияют и на календарь, и на вкладку привычек.
 * Сбрасываем оба корня кэшей, иначе месячные батчи со staleTime 5 минут останутся старыми.
 */
export async function invalidatePlanningData(queryClient: QueryClient): Promise<void> {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: ['schedule'] }),
    queryClient.invalidateQueries({ queryKey: ['habit'] }),
  ])
}
