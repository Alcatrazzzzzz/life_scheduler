import { describe, expect, it } from 'vitest'
import type { ScheduleEntry } from './api'
import { isEntryPast, monthRange, monthsCovering } from './calendar'

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

describe('isEntryPast', () => {  it('считает прошедшей вчерашнюю запись без времени', () => {
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

describe('monthsCovering', () => {
  function monthKeys(dates: Date[]): string[] {
    return dates.map((date) => `${date.getFullYear()}-${date.getMonth() + 1}`)
  }

  it('одна неделя внутри месяца — одна плитка', () => {
    expect(monthKeys(monthsCovering('2026-11-16', '2026-11-22'))).toEqual(['2026-11'])
  })

  it('неделя на стыке месяцев — две плитки', () => {
    expect(monthKeys(monthsCovering('2026-10-30', '2026-11-05'))).toEqual([
      '2026-10',
      '2026-11',
    ])
  })

  it('диапазон через границу года не теряет январь', () => {
    expect(monthKeys(monthsCovering('2026-12-28', '2027-01-03'))).toEqual([
      '2026-12',
      '2027-1',
    ])
  })

  it('сетка месяца захватывает соседние календарные месяцы', () => {
    expect(monthKeys(monthsCovering('2026-10-26', '2026-12-06'))).toEqual([
      '2026-10',
      '2026-11',
      '2026-12',
    ])
  })
})

describe('monthRange', () => {
  it('начинается с первого и заканчивается последним днём месяца', () => {
    expect(monthRange(new Date(2026, 10, 20))).toEqual({
      from: '2026-11-01',
      to: '2026-11-30',
    })
  })

  it('корректно определяет конец февраля в високосный год', () => {
    expect(monthRange(new Date(2028, 1, 10))).toEqual({
      from: '2028-02-01',
      to: '2028-02-29',
    })
  })
})
