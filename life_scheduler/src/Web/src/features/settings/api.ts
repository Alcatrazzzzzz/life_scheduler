import { toApiError } from '../../lib/apiError'

export type ShortcutMode = 'toggle' | 'hold'

export interface VoiceShortcutSetting {
  combo: string
  mode: ShortcutMode
  holdDelayMs: number
}

export const DEFAULT_VOICE_SHORTCUT: VoiceShortcutSetting = {
  combo: 'Ctrl+Shift+Space',
  mode: 'toggle',
  holdDelayMs: 350,
}

const ENDPOINT = '/api/settings/voice-shortcut'

export async function getVoiceShortcut(): Promise<VoiceShortcutSetting> {
  const res = await fetch(ENDPOINT)
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as VoiceShortcutSetting
}

export async function updateVoiceShortcut(
  input: VoiceShortcutSetting,
): Promise<VoiceShortcutSetting> {
  const res = await fetch(ENDPOINT, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!res.ok) throw await toApiError(res)

  return (await res.json()) as VoiceShortcutSetting
}
