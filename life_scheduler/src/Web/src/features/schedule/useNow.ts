import { useEffect, useState } from 'react'

/** Длительность перехода линии «сейчас» в CSS (--duration-minute) должна совпадать с этим шагом. */
export const NOW_TICK_MS = 60_000

/** Текущее время с тиком на границе минуты: подписи и линия «сейчас» меняются вместе с часами. */
export function useNow(): Date {
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    let intervalId: number | undefined

    const timeoutId = window.setTimeout(() => {
      setNow(new Date())
      intervalId = window.setInterval(() => setNow(new Date()), NOW_TICK_MS)
    }, NOW_TICK_MS - (Date.now() % NOW_TICK_MS))

    return () => {
      window.clearTimeout(timeoutId)
      if (intervalId !== undefined) window.clearInterval(intervalId)
    }
  }, [])

  return now
}
