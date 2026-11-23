import { useEffect, useRef, useState, type KeyboardEvent } from 'react'
import { ChevronDown, Clock } from 'lucide-react'
import { centerSelected } from './popoverScroll'
import { Popover } from './Popover'
import { parseTypedTime } from './parseTypedTime'

const MINUTE_STEP = 5

function pad2(value: number): string {
  return String(value).padStart(2, '0')
}

function parseTime(value: string): { hour: number; minute: number } | null {
  const match = /^(\d{2}):(\d{2})$/.exec(value)
  if (!match) return null

  return { hour: Number(match[1]), minute: Number(match[2]) }
}

function minuteOptions(current: number | null): number[] {
  const options = Array.from({ length: 60 / MINUTE_STEP }, (_, index) => index * MINUTE_STEP)

  if (current !== null && !options.includes(current)) {
    options.push(current)
    options.sort((a, b) => a - b)
  }

  return options
}

interface TimeFieldProps {
  label: string
  value: string
  onChange: (value: string) => void
  disabled?: boolean
  size?: 'md' | 'sm'
}

export function TimeField({ label, value, onChange, disabled, size = 'md' }: TimeFieldProps) {
  const fieldRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const hoursRef = useRef<HTMLDivElement>(null)
  const minutesRef = useRef<HTMLDivElement>(null)
  const [open, setOpen] = useState(false)
  // null — поле показывает value; строка — то, что пользователь набрал прямо сейчас.
  const [draft, setDraft] = useState<string | null>(null)

  const parsed = parseTime(value)
  const display = parsed ? `${pad2(parsed.hour)}:${pad2(parsed.minute)}` : ''
  const shown = draft ?? display

  useEffect(() => {
    if (!open) return
    centerSelected(hoursRef.current)
    centerSelected(minutesRef.current)
  }, [open])

  function currentParts(): { hour: number; minute: number } {
    const typed = parseTypedTime(draft ?? display)
    return (typed ? parseTime(typed) : null) ?? { hour: 0, minute: 0 }
  }

  function commitDraft() {
    if (draft === null) return

    const normalized = parseTypedTime(draft)
    if (normalized !== null) onChange(normalized)
    setDraft(null)
  }

  function pickHour(hour: number) {
    const minute = currentParts().minute
    onChange(`${pad2(hour)}:${pad2(minute)}`)
    setDraft(null)
  }

  function pickMinute(minute: number) {
    const hour = currentParts().hour
    onChange(`${pad2(hour)}:${pad2(minute)}`)
    setDraft(null)
    setOpen(false)
    inputRef.current?.focus()
  }

  function handleInputKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') {
      event.preventDefault()
      event.currentTarget.blur()
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setOpen(true)
    }
  }

  function handleColumnKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    const target = event.target as HTMLElement
    if (target.getAttribute('role') !== 'option') return

    const options = Array.from(
      event.currentTarget.querySelectorAll<HTMLElement>('[role="option"]'),
    )
    const index = options.indexOf(target)
    if (index === -1) return

    const moves: Record<string, number> = { ArrowUp: -1, ArrowDown: 1 }
    let next: number | null = null

    if (event.key in moves) next = index + moves[event.key]
    if (event.key === 'Home') next = 0
    if (event.key === 'End') next = options.length - 1

    if (next === null) return

    event.preventDefault()
    options[Math.max(0, Math.min(options.length - 1, next))]?.focus()
  }

  return (
    <div className={`field field--${size}`}>
      <span className="field__label">{label}</span>

      <div className="field__control">
        <div className="field-input-group" ref={fieldRef}>
          <Clock className="field-input-group__icon" size={15} strokeWidth={1.5} aria-hidden="true" />

          <input
            ref={inputRef}
            type="text"
            className="field-input-group__input"
            value={shown}
            placeholder="чч:мм"
            inputMode="numeric"
            autoComplete="off"
            spellCheck={false}
            maxLength={5}
            aria-label={label}
            disabled={disabled}
            onChange={(event) => setDraft(event.target.value.replace(/[^\d:.,\s]/g, ''))}
            onFocus={(event) => event.currentTarget.select()}
            onBlur={commitDraft}
            onKeyDown={handleInputKeyDown}
          />

          <button
            type="button"
            className="field-input-group__button"
            aria-haspopup="dialog"
            aria-expanded={open}
            aria-label={`${label}: открыть список`}
            disabled={disabled}
            onClick={() => setOpen((current) => !current)}
          >
            <ChevronDown size={15} strokeWidth={1.5} aria-hidden="true" />
          </button>
        </div>

        <Popover
          open={open}
          anchorRef={fieldRef}
          onClose={() => setOpen(false)}
          label={label}
          returnFocusRef={inputRef}
        >
          <div className="time-picker">
            <div
              className="time-picker__column"
              role="listbox"
              aria-label="Часы"
              ref={hoursRef}
              onKeyDown={handleColumnKeyDown}
            >
              {Array.from({ length: 24 }, (_, hour) => (
                <button
                  key={hour}
                  type="button"
                  role="option"
                  className="time-picker__option"
                  aria-selected={parsed?.hour === hour}
                  aria-label={pad2(hour)}
                  onClick={() => pickHour(hour)}
                >
                  {pad2(hour)}
                </button>
              ))}
            </div>

            <div
              className="time-picker__column"
              role="listbox"
              aria-label="Минуты"
              ref={minutesRef}
              onKeyDown={handleColumnKeyDown}
            >
              {minuteOptions(parsed?.minute ?? null).map((minute) => (
                <button
                  key={minute}
                  type="button"
                  role="option"
                  className="time-picker__option"
                  aria-selected={parsed?.minute === minute}
                  aria-label={pad2(minute)}
                  onClick={() => pickMinute(minute)}
                >
                  {pad2(minute)}
                </button>
              ))}
            </div>
          </div>
        </Popover>
      </div>
    </div>
  )
}
