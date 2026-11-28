import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getVoiceShortcut,
  updateVoiceShortcut,
  type VoiceShortcutSetting,
} from './api'

export const settingsKeys = {
  all: ['settings'] as const,
  voiceShortcut: () => [...settingsKeys.all, 'voice-shortcut'] as const,
}

export function useVoiceShortcut() {
  return useQuery({
    queryKey: settingsKeys.voiceShortcut(),
    queryFn: getVoiceShortcut,
    staleTime: 5 * 60_000,
  })
}

export function useUpdateVoiceShortcut() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: VoiceShortcutSetting) => updateVoiceShortcut(input),
    onSuccess: (saved) => {
      queryClient.setQueryData(settingsKeys.voiceShortcut(), saved)
    },
  })
}
