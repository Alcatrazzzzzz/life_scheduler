import { describe, expect, it } from 'vitest'
import {
  formatShortcut,
  isEditableTarget,
  isShortcutKey,
  matchesShortcut,
  shortcutKey,
  type ShortcutEvent,
} from './shortcut'

function event(partial: Partial<ShortcutEvent> & { key: string }): ShortcutEvent {
  return {
    ctrlKey: false,
    altKey: false,
    shiftKey: false,
    metaKey: false,
    ...partial,
  }
}

describe('formatShortcut', () => {
  it('builds a canonical combo in modifier order', () => {
    const combo = event({ key: ' ', ctrlKey: true, shiftKey: true })

    expect(formatShortcut(combo)).toBe('Ctrl+Shift+Space')
  })

  it('keeps Ctrl, Alt, Shift, Meta order regardless of press order', () => {
    const combo = event({ key: 'k', metaKey: true, altKey: true, ctrlKey: true })

    expect(formatShortcut(combo)).toBe('Ctrl+Alt+Meta+K')
  })

  it('uppercases single-character keys', () => {
    expect(formatShortcut(event({ key: 'j', ctrlKey: true }))).toBe('Ctrl+J')
  })

  it('returns null for modifier-only presses', () => {
    expect(formatShortcut(event({ key: 'Control', ctrlKey: true }))).toBeNull()
  })

  it('returns null without a modifier', () => {
    expect(formatShortcut(event({ key: 'Space' }))).toBeNull()
  })
})

describe('matchesShortcut', () => {
  it('matches an equivalent event', () => {
    const combo = 'Ctrl+Shift+Space'

    expect(matchesShortcut(event({ key: ' ', ctrlKey: true, shiftKey: true }), combo)).toBe(true)
  })

  it('rejects a missing modifier', () => {
    expect(matchesShortcut(event({ key: ' ', ctrlKey: true }), 'Ctrl+Shift+Space')).toBe(false)
  })
})

describe('shortcutKey / isShortcutKey', () => {
  it('extracts the main key', () => {
    expect(shortcutKey('Ctrl+Shift+Space')).toBe('Space')
  })

  it('matches the released main key', () => {
    expect(isShortcutKey(event({ key: ' ' }), 'Ctrl+Shift+Space')).toBe(true)
    expect(isShortcutKey(event({ key: 'Control' }), 'Ctrl+Shift+Space')).toBe(false)
  })
})

describe('isEditableTarget', () => {
  it('detects form fields and contenteditable', () => {
    expect(isEditableTarget({ tagName: 'INPUT' } as unknown as EventTarget)).toBe(true)
    expect(isEditableTarget({ tagName: 'textarea' } as unknown as EventTarget)).toBe(true)
    expect(isEditableTarget({ tagName: 'SELECT' } as unknown as EventTarget)).toBe(true)
    expect(isEditableTarget({ isContentEditable: true } as unknown as EventTarget)).toBe(true)
  })

  it('ignores non-editable and empty targets', () => {
    expect(isEditableTarget({ tagName: 'BUTTON' } as unknown as EventTarget)).toBe(false)
    expect(isEditableTarget(null)).toBe(false)
  })
})
