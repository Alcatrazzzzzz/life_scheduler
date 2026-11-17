import { useEffect, useRef, useState } from 'react'
import { Sun } from 'lucide-react'
import type { ScheduleEntry } from '../api'
import {
  HOURS_IN_DAY,
  HOUR_HEIGHT,
  MIN_EVENT_MINUTES,
  formatClock,
  formatDayMonth,
  formatTimeRange,
  formatWeekdayShort,
  groupByDate,
  isSameDay,
  isToday,
  layoutTimedEntries,
  toDateString,
  toMinutes,
  toTimeString,
} from '../calendar'

interface WeekViewProps {
  days: Date[]
  entries: ScheduleEntry[]
  onCreateAt: (date: string, start: string | null) => void
  onEntryClick: (entry: ScheduleEntry) => void
}

const HOURS = Array.from({ length: HOURS_IN_DAY }, (_, hour) => hour)

export function WeekView({ days, entries, onCreateAt, onEntryClick }: WeekViewProps) {
  const scrollRef = useRef<HTMLDivElement>(null)
  const grouped = groupByDate(entries)
  const [now, setNow] = useState(() => new Date())
  const initialHourRef = useRef(
    days.some((day) => isSameDay(day, new Date())) ? new Date().getHours() : 7,
  )

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 60_000)
    return () => window.clearInterval(timer)
  }, [])

  useEffect(() => {
    const scroller = scrollRef.current
    if (!scroller) return

    const stickyHeight = scroller.querySelector<HTMLElement>('.week-sticky')?.offsetHeight ?? 0
    scroller.scrollTop = Math.max((initialHourRef.current - 1) * HOUR_HEIGHT - stickyHeight, 0)
  }, [])

  const nowMinutes = now.getHours() * 60 + now.getMinutes()

  return (
    <div className="week-view" ref={scrollRef}>
      <div className="week-sticky">
        <div className="week-header">
          <div className="week-header__gutter" />
          {days.map((day) => {
            const classes = ['week-header__day']
            if (isToday(day)) classes.push('week-header__day--today')
            if (day.getDay() === 0 || day.getDay() === 6) classes.push('week-header__day--weekend')

            return (
              <div key={toDateString(day)} className={classes.join(' ')}>
                <span className="week-header__weekday">{formatWeekdayShort(day)}</span>
                <span className="week-header__number">{day.getDate()}</span>
                <span className="week-header__month">{formatDayMonth(day).split(' ')[1]}</span>
              </div>
            )
          })}
        </div>

        <div className="week-allday">
          <div className="week-allday__label" role="img" aria-label="Весь день">
            <Sun size={14} strokeWidth={1.5} aria-hidden="true" />
          </div>
          {days.map((day) => {
            const dayKey = toDateString(day)
            const untimed = (grouped.get(dayKey) ?? []).filter((entry) => entry.start === null)
            const isWeekend = day.getDay() === 0 || day.getDay() === 6

            return (
              <div
                className={`week-allday__cell${isWeekend ? ' week-allday__cell--weekend' : ''}`}
                key={dayKey}
              >
                <button
                  type="button"
                  className="week-allday__surface"
                  aria-label={`Новая запись без времени ${dayKey}`}
                  onClick={() => onCreateAt(dayKey, null)}
                />
                <div className="week-allday__chips">
                  {untimed.map((entry) => (
                    <button
                      type="button"
                      className="week-chip"
                      key={entry.id}
                      onClick={() => onEntryClick(entry)}
                    >
                      {entry.title}
                    </button>
                  ))}
                </div>
              </div>
            )
          })}
        </div>
      </div>

      <div className="week-grid" style={{ height: HOURS_IN_DAY * HOUR_HEIGHT }}>
        <div className="week-gutter">
          {HOURS.slice(1).map((hour) => (
            <div key={hour} className="week-gutter__label" style={{ top: hour * HOUR_HEIGHT }}>
              {toTimeString(hour * 60)}
            </div>
          ))}
        </div>

        {days.map((day) => {
          const dayKey = toDateString(day)
          const dayEntries = grouped.get(dayKey) ?? []
          const positioned = layoutTimedEntries(dayEntries)
          const isWeekend = day.getDay() === 0 || day.getDay() === 6

          return (
            <div key={dayKey} className={`week-day${isWeekend ? ' week-day--weekend' : ''}`}>
              {HOURS.map((hour) => (
                <div key={hour} className="week-hour-line" style={{ top: hour * HOUR_HEIGHT }} />
              ))}

              {HOURS.map((hour) => {
                const startTime = toTimeString(hour * 60)

                return (
                  <button
                    key={hour}
                    type="button"
                    className="week-slot"
                    style={{ top: hour * HOUR_HEIGHT, height: HOUR_HEIGHT }}
                    tabIndex={-1}
                    aria-label={`Новая запись ${dayKey} в ${startTime}`}
                    onClick={() => onCreateAt(dayKey, startTime)}
                  />
                )
              })}

              {positioned.map(({ entry, column, columns }) => {
                const start = toMinutes(entry.start ?? '00:00')
                const end = Math.max(
                  entry.end === null ? start : toMinutes(entry.end),
                  start + MIN_EVENT_MINUTES,
                )
                const height = ((end - start) / 60) * HOUR_HEIGHT

                return (
                  <button
                    key={entry.id}
                    type="button"
                    className={`week-event${height < 44 ? ' week-event--short' : ''}`}
                    style={{
                      top: (start / 60) * HOUR_HEIGHT,
                      height,
                      left: `calc(${(column / columns) * 100}% + 2px)`,
                      width: `calc(${100 / columns}% - 4px)`,
                    }}
                    onClick={() => onEntryClick(entry)}
                  >
                    <span className="week-event__time">
                      {formatTimeRange(entry.start, entry.end)}
                    </span>
                    <span className="week-event__title">{entry.title}</span>
                  </button>
                )
              })}

              {isToday(day) && (
                <div
                  className="week-now"
                  style={{ top: (nowMinutes / 60) * HOUR_HEIGHT }}
                  aria-hidden="true"
                >
                  <span className="week-now__dot" />
                  <span className="week-now__label">{formatClock(now)}</span>
                </div>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
