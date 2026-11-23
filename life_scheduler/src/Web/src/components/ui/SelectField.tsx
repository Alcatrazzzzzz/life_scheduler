import { useEffect, useRef, useState, type KeyboardEvent } from 'react'
import { Check, ChevronDown } from 'lucide-react'
import { centerSelected } from './popoverScroll'
import { Popover } from './Popover'

export interface SelectOption<T extends string> {
  value: T
  label: string
}

interface SelectFieldProps<T extends string> {
  label: string
  value: T
  options: SelectOption<T>[]
  onChange: (value: T) => void
  disabled?: boolean
  size?: 'md' | 'sm'
}

export function SelectField<T extends string>({
  label,
  value,
  options,
  onChange,
  disabled,
  size = 'md',
}: SelectFieldProps<T>) {
  const triggerRef = useRef<HTMLButtonElement>(null)
  const listRef = useRef<HTMLDivElement>(null)
  const [open, setOpen] = useState(false)

  const selected = options.find((option) => option.value === value)

  useEffect(() => {
    if (!open) return
    centerSelected(listRef.current)
    listRef.current?.querySelector<HTMLElement>('[aria-selected="true"]')?.focus()
  }, [open])

  function pick(next: T) {
    onChange(next)
    setOpen(false)
    triggerRef.current?.focus()
  }

  function handleListKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    const target = event.target as HTMLElement
    if (target.getAttribute('role') !== 'option') return

    const items = Array.from(
      event.currentTarget.querySelectorAll<HTMLElement>('[role="option"]'),
    )
    const index = items.indexOf(target)
    if (index === -1) return

    const moves: Record<string, number> = { ArrowUp: -1, ArrowDown: 1 }
    let next: number | null = null

    if (event.key in moves) next = index + moves[event.key]
    if (event.key === 'Home') next = 0
    if (event.key === 'End') next = items.length - 1

    if (next === null) return

    event.preventDefault()
    items[Math.max(0, Math.min(items.length - 1, next))]?.focus()
  }

  return (
    <div className={`field field--${size}`}>
      <span className="field__label">{label}</span>

      <div className="field__control">
        <button
          ref={triggerRef}
          type="button"
          className="field-trigger"
          aria-haspopup="listbox"
          aria-expanded={open}
          aria-label={`${label}: ${selected?.label ?? ''}`}
          disabled={disabled}
          onClick={() => setOpen((current) => !current)}
          onKeyDown={(event) => {
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
              event.preventDefault()
              setOpen(true)
            }
          }}
        >
          <span className="field-trigger__value">{selected?.label}</span>
          <ChevronDown className="field-trigger__chevron" size={15} strokeWidth={1.5} aria-hidden="true" />
        </button>

        <Popover open={open} anchorRef={triggerRef} onClose={() => setOpen(false)} label={label} stretch>
          <div
            className="select-list"
            role="listbox"
            aria-label={label}
            ref={listRef}
            onKeyDown={handleListKeyDown}
          >
            {options.map((option) => (
              <button
                key={option.value}
                type="button"
                role="option"
                className="select-option"
                aria-selected={option.value === value}
                onClick={() => pick(option.value)}
              >
                <span className="select-option__label">{option.label}</span>
                {option.value === value && (
                  <Check className="select-option__check" size={14} strokeWidth={1.5} aria-hidden="true" />
                )}
              </button>
            ))}
          </div>
        </Popover>
      </div>
    </div>
  )
}
