import { useEffect, useRef, useState, type KeyboardEvent } from 'react'
import { CalendarDays, ChevronDown, ChevronLeft, ChevronRight } from 'lucide-react'
import { Popover } from './Popover'

const MONTHS_GENITIVE = [
  'января',
  'февраля',
  'марта',
  'апреля',
  'мая',
  'июня',
  'июля',
  'августа',
  'сентября',
  'октября',
  'ноября',
  'декабря',
]

const MONTHS_NOMINATIVE = [
  'январь',
  'февраль',
  'март',
  'апрель',
  'май',
  'июнь',
  'июль',
  'август',
  'сентябрь',
  'октябрь',
  'ноябрь',
  'декабрь',
]

const WEEKDAY_LABELS = ['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'вс']

function pad2(value: number): string {
  return String(value).padStart(2, '0')
}

function toDateString(date: Date): string {
  return `${date.getFullYear()}-${pad2(date.getMonth() + 1)}-${pad2(date.getDate())}`
}

function parseDateString(value: string): Date | null {
  if (!value) return null

  const [year, month, day] = value.split('-').map(Number)
  if (!year || !month || !day) return null

  return new Date(year, month - 1, day)
}

function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1)
}

function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days)
}

function addMonths(date: Date, months: number): Date {
  return new Date(date.getFullYear(), date.getMonth() + months, 1)
}

function sameMonth(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth()
}

/** Сетка месяца: 6 недель по 7 дней, неделя начинается с понедельника. */
function monthGrid(anchor: Date): Date[] {
  const first = startOfMonth(anchor)
  const offset = (first.getDay() + 6) % 7

  return Array.from({ length: 42 }, (_, index) => addDays(first, index - offset))
}

function formatDisplay(value: string): string {
  const date = parseDateString(value)
  if (!date) return ''
  return `${date.getDate()} ${MONTHS_GENITIVE[date.getMonth()]} ${date.getFullYear()}`
}

function formatDayLabel(date: Date): string {
  return `${date.getDate()} ${MONTHS_GENITIVE[date.getMonth()]} ${date.getFullYear()}`
}

function formatMonthTitle(month: Date): string {
  const name = MONTHS_NOMINATIVE[month.getMonth()]
  return `${name.charAt(0).toUpperCase()}${name.slice(1)} ${month.getFullYear()}`
}

interface DateFieldProps {
  label: string
  value: string
  onChange: (value: string) => void
  disabled?: boolean
  clearable?: boolean
  min?: string
  max?: string
  size?: 'md' | 'sm'
}

export function DateField({
  label,
  value,
  onChange,
  disabled,
  clearable,
  min,
  max,
  size = 'md',
}: DateFieldProps) {
  const triggerRef = useRef<HTMLButtonElement>(null)
  const gridRef = useRef<HTMLDivElement>(null)
  const pendingFocusRef = useRef<string | null>(null)
  const [open, setOpen] = useState(false)
  const [viewMonth, setViewMonth] = useState(() => startOfMonth(parseDateString(value) ?? new Date()))

  const today = new Date()
  const display = formatDisplay(value)

  useEffect(() => {
    if (!open) return

    const target = gridRef.current?.querySelector<HTMLElement>(
      `[data-date="${value || toDateString(new Date())}"]`,
    )
    target?.focus()
  }, [open, value])

  useEffect(() => {
    const iso = pendingFocusRef.current
    if (!iso) return

    pendingFocusRef.current = null
    gridRef.current?.querySelector<HTMLElement>(`[data-date="${iso}"]`)?.focus()
  }, [viewMonth])

  function openPanel() {
    setViewMonth(startOfMonth(parseDateString(value) ?? new Date()))
    setOpen(true)
  }

  function select(date: Date) {
    onChange(toDateString(date))
    setOpen(false)
    triggerRef.current?.focus()
  }

  function moveView(months: number) {
    setViewMonth((current) => addMonths(current, months))
  }

  function focusDay(date: Date) {
    const iso = toDateString(date)

    if (!sameMonth(date, viewMonth)) {
      pendingFocusRef.current = iso
      setViewMonth(startOfMonth(date))
      return
    }

    gridRef.current?.querySelector<HTMLElement>(`[data-date="${iso}"]`)?.focus()
  }

  function handleGridKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    const target = event.target as HTMLElement
    const iso = target.dataset.date
    if (!iso) return

    const current = parseDateString(iso)
    if (!current) return

    const moves: Record<string, number> = {
      ArrowLeft: -1,
      ArrowRight: 1,
      ArrowUp: -7,
      ArrowDown: 7,
    }

    if (event.key in moves) {
      event.preventDefault()
      focusDay(addDays(current, moves[event.key]))
      return
    }

    if (event.key === 'PageUp' || event.key === 'PageDown') {
      event.preventDefault()
      const next = addMonths(current, event.key === 'PageUp' ? -1 : 1)
      focusDay(new Date(next.getFullYear(), next.getMonth(), current.getDate()))
    }
  }

  const days = monthGrid(viewMonth)
  const todayIso = toDateString(today)

  return (
    <div className={`field field--${size}`}>
      <span className="field__label">{label}</span>

      <div className="field__control">
        <button
          ref={triggerRef}
          type="button"
          className="field-trigger"
          aria-haspopup="dialog"
          aria-expanded={open}
          aria-label={`${label}: ${display || 'не выбрано'}`}
          disabled={disabled}
          onClick={() => (open ? setOpen(false) : openPanel())}
        >
          <CalendarDays className="field-trigger__icon" size={15} strokeWidth={1.5} aria-hidden="true" />
          <span className={`field-trigger__value${display ? '' : ' field-trigger__value--empty'}`}>
            {display || 'Не выбрано'}
          </span>
          <ChevronDown className="field-trigger__chevron" size={15} strokeWidth={1.5} aria-hidden="true" />
        </button>

        <Popover open={open} anchorRef={triggerRef} onClose={() => setOpen(false)} label={label}>
          <div className="date-picker">
            <div className="date-picker__header">
              <button
                type="button"
                className="picker-icon-button"
                aria-label="Предыдущий месяц"
                onClick={() => moveView(-1)}
              >
                <ChevronLeft size={16} strokeWidth={1.5} aria-hidden="true" />
              </button>
              <span className="date-picker__title">{formatMonthTitle(viewMonth)}</span>
              <button
                type="button"
                className="picker-icon-button"
                aria-label="Следующий месяц"
                onClick={() => moveView(1)}
              >
                <ChevronRight size={16} strokeWidth={1.5} aria-hidden="true" />
              </button>
            </div>

            <div className="date-picker__weekdays" aria-hidden="true">
              {WEEKDAY_LABELS.map((weekday) => (
                <span key={weekday}>{weekday}</span>
              ))}
            </div>

            <div className="date-picker__grid" ref={gridRef} onKeyDown={handleGridKeyDown}>
              {days.map((day) => {
                const iso = toDateString(day)
                const isSelected = iso === value
                const isDisabled = (min !== undefined && iso < min) || (max !== undefined && iso > max)
                const classes = ['date-picker__day']

                if (!sameMonth(day, viewMonth)) classes.push('date-picker__day--outside')
                if (iso === todayIso) classes.push('date-picker__day--today')
                if (isSelected) classes.push('date-picker__day--selected')

                return (
                  <button
                    key={iso}
                    type="button"
                    className={classes.join(' ')}
                    data-date={iso}
                    aria-label={formatDayLabel(day)}
                    aria-pressed={isSelected}
                    disabled={isDisabled}
                    onClick={() => select(day)}
                  >
                    {day.getDate()}
                  </button>
                )
              })}
            </div>

            <div className="date-picker__footer">
              <button
                type="button"
                className="picker-link"
                onClick={() => {
                  const now = new Date()
                  setViewMonth(startOfMonth(now))
                  onChange(toDateString(now))
                  setOpen(false)
                  triggerRef.current?.focus()
                }}
              >
                Сегодня
              </button>

              {clearable && value && (
                <button
                  type="button"
                  className="picker-link"
                  onClick={() => {
                    onChange('')
                    setOpen(false)
                    triggerRef.current?.focus()
                  }}
                >
                  Очистить
                </button>
              )}
            </div>
          </div>
        </Popover>
      </div>
    </div>
  )
}
