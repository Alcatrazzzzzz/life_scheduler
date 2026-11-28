export interface ShortcutEvent {
  key: string
  ctrlKey: boolean
  altKey: boolean
  shiftKey: boolean
  metaKey: boolean
}

type Modifier = 'Ctrl' | 'Alt' | 'Shift' | 'Meta'

const MODIFIER_KEYS = new Set(['Control', 'Alt', 'Shift', 'Meta', 'OS', 'AltGraph'])

function keyFromEvent(event: ShortcutEvent): string | null {
  if (MODIFIER_KEYS.has(event.key)) return null
  if (event.key === ' ' || event.key === 'Spacebar') return 'Space'
  if (event.key.length === 1) return event.key.toUpperCase()

  return event.key
}

/** Возвращает каноническую комбинацию или null, если нажаты только модификаторы. */
export function formatShortcut(event: ShortcutEvent): string | null {
  const key = keyFromEvent(event)
  if (key === null) return null

  const pressed = [
    event.ctrlKey ? 'Ctrl' : null,
    event.altKey ? 'Alt' : null,
    event.shiftKey ? 'Shift' : null,
    event.metaKey ? 'Meta' : null,
  ].filter((modifier): modifier is Modifier => modifier !== null)

  if (pressed.length === 0) return null

  return [...pressed, key].join('+')
}

export function matchesShortcut(event: ShortcutEvent, combo: string): boolean {
  return formatShortcut(event) === combo
}

/** Клавиша (без модификаторов) из сохранённой комбинации. */
export function shortcutKey(combo: string): string | null {
  const parts = combo.split('+').filter((part) => part.length > 0)
  return parts.length > 0 ? parts[parts.length - 1] : null
}

/** Отпущена ли основная клавиша комбинации (модификаторы не считаются). */
export function isShortcutKey(event: ShortcutEvent, combo: string): boolean {
  const key = keyFromEvent(event)
  return key !== null && key === shortcutKey(combo)
}

export function isEditableTarget(target: EventTarget | null): boolean {
  if (!target || typeof target !== 'object') return false

  const element = target as { tagName?: unknown; isContentEditable?: unknown }

  if (typeof element.tagName === 'string') {
    const tag = element.tagName.toUpperCase()
    if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return true
  }

  return element.isContentEditable === true
}
