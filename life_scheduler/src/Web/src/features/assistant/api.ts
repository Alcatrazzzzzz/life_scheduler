import { toApiError } from '../../lib/apiError'

export interface AssistantAction {
  name: string
  arguments: Record<string, unknown>
}

export interface AssistantResult {
  kind: 'habit_created' | 'habit_logged' | 'habits_listed' | 'schedule_created'
  message: string
  id?: string | null
  title?: string | null
  date?: string | null
  alreadyLogged?: boolean | null
  habits?: string[] | null
}

export interface AssistantResponse {
  recognizedText: string
  action: AssistantAction
  result: AssistantResult
}

function fileNameFor(blob: Blob): string {
  if (blob.type.includes('ogg')) return 'recording.ogg'
  if (blob.type.includes('wav')) return 'recording.wav'
  return 'recording.webm'
}

export async function processCommand(input: Blob | string): Promise<AssistantResponse> {
  const init: RequestInit = { method: 'POST' }

  if (typeof input === 'string') {
    init.headers = { 'Content-Type': 'application/json' }
    init.body = JSON.stringify({ text: input })
  } else {
    const form = new FormData()
    form.append('audio', input, fileNameFor(input))
    init.body = form
  }

  const res = await fetch('/api/assistant/command', init)
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as AssistantResponse
}
