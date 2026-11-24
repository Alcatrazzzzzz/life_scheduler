import { describe, expect, it } from 'vitest'
import type { HabitDayStatus, HabitSummary } from './api'
import {
  aggregateDays,
  atRiskHabits,
  currentWeekStats,
  dayState,
  habitSparkline,
  heatmapWeeks,
  orderByChain,
  previousWeekStats,
  todayChains,
  trend,
  weekBuckets,
} from './habitStats'

function habit(
  overrides: Partial<HabitSummary> & { id: string; title: string },
): HabitSummary {
  return {
    date: '2026-11-01',
    start: null,
    end: null,
    notes: null,
    recurrence: 'daily',
    reward: null,
    stackAfterId: null,
    stackAfterTitle: null,
    currentStreak: 0,
    longestStreak: 0,
    missedTwice: false,
    lastOccurrenceMissed: false,
    lastMissedDate: null,
    nextOccurrenceDate: null,
    todayScheduled: false,
    todayDone: false,
    completionRate: null,
    days: [],
    ...overrides,
  }
}

function day(date: string, scheduled: boolean, completed: boolean): HabitDayStatus {
  return { date, scheduled, completed }
}

const today = '2026-11-20'

describe('aggregateDays', () => {
  it('суммирует запланированные и выполненные дни по всем привычкам', () => {
    const aggregates = aggregateDays([
      habit({ id: 'a', title: 'A', days: [day('2026-11-20', true, true)] }),
      habit({ id: 'b', title: 'B', days: [day('2026-11-20', true, false)] }),
    ])

    expect(aggregates).toEqual([
      { date: '2026-11-20', scheduled: 2, completed: 1 },
    ])
  })
})

describe('weekBuckets', () => {
  it('исключает будущие дни и заполняет пустые недели', () => {
    const aggregates = [
      { date: '2026-11-09', scheduled: 1, completed: 1 },
      { date: '2026-11-16', scheduled: 1, completed: 0 },
      { date: '2026-11-20', scheduled: 1, completed: 1 },
      { date: '2026-11-21', scheduled: 1, completed: 0 },
    ]

    expect(weekBuckets(aggregates, today, 3)).toEqual([
      { start: '2026-11-02', scheduled: 0, completed: 0 },
      { start: '2026-11-09', scheduled: 1, completed: 1 },
      { start: '2026-11-16', scheduled: 2, completed: 1 },
    ])
  })
})

describe('week stats and trend', () => {
  it('считает текущую и прошлую неделю до сегодня', () => {
    const aggregates = [
      { date: '2026-11-09', scheduled: 2, completed: 2 },
      { date: '2026-11-16', scheduled: 2, completed: 1 },
      { date: '2026-11-20', scheduled: 1, completed: 1 },
      { date: '2026-11-22', scheduled: 1, completed: 0 },
    ]

    expect(currentWeekStats(aggregates, today)).toEqual({ scheduled: 3, completed: 2 })
    expect(previousWeekStats(aggregates, today)).toEqual({ scheduled: 2, completed: 2 })
    expect(trend({ scheduled: 3, completed: 2 }, { scheduled: 2, completed: 2 })).toBe(-33)
  })

  it('возвращает null, если прошлая неделя пуста', () => {
    expect(trend({ scheduled: 2, completed: 1 }, { scheduled: 0, completed: 0 })).toBeNull()
  })
})

describe('heatmapWeeks', () => {
  it('возвращает заданное число недель по семь дней', () => {
    const weeks = heatmapWeeks([], today, 2)

    expect(weeks).toHaveLength(2)
    expect(weeks[0]).toHaveLength(7)
    expect(weeks[0][0]).toEqual({ date: '2026-11-09', scheduled: 0, completed: 0 })
    expect(weeks[1][6]).toEqual({ date: '2026-11-22', scheduled: 0, completed: 0 })
  })
})

describe('habitSparkline', () => {
  it('даёт долю выполнения по неделям и null для пустых', () => {
    const sample = habit({
      id: 'a',
      title: 'A',
      days: [
        day('2026-11-09', true, true),
        day('2026-11-16', true, false),
        day('2026-11-17', true, true),
      ],
    })

    expect(habitSparkline(sample, today, 3)).toEqual([null, 1, 0.5])
  })
})

describe('todayChains', () => {
  it('строит цепочку по stackAfterId и выносит одиночек', () => {
    const a = habit({ id: 'a', title: 'A', todayScheduled: true })
    const b = habit({
      id: 'b',
      title: 'B',
      todayScheduled: true,
      stackAfterId: a.id,
    })
    const c = habit({
      id: 'c',
      title: 'C',
      todayScheduled: true,
      stackAfterId: b.id,
    })
    const d = habit({ id: 'd', title: 'D', todayScheduled: true })
    const e = habit({
      id: 'e',
      title: 'E',
      todayScheduled: true,
      stackAfterId: 'missing',
    })

    const result = todayChains([a, b, c, d, e])

    expect(result.chains.map((chain) => chain.map((item) => item.id))).toEqual([
      ['a', 'b', 'c'],
    ])
    expect(result.singles.map((item) => item.id)).toEqual(['d', 'e'])
  })
})

describe('orderByChain', () => {
  it('ставит зависимые привычки после триггера', () => {
    const a = habit({ id: 'a', title: 'A' })
    const b = habit({ id: 'b', title: 'B', stackAfterId: a.id })
    const c = habit({ id: 'c', title: 'C', stackAfterId: a.id })
    const d = habit({ id: 'd', title: 'D' })

    expect(orderByChain([c, d, b, a]).map((item) => item.id)).toEqual([
      'a',
      'b',
      'c',
      'd',
    ])
  })
})

describe('dayState', () => {
  it('различает пропуск, выполнение, план и будущее', () => {
    expect(dayState(day('2026-11-19', true, false), today)).toBe('missed')
    expect(dayState(day('2026-11-20', true, false), today)).toBe('planned')
    expect(dayState(day('2026-11-20', true, true), today)).toBe('done')
    expect(dayState(day('2026-11-21', true, false), today)).toBe('future')
    expect(dayState(day('2026-11-20', false, false), today)).toBe('off')
  })
})

describe('atRiskHabits', () => {
  it('включает два пропуска и вчерашний пропуск при незакрытом сегодня', () => {
    const missedTwice = habit({ id: 'a', title: 'A', missedTwice: true })
    const yesterday = habit({
      id: 'b',
      title: 'B',
      lastOccurrenceMissed: true,
      todayScheduled: true,
      todayDone: false,
    })
    const safe = habit({
      id: 'c',
      title: 'C',
      lastOccurrenceMissed: true,
      todayScheduled: true,
      todayDone: true,
    })

    expect(atRiskHabits([missedTwice, yesterday, safe]).map((item) => item.id)).toEqual([
      'a',
      'b',
    ])
  })
})
