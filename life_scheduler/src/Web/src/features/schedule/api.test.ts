import { afterEach, describe, expect, it, vi } from 'vitest'
import { listHistory } from './api'

function stubFetch() {
  const fetchMock = vi
    .fn()
    .mockResolvedValue(new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } }))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function requestUrl(fetchMock: ReturnType<typeof stubFetch>): URL {
  return new URL(String(fetchMock.mock.calls[0][0]), 'http://localhost')
}

describe('listHistory', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('без фильтров запрашивает историю без параметров', async () => {
    const fetchMock = stubFetch()

    await listHistory()

    expect(requestUrl(fetchMock).pathname).toBe('/api/schedules/history')
    expect(requestUrl(fetchMock).search).toBe('')
  })

  it('передаёт все фильтры в query string', async () => {
    const fetchMock = stubFetch()

    await listHistory({
      from: '2026-11-01',
      to: '2026-11-30',
      query: 'отчёт',
      kind: 'task',
      result: 'missed',
      hasAttachments: true,
    })

    const url = requestUrl(fetchMock)
    expect(url.searchParams.get('from')).toBe('2026-11-01')
    expect(url.searchParams.get('to')).toBe('2026-11-30')
    expect(url.searchParams.get('q')).toBe('отчёт')
    expect(url.searchParams.get('kind')).toBe('task')
    expect(url.searchParams.get('result')).toBe('missed')
    expect(url.searchParams.get('hasAttachments')).toBe('true')
  })
})
