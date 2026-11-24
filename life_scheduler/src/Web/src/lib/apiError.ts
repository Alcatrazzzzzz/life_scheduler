/** Превращает ответ ProblemDetails в Error с читаемыми сообщениями. */
export async function toApiError(res: Response): Promise<Error> {
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
