import { describe, expect, it } from 'vitest'
import type { ScheduleEntry } from './api'
import { isEntryPast } from './calendar'

function entry(overrides: Partial<ScheduleEntry> = {}): ScheduleEntry {
  return {
    id: 'entry',
    title: 'Запись',
    date: '2026-11-20',
    start: null,
    end: null,
    notes: null,
    createdAt: '2026-11-20T09:00:00+00:00',
    recurrence: 'none',
    attachmentCount: 0,
    ...overrides,
  }
}

const noon = new Date(2026, 10, 20, 12, 0)

describe('isEntryPast', () => {
  it('считает прошедшей вчерашнюю запись без времени', () => {
    expect(isEntryPast(entry({ date: '2026-11-19' }), noon)).toBe(true)
  })

  it('не считает прошедшей сегодняшнюю запись без времени', () => {
    expect(isEntryPast(entry(), noon)).toBe(false)
  })

  it('считает прошедшей запись, которая уже закончилась', () => {
    expect(isEntryPast(entry({ start: '10:00', end: '11:00' }), noon)).toBe(true)
  })

  it('не считает прошедшей запись, которая ещё идёт', () => {
    expect(isEntryPast(entry({ start: '11:30', end: '13:00' }), noon)).toBe(false)
  })

  it('считает прошедшей запись, закончившуюся ровно сейчас', () => {
    expect(isEntryPast(entry({ start: '11:00', end: '12:00' }), noon)).toBe(true)
  })

  it('не считает прошедшей будущую запись', () => {
    expect(isEntryPast(entry({ date: '2026-11-21' }), noon)).toBe(false)
  })

  it('никогда не считает прошедшей повторяющуюся серию', () => {
    expect(isEntryPast(entry({ date: '2026-11-19', recurrence: 'daily' }), noon)).toBe(false)
  })
})
