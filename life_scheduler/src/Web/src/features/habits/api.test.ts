import { afterEach, describe, expect, it, vi } from 'vitest'
import { completeHabit, createHabit, listHabits, uncompleteHabit } from './api'

function stubFetch(body: string = '{}') {
  const fetchMock = vi
    .fn()
    .mockResolvedValue(
      new Response(body, { status: 200, headers: { 'Content-Type': 'application/json' } }),
    )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function requestUrl(fetchMock: ReturnType<typeof stubFetch>): URL {
  return new URL(String(fetchMock.mock.calls[0][0]), 'http://localhost')
}

function requestInit(fetchMock: ReturnType<typeof stubFetch>): RequestInit {
  return fetchMock.mock.calls[0][1] as RequestInit
}

describe('listHabits', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('по умолчанию запрашивает окно 91 день', async () => {
    const fetchMock = stubFetch(JSON.stringify({ today: '2026-11-20', habits: [] }))

    const response = await listHabits()

    expect(requestUrl(fetchMock).pathname).toBe('/api/habits')
    expect(requestUrl(fetchMock).searchParams.get('days')).toBe('91')
    expect(response.today).toBe('2026-11-20')
  })
})

describe('createHabit', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('создаёт привычку с наградой и stacking', async () => {
    const fetchMock = stubFetch(JSON.stringify({ id: 'habit-1' }))
    const stackAfterId = 'trigger-1'

    await createHabit({
      title: 'Душ',
      date: '2026-11-20',
      start: null,
      end: null,
      recurrence: 'daily',
      reward: 'Кофе',
      stackAfterId,
    })

    expect(requestUrl(fetchMock).pathname).toBe('/api/schedules')
    expect(requestInit(fetchMock).method).toBe('POST')

    const body = JSON.parse(String(requestInit(fetchMock).body)) as Record<string, unknown>
    expect(body.kind).toBe('habit')
    expect(body.reward).toBe('Кофе')
    expect(body.stackAfterId).toBe(stackAfterId)
  })
})

describe('completion', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('отмечает конкретный день', async () => {
    const fetchMock = stubFetch()

    await completeHabit('habit-1', '2026-11-20')

    expect(requestUrl(fetchMock).pathname).toBe('/api/schedules/habit-1/completion')
    expect(requestInit(fetchMock).method).toBe('POST')
    expect(JSON.parse(String(requestInit(fetchMock).body))).toEqual({ date: '2026-11-20' })
  })

  it('снимает отметку через query string', async () => {
    const fetchMock = stubFetch()

    await uncompleteHabit('habit-1', '2026-11-20')

    expect(requestUrl(fetchMock).pathname).toBe('/api/schedules/habit-1/completion')
    expect(requestUrl(fetchMock).searchParams.get('date')).toBe('2026-11-20')
    expect(requestInit(fetchMock).method).toBe('DELETE')
  })
})
