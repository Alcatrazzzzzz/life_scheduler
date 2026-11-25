import { afterEach, describe, expect, it, vi } from 'vitest'
import { processCommand } from './api'

const responseBody = JSON.stringify({
  recognizedText: 'отметь планёрку',
  action: { name: 'log_habit', arguments: { habit_name: 'планёрку' } },
  result: { kind: 'habit_logged', message: 'Привычка отмечена.' },
})

function stubFetch() {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(responseBody, {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('processCommand', () => {
  it('sends text as JSON', async () => {
    const fetchMock = stubFetch()

    await processCommand('отметь планёрку')

    expect(fetchMock).toHaveBeenCalledTimes(1)
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(url).toBe('/api/assistant/command')
    expect(init.method).toBe('POST')
    expect(init.headers).toEqual({ 'Content-Type': 'application/json' })
    expect(init.body).toBe(JSON.stringify({ text: 'отметь планёрку' }))
  })

  it('sends audio as multipart form data', async () => {
    const fetchMock = stubFetch()
    const blob = new Blob(['audio'], { type: 'audio/webm;codecs=opus' })

    await processCommand(blob)

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(init.body).toBeInstanceOf(FormData)

    const form = init.body as FormData
    const audio = form.get('audio')
    expect(audio).toBeInstanceOf(Blob)
    expect((audio as File).name).toBe('recording.webm')
  })

  it('throws API error text for non-ok responses', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ title: 'Не удалось распознать речь' }), {
          status: 422,
          headers: { 'Content-Type': 'application/json' },
        }),
      ),
    )

    await expect(processCommand('тест')).rejects.toThrow('Не удалось распознать речь')
  })
})
