import { describe, expect, it } from 'vitest'
import type { ScheduleEntry } from './api'
import { mergeWindowEntries } from './queries'

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
    kind: 'event',
    attachmentCount: 0,
    ...overrides,
  }
}

describe('mergeWindowEntries', () => {
  it('оставляет только вхождения внутри видимого диапазона', () => {
    const merged = mergeWindowEntries(
      [[entry({ id: 'a', date: '2026-10-31' }), entry({ id: 'b', date: '2026-11-02' })]],
      '2026-11-01',
      '2026-11-30',
    )

    expect(merged.map((item) => item.id)).toEqual(['b'])
  })

  it('убирает дубли на стыке пересекающихся месячных плиток', () => {
    const shared = entry({ id: 'a', date: '2026-11-01' })

    const merged = mergeWindowEntries([[shared], [shared]], '2026-10-26', '2026-12-06')

    expect(merged).toHaveLength(1)
  })

  it('сохраняет разные вхождения повторяющейся записи', () => {
    const merged = mergeWindowEntries(
      [
        [
          entry({ id: 'repeat', date: '2026-11-02', recurrence: 'daily' }),
          entry({ id: 'repeat', date: '2026-11-03', recurrence: 'daily' }),
        ],
      ],
      '2026-11-01',
      '2026-11-30',
    )

    expect(merged.map((item) => item.date)).toEqual(['2026-11-02', '2026-11-03'])
  })

  it('сортирует по дате, а внутри дня — сначала записи без времени', () => {
    const merged = mergeWindowEntries(
      [
        [
          entry({ id: 'timed', date: '2026-11-03', start: '09:00' }),
          entry({ id: 'allday', date: '2026-11-03' }),
          entry({ id: 'earlier', date: '2026-11-02' }),
        ],
      ],
      '2026-11-01',
      '2026-11-30',
    )

    expect(merged.map((item) => item.id)).toEqual(['earlier', 'allday', 'timed'])
  })
})
