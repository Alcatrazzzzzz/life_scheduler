import { useState, type FormEvent, type KeyboardEvent } from 'react'
import { Modal } from '../../components/ui/Modal'
import { SelectField, type SelectOption } from '../../components/ui/SelectField'
import {
  DEFAULT_VOICE_SHORTCUT,
  type ShortcutMode,
  type VoiceShortcutSetting,
} from './api'
import { useUpdateVoiceShortcut, useVoiceShortcut } from './queries'
import { formatShortcut } from './shortcut'
import './settings.css'

const MODE_OPTIONS: SelectOption<ShortcutMode>[] = [
  { value: 'toggle', label: 'Обычное нажатие' },
  { value: 'hold', label: 'Удержание' },
]

const MIN_HOLD_DELAY_MS = 0
const MAX_HOLD_DELAY_MS = 2000

interface SettingsDialogProps {
  onClose: () => void
}

export function SettingsDialog({ onClose }: SettingsDialogProps) {
  const shortcutQuery = useVoiceShortcut()

  return (
    <Modal title="Настройки" onClose={onClose}>
      {shortcutQuery.isPending && <p className="settings-hint">Загрузка настроек…</p>}

      {shortcutQuery.isError && (
        <div className="settings-form">
          <p className="settings-error">{shortcutQuery.error.message}</p>
          <div className="modal__actions">
            <button
              type="button"
              className="btn btn--secondary"
              onClick={() => void shortcutQuery.refetch()}
            >
              Повторить
            </button>
          </div>
        </div>
      )}

      {shortcutQuery.data !== undefined && (
        <SettingsForm initial={shortcutQuery.data} onClose={onClose} />
      )}
    </Modal>
  )
}

interface SettingsFormProps {
  initial: VoiceShortcutSetting
  onClose: () => void
}

function SettingsForm({ initial, onClose }: SettingsFormProps) {
  const update = useUpdateVoiceShortcut()

  const [combo, setCombo] = useState(initial.combo)
  const [mode, setMode] = useState<ShortcutMode>(initial.mode)
  const [holdDelay, setHoldDelay] = useState(String(initial.holdDelayMs))
  const [capturing, setCapturing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const delay = Number(holdDelay)
  const delayValid =
    Number.isInteger(delay) && delay >= MIN_HOLD_DELAY_MS && delay <= MAX_HOLD_DELAY_MS

  function handleCaptureKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (!capturing) return

    event.preventDefault()
    event.stopPropagation()

    if (event.key === 'Escape') {
      setCapturing(false)
      return
    }

    const next = formatShortcut(event)
    if (next === null) return

    setCombo(next)
    setCapturing(false)
    setError(null)
  }

  function resetToDefaults() {
    setCombo(DEFAULT_VOICE_SHORTCUT.combo)
    setMode(DEFAULT_VOICE_SHORTCUT.mode)
    setHoldDelay(String(DEFAULT_VOICE_SHORTCUT.holdDelayMs))
    setCapturing(false)
    setError(null)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (capturing) return

    if (!delayValid) {
      setError(
        `Задержка должна быть целым числом от ${MIN_HOLD_DELAY_MS} до ${MAX_HOLD_DELAY_MS} мс.`,
      )
      return
    }

    setError(null)

    try {
      await update.mutateAsync({ combo, mode, holdDelayMs: delay })
      onClose()
    } catch (mutationError) {
      setError(
        mutationError instanceof Error ? mutationError.message : 'Не удалось сохранить настройки.',
      )
    }
  }

  return (
    <form className="settings-form" onSubmit={handleSubmit}>
      <div className="settings-field">
        <span className="settings-field__label">Шорткат голосового ввода</span>
        <button
          type="button"
          className={capturing ? 'settings-shortcut is-capturing' : 'settings-shortcut'}
          onClick={() => {
            setCapturing(true)
            setError(null)
          }}
          onKeyDown={handleCaptureKeyDown}
          onBlur={() => setCapturing(false)}
        >
          {capturing ? 'Нажмите комбинацию…' : combo}
        </button>
        <p className="settings-hint">
          {capturing
            ? 'Нажмите клавиши вместе. Escape — отмена.'
            : 'Кликните, чтобы изменить. Нужен хотя бы один модификатор: Ctrl, Alt, Shift или Meta.'}
        </p>
      </div>

      <SelectField<ShortcutMode>
        label="Режим"
        value={mode}
        options={MODE_OPTIONS}
        onChange={setMode}
      />

      {mode === 'toggle' && (
        <p className="settings-hint">
          Нажатие начинает запись, повторное нажатие — останавливает.
        </p>
      )}

      {mode === 'hold' && (
        <>
          <label className="settings-field">
            <span className="settings-field__label">Задержка удержания, мс</span>
            <input
              className="settings-number"
              type="number"
              min={MIN_HOLD_DELAY_MS}
              max={MAX_HOLD_DELAY_MS}
              step={50}
              inputMode="numeric"
              value={holdDelay}
              onChange={(event) => setHoldDelay(event.target.value)}
            />
          </label>
          <p className="settings-hint">
            Запись начнётся, если держать клавишу дольше задержки; отпускание остановит запись.
          </p>
        </>
      )}

      {error && <p className="settings-error">{error}</p>}

      <div className="modal__actions">
        <button type="button" className="btn btn--ghost" onClick={resetToDefaults}>
          Сбросить
        </button>
        <button
          type="submit"
          className="btn btn--primary"
          disabled={update.isPending || capturing || !delayValid}
        >
          {update.isPending ? 'Сохраняю…' : 'Сохранить'}
        </button>
      </div>
    </form>
  )
}
