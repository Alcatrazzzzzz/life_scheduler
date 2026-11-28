import { Mic, Square, X } from 'lucide-react'
import { useCallback, useEffect, useRef, useState, type KeyboardEvent } from 'react'
import { isEditableTarget, isShortcutKey, matchesShortcut } from '../../settings/shortcut'
import type { VoiceShortcutSetting } from '../../settings/api'
import { useVoiceShortcut } from '../../settings/queries'
import type { AssistantResponse } from '../api'
import { useProcessCommand } from '../queries'
import '../assistant.css'

type AssistantState =
  | { phase: 'idle' }
  | { phase: 'recording'; seconds: number }
  | { phase: 'processing' }
  | { phase: 'result'; response: AssistantResponse }
  | { phase: 'error'; message: string }

const MAX_RECORDING_SECONDS = 30

function pickMimeType(): string | null {
  const candidates = ['audio/webm;codecs=opus', 'audio/webm', 'audio/ogg;codecs=opus']

  for (const candidate of candidates) {
    if (typeof MediaRecorder !== 'undefined' && MediaRecorder.isTypeSupported(candidate)) {
      return candidate
    }
  }

  return null
}

function describeRecordingError(error: unknown): string {
  if (error instanceof DOMException) {
    if (error.name === 'NotAllowedError') {
      return 'Разрешите доступ к микрофону в настройках браузера.'
    }

    if (error.name === 'NotFoundError') {
      return 'Микрофон не найден.'
    }
  }

  return error instanceof Error ? error.message : 'Не удалось начать запись.'
}

export function VoiceAssistant() {
  const [open, setOpen] = useState(false)
  const [state, setState] = useState<AssistantState>({ phase: 'idle' })
  const { mutateAsync } = useProcessCommand()
  const shortcut = useVoiceShortcut()

  const recorderRef = useRef<MediaRecorder | null>(null)
  const chunksRef = useRef<Blob[]>([])
  const streamRef = useRef<MediaStream | null>(null)
  const timerRef = useRef<number | null>(null)
  const startedAtRef = useRef(0)
  const cancelledRef = useRef(false)
  const mountedRef = useRef(true)
  const phaseRef = useRef<AssistantState['phase']>('idle')
  const shortcutRef = useRef<VoiceShortcutSetting | null>(null)
  const holdTimerRef = useRef<number | null>(null)
  const holdPressedRef = useRef(false)
  const holdRecordingRef = useRef(false)

  const stopTracks = useCallback(() => {
    streamRef.current?.getTracks().forEach((track) => track.stop())
    streamRef.current = null
  }, [])

  const clearTimer = useCallback(() => {
    if (timerRef.current !== null) {
      window.clearInterval(timerRef.current)
      timerRef.current = null
    }
  }, [])

  useEffect(() => {
    mountedRef.current = true

    return () => {
      mountedRef.current = false
      cancelledRef.current = true
      clearTimer()

      const recorder = recorderRef.current
      if (recorder && recorder.state !== 'inactive') {
        recorder.stop()
      }

      recorderRef.current = null
      stopTracks()
    }
  }, [clearTimer, stopTracks])

  useEffect(() => {
    phaseRef.current = state.phase
  }, [state.phase])

  useEffect(() => {
    shortcutRef.current = shortcut.data ?? null
  }, [shortcut.data])

  const submit = useCallback(
    async (blob: Blob) => {
      setState({ phase: 'processing' })

      try {
        const response = await mutateAsync(blob)

        if (mountedRef.current) {
          setState({ phase: 'result', response })
        }
      } catch (error) {
        if (mountedRef.current) {
          setState({
            phase: 'error',
            message: error instanceof Error ? error.message : 'Не удалось выполнить команду.',
          })
        }
      }
    },
    [mutateAsync],
  )

  const startRecording = useCallback(async () => {
    if (!navigator.mediaDevices?.getUserMedia || typeof MediaRecorder === 'undefined') {
      setState({ phase: 'error', message: 'Браузер не поддерживает запись звука.' })
      return
    }

    const mimeType = pickMimeType()

    if (!mimeType) {
      setState({ phase: 'error', message: 'Браузер не поддерживает webm/opus.' })
      return
    }

    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: { channelCount: 1, echoCancellation: true, noiseSuppression: true },
      })

      if (!mountedRef.current) {
        stream.getTracks().forEach((track) => track.stop())
        return
      }

      streamRef.current = stream
      chunksRef.current = []
      cancelledRef.current = false

      const recorder = new MediaRecorder(stream, { mimeType })
      recorderRef.current = recorder

      recorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          chunksRef.current.push(event.data)
        }
      }

      recorder.onstop = () => {
        const chunks = chunksRef.current
        chunksRef.current = []
        stopTracks()
        clearTimer()

        if (cancelledRef.current) {
          cancelledRef.current = false
          if (mountedRef.current) setState({ phase: 'idle' })
          return
        }

        const blob = new Blob(chunks, { type: recorder.mimeType || mimeType })

        if (blob.size === 0) {
          if (mountedRef.current) {
            setState({ phase: 'error', message: 'Запись пустая. Попробуйте ещё раз.' })
          }
          return
        }

        void submit(blob)
      }

      recorder.start(250)
      startedAtRef.current = Date.now()
      setState({ phase: 'recording', seconds: 0 })

      timerRef.current = window.setInterval(() => {
        const elapsed = Math.floor((Date.now() - startedAtRef.current) / 1000)

        if (elapsed >= MAX_RECORDING_SECONDS) {
          if (recorder.state !== 'inactive') recorder.stop()
          return
        }

        if (mountedRef.current) {
          setState({ phase: 'recording', seconds: elapsed })
        }
      }, 250)
    } catch (error) {
      stopTracks()
      clearTimer()
      setState({ phase: 'error', message: describeRecordingError(error) })
    }
  }, [clearTimer, stopTracks, submit])

  const stopRecording = useCallback(() => {
    const recorder = recorderRef.current

    if (recorder && recorder.state !== 'inactive') {
      recorder.stop()
    }
  }, [])

  const cancelRecording = useCallback(() => {
    cancelledRef.current = true
    const recorder = recorderRef.current

    if (recorder && recorder.state !== 'inactive') {
      recorder.stop()
      return
    }

    stopTracks()
    clearTimer()
    setState({ phase: 'idle' })
  }, [clearTimer, stopTracks])

  useEffect(() => {
    function clearHoldTimer() {
      if (holdTimerRef.current !== null) {
        window.clearTimeout(holdTimerRef.current)
        holdTimerRef.current = null
      }
    }

    function releaseHold() {
      holdPressedRef.current = false
      clearHoldTimer()

      if (holdRecordingRef.current) {
        holdRecordingRef.current = false
        stopRecording()
      }
    }

    function onKeyDown(event: globalThis.KeyboardEvent) {
      const config = shortcutRef.current

      if (!config || event.repeat) return
      if (document.querySelector('dialog[open]')) return
      if (isEditableTarget(event.target)) return
      if (!matchesShortcut(event, config.combo)) return

      const phase = phaseRef.current
      event.preventDefault()

      if (config.mode === 'toggle') {
        if (phase === 'recording') {
          stopRecording()
          return
        }

        if (phase === 'idle' || phase === 'result' || phase === 'error') {
          setOpen(true)
          void startRecording()
        }

        return
      }

      if (phase !== 'idle' && phase !== 'result' && phase !== 'error') return

      holdPressedRef.current = true
      holdTimerRef.current = window.setTimeout(() => {
        holdTimerRef.current = null

        if (!holdPressedRef.current) return

        holdRecordingRef.current = true
        setOpen(true)
        void startRecording()
      }, config.holdDelayMs)
    }

    function onKeyUp(event: globalThis.KeyboardEvent) {
      const config = shortcutRef.current

      if (!config || config.mode !== 'hold') return
      if (!holdPressedRef.current) return
      if (!isShortcutKey(event, config.combo)) return

      releaseHold()
    }

    function onBlur() {
      releaseHold()
    }

    window.addEventListener('keydown', onKeyDown)
    window.addEventListener('keyup', onKeyUp)
    window.addEventListener('blur', onBlur)

    return () => {
      window.removeEventListener('keydown', onKeyDown)
      window.removeEventListener('keyup', onKeyUp)
      window.removeEventListener('blur', onBlur)
      clearHoldTimer()
    }
  }, [startRecording, stopRecording])

  const close = useCallback(() => {
    if (state.phase === 'recording') {
      cancelRecording()
    }

    setOpen(false)
    setState({ phase: 'idle' })
  }, [cancelRecording, state.phase])

  const handleKeyDown = (event: KeyboardEvent<HTMLElement>) => {
    if (event.key === 'Escape' && state.phase !== 'recording') {
      event.stopPropagation()
      close()
    }
  }

  return (
    <div className="voice-assistant">
      {open && (
        <section
          className="voice-panel"
          role="dialog"
          aria-label="Голосовой помощник"
          onKeyDown={handleKeyDown}
        >
          <header className="voice-panel__header">
            <h2 className="voice-panel__title">Голосовой помощник</h2>
            <button
              type="button"
              className="btn btn--icon voice-panel__close"
              onClick={close}
              aria-label="Закрыть"
            >
              <X size={16} strokeWidth={1.5} aria-hidden="true" />
            </button>
          </header>

          <div className="voice-panel__body" aria-live="polite">
            {state.phase === 'idle' && (
              <p className="voice-panel__hint">
                Нажмите «Записать» и произнесите команду: отметить привычку, создать
                привычку, показать привычки или создать встречу.
              </p>
            )}

            {state.phase === 'recording' && (
              <p className="voice-panel__recording">
                <span className="voice-panel__dot" aria-hidden="true" />
                Запись… {state.seconds} с из {MAX_RECORDING_SECONDS}
              </p>
            )}

            {state.phase === 'processing' && (
              <p className="voice-panel__hint">Распознаю и выполняю…</p>
            )}

            {state.phase === 'result' && (
              <div className="voice-panel__result">
                <p className="voice-panel__recognized">«{state.response.recognizedText}»</p>
                <p className="voice-panel__message">{state.response.result.message}</p>

                {state.response.result.habits && state.response.result.habits.length > 0 && (
                  <ul className="voice-panel__list">
                    {state.response.result.habits.map((line, index) => (
                      <li key={`${index}-${line}`}>{line}</li>
                    ))}
                  </ul>
                )}
              </div>
            )}

            {state.phase === 'error' && (
              <p className="voice-panel__error">{state.message}</p>
            )}
          </div>

          <div className="voice-panel__actions">
            {state.phase === 'idle' && (
              <button
                type="button"
                className="btn btn--primary"
                onClick={() => void startRecording()}
              >
                <Mic size={16} strokeWidth={1.5} aria-hidden="true" />
                Записать
              </button>
            )}

            {state.phase === 'recording' && (
              <>
                <button type="button" className="btn btn--primary" onClick={stopRecording}>
                  <Square size={14} strokeWidth={1.5} aria-hidden="true" />
                  Готово
                </button>
                <button type="button" className="btn btn--ghost" onClick={cancelRecording}>
                  Отмена
                </button>
              </>
            )}

            {state.phase === 'processing' && (
              <span className="voice-panel__hint">Это может занять до минуты…</span>
            )}

            {state.phase === 'result' && (
              <button type="button" className="btn btn--secondary" onClick={close}>
                Готово
              </button>
            )}

            {state.phase === 'error' && (
              <>
                <button
                  type="button"
                  className="btn btn--primary"
                  onClick={() => void startRecording()}
                >
                  Повторить
                </button>
                <button type="button" className="btn btn--ghost" onClick={close}>
                  Закрыть
                </button>
              </>
            )}
          </div>
        </section>
      )}

      <button
        type="button"
        className="btn btn--secondary voice-fab"
        aria-label="Голосовая команда"
        aria-expanded={open}
        onClick={open ? close : () => setOpen(true)}
      >
        <Mic size={20} strokeWidth={1.5} aria-hidden="true" />
      </button>
    </div>
  )
}
