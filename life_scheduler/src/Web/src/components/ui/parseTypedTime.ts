function pad2(value: number): string {
  return String(value).padStart(2, '0')
}

/** Принимает «9:30», «0930», «930», «9.30». Возвращает «HH:MM» или null. */
export function parseTypedTime(raw: string): string | null {
  const cleaned = raw.trim().replace(/[.,\s]/g, ':')

  let hour: number
  let minute: number

  const withColon = /^(\d{1,2}):(\d{1,2})$/.exec(cleaned)

  if (withColon) {
    hour = Number(withColon[1])
    minute = Number(withColon[2])
  } else if (/^\d{1,4}$/.test(cleaned)) {
    if (cleaned.length <= 2) {
      hour = Number(cleaned)
      minute = 0
    } else {
      const splitAt = cleaned.length === 3 ? 1 : 2
      hour = Number(cleaned.slice(0, splitAt))
      minute = Number(cleaned.slice(splitAt))
    }
  } else {
    return null
  }

  if (hour > 23 || minute > 59) return null

  return `${pad2(hour)}:${pad2(minute)}`
}
