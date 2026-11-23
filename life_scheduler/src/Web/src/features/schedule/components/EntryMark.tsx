import { Check, X } from 'lucide-react'
import type { ScheduleResult } from '../api'

interface EntryMarkProps {
  result?: ScheduleResult
  past?: boolean
}

/** Отметка записи в календаре: выполнение, пропуск или просто прошедшая дата. */
export function EntryMark({ result, past }: EntryMarkProps) {
  if (result === 'completed') {
    return (
      <Check
        className="entry-result entry-result--completed"
        size={13}
        strokeWidth={1.5}
        role="img"
        aria-label="Выполнено"
      />
    )
  }

  if (result === 'missed') {
    return (
      <X
        className="entry-result entry-result--missed"
        size={13}
        strokeWidth={1.5}
        role="img"
        aria-label="Пропущено"
      />
    )
  }

  if (past) {
    return (
      <Check
        className="entry-past"
        size={13}
        strokeWidth={1.5}
        role="img"
        aria-label="Прошло"
      />
    )
  }

  return null
}
