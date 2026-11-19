import { Paperclip } from 'lucide-react'
import type { ScheduleEntry } from '../api'
import {
  formatFullDate,
  formatTimeRange,
  groupByDate,
  parseDateString,
} from '../calendar'
import { useScheduleHistory } from '../queries'

interface HistoryViewProps {
  onEntryClick: (entry: ScheduleEntry) => void
}

export function HistoryView({ onEntryClick }: HistoryViewProps) {
  const history = useScheduleHistory()

  if (history.isPending) {
    return <p className="calendar-status">Загрузка…</p>
  }

  if (history.isError) {
    return <p className="schedule-error">{history.error.message}</p>
  }

  const entries = history.data ?? []

  if (entries.length === 0) {
    return <p className="calendar-status">Пока нет прошедших записей.</p>
  }

  const grouped = groupByDate(entries)

  return (
    <div className="history-view">
      {[...grouped.entries()].map(([date, dayEntries]) => (
        <section className="history-day" key={date}>
          <h2 className="history-day__title">{formatFullDate(parseDateString(date))}</h2>

          <ul className="history-list">
            {dayEntries.map((entry) => (
              <li key={`${entry.id}-${entry.date}`}>
                <button
                  type="button"
                  className="history-row"
                  onClick={() => onEntryClick(entry)}
                >
                  <span className="history-row__time">
                    {formatTimeRange(entry.start, entry.end)}
                  </span>
                  <span className="history-row__title">{entry.title}</span>
                  {entry.attachmentCount > 0 && (
                    <span className="history-row__files">
                      <Paperclip size={13} strokeWidth={1.5} aria-hidden="true" />
                      {entry.attachmentCount}
                    </span>
                  )}
                </button>
              </li>
            ))}
          </ul>
        </section>
      ))}
    </div>
  )
}
