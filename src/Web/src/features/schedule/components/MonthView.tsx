import { Repeat } from 'lucide-react'
import type { ScheduleEntry } from '../api'
import {
  WEEKDAY_LABELS,
  groupByDate,
  isToday,
  toDateString,
} from '../calendar'

interface MonthViewProps {
  anchor: Date
  days: Date[]
  entries: ScheduleEntry[]
  onCreateAt: (date: string, start: string | null) => void
  onEntryClick: (entry: ScheduleEntry) => void
}

const MAX_CHIPS = 3

export function MonthView({ anchor, days, entries, onCreateAt, onEntryClick }: MonthViewProps) {
  const grouped = groupByDate(entries)

  return (
    <div className="month-view">
      <div className="month-weekdays">
        {WEEKDAY_LABELS.map((label) => (
          <span key={label}>{label}</span>
        ))}
      </div>

      <div className="month-grid">
        {days.map((day) => {
          const dayKey = toDateString(day)
          const dayEntries = grouped.get(dayKey) ?? []
          const visible = dayEntries.slice(0, MAX_CHIPS)
          const hidden = dayEntries.length - visible.length
          const outside = day.getMonth() !== anchor.getMonth()
          const classes = [
            'month-cell',
            outside ? 'month-cell--outside' : '',
            isToday(day) ? 'month-cell--today' : '',
          ]
            .filter(Boolean)
            .join(' ')

          return (
            <div className={classes} key={dayKey}>
              <button
                type="button"
                className="month-cell__surface"
                aria-label={`Новая запись ${dayKey}`}
                onClick={() => onCreateAt(dayKey, null)}
              />
              <span className="month-cell__number">{day.getDate()}</span>

              <div className="month-cell__entries">
                {visible.map((entry) => (
                  <button
                    type="button"
                    className="month-chip"
                    key={`${entry.id}-${entry.date}`}
                    onClick={() => onEntryClick(entry)}
                  >
                    {entry.start && (
                      <span className="month-chip__time">{entry.start.slice(0, 5)}</span>
                    )}
                    <span className="month-chip__title">{entry.title}</span>
                    {entry.recurrence !== 'none' && (
                      <Repeat
                        className="entry-repeat month-chip__repeat"
                        size={13}
                        strokeWidth={1.5}
                        role="img"
                        aria-label="Повторяется"
                      />
                    )}
                  </button>
                ))}
                {hidden > 0 && <span className="month-cell__more">ещё {hidden}</span>}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
