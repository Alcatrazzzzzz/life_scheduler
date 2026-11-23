import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  type ReactNode,
  type RefObject,
} from 'react'
import './fields.css'

const GAP = 6
const VIEWPORT_MARGIN = 8

interface PopoverProps {
  open: boolean
  anchorRef: RefObject<HTMLElement | null>
  onClose: () => void
  label: string
  stretch?: boolean
  returnFocusRef?: RefObject<HTMLElement | null>
  className?: string
  children: ReactNode
}

interface PopoverPosition {
  top: number
  left: number
  minWidth: number
}

/**
 * Панель под полем. Позиция считается от триггера и обновляется на скролл и resize,
 * поэтому панель не обрезается контейнерами с overflow. Рендерится на месте,
 * без портала: нативный <dialog> делает всё за своими пределами inert.
 */
export function Popover({
  open,
  anchorRef,
  onClose,
  label,
  stretch,
  returnFocusRef,
  className,
  children,
}: PopoverProps) {
  const panelRef = useRef<HTMLDivElement>(null)
  const [position, setPosition] = useState<PopoverPosition | null>(null)

  const reposition = useCallback(() => {
    const anchor = anchorRef.current
    const panel = panelRef.current
    if (!anchor || !panel) return

    const anchorRect = anchor.getBoundingClientRect()
    const width = stretch ? Math.max(anchorRect.width, panel.offsetWidth) : panel.offsetWidth
    const height = panel.offsetHeight

    const fitsBelow = anchorRect.bottom + GAP + height <= window.innerHeight - VIEWPORT_MARGIN
    const fitsAbove = anchorRect.top - GAP - height >= VIEWPORT_MARGIN
    const openUp = !fitsBelow && fitsAbove

    const top = openUp ? anchorRect.top - GAP - height : anchorRect.bottom + GAP
    let left = anchorRect.left

    if (left + width > window.innerWidth - VIEWPORT_MARGIN) {
      left = window.innerWidth - width - VIEWPORT_MARGIN
    }

    setPosition({
      top: Math.max(VIEWPORT_MARGIN, Math.min(top, window.innerHeight - height - VIEWPORT_MARGIN)),
      left: Math.max(VIEWPORT_MARGIN, left),
      minWidth: stretch ? anchorRect.width : 0,
    })
  }, [anchorRef, stretch])

  useLayoutEffect(() => {
    if (open) reposition()
  }, [open, reposition])

  useEffect(() => {
    if (!open) return

    function handlePointerDown(event: PointerEvent) {
      const target = event.target as Node
      if (panelRef.current?.contains(target) || anchorRef.current?.contains(target)) return
      onClose()
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== 'Escape') return
      // Иначе Escape закроет и сам <dialog>.
      event.preventDefault()
      event.stopPropagation()
      onClose()
      const focusTarget = returnFocusRef ?? anchorRef
      focusTarget.current?.focus()
    }

    document.addEventListener('pointerdown', handlePointerDown, true)
    document.addEventListener('keydown', handleKeyDown, true)
    window.addEventListener('resize', reposition)
    window.addEventListener('scroll', reposition, true)

    return () => {
      document.removeEventListener('pointerdown', handlePointerDown, true)
      document.removeEventListener('keydown', handleKeyDown, true)
      window.removeEventListener('resize', reposition)
      window.removeEventListener('scroll', reposition, true)
    }
  }, [open, onClose, reposition, anchorRef, returnFocusRef])

  if (!open) return null

  return (
    <div
      ref={panelRef}
      className={className ? `popover ${className}` : 'popover'}
      role="dialog"
      aria-label={label}
      style={
        position
          ? { top: position.top, left: position.left, minWidth: position.minWidth }
          : { visibility: 'hidden' }
      }
    >
      {children}
    </div>
  )
}
