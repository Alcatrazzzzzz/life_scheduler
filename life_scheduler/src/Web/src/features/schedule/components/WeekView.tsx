import { useEffect, useRef } from 'react'
import { Repeat, Sun } from 'lucide-react'
import { StreakBadge } from '../../../components/ui/StreakBadge'
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
  isEntryPast,
  isSameDay,
  isToday,
  layoutTimedEntries,
  toDateString,
  toMinutes,
  toTimeString,
} from '../calendar'
import { NOW_TICK_MS } from '../useNow'
import { EntryMark } from './EntryMark'

interface WeekViewProps {
  days: Date[]
  entries: ScheduleEntry[]
  now: Date
  onCreateAt: (date: string, start: string | null) => void
  onEntryClick: (entry: ScheduleEntry) => void
}

const HOURS = Array.from({ length: HOURS_IN_DAY }, (_, hour) => hour)

export function WeekView({ days, entries, now, onCreateAt, onEntryClick }: WeekViewProps) {
  const scrollRef = useRef<HTMLDivElement>(null)
  const grouped = groupByDate(entries)
  const initialHourRef = useRef(
    days.some((day) => isSameDay(day, new Date())) ? new Date().getHours() : 7,
  )

  useEffect(() => {
    const scroller = scrollRef.current
    if (!scroller) return

    const stickyHeight = scroller.querySelector<HTMLElement>('.week-sticky')?.offsetHeight ?? 0
    scroller.scrollTop = Math.max((initialHourRef.current - 1) * HOUR_HEIGHT - stickyHeight, 0)
  }, [])

  // Цель перехода — положение на момент следующего тика: за переход линия проползает ровно
  // минуту и весь интервал показывает реальное время, а не отстаёт от него на минуту.
  const targetSeconds =
    now.getHours() * 3600 + now.getMinutes() * 60 + now.getSeconds() + NOW_TICK_MS / 1000

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
                  {untimed.map((entry) => {
                    const past = isEntryPast(entry, now)
                    const history = entry.result === 'completed' || entry.result === 'missed'

                    return (
                      <button
                        type="button"
                        className={`week-chip${past || history ? ' week-chip--past' : ''}`}
                        key={`${entry.id}-${entry.date}`}
                        onClick={() => onEntryClick(entry)}
                      >
                        <span className="week-chip__title">{entry.title}</span>
                        <EntryMark result={entry.result} past={past} />
                        {entry.kind === 'habit' && entry.streak ? (
                          <StreakBadge value={entry.streak} />
                        ) : null}
                        {entry.recurrence !== 'none' && (
                          <Repeat
                            className="entry-repeat"
                            size={13}
                            strokeWidth={1.5}
                            role="img"
                            aria-label="Повторяется"
                          />
                        )}
                      </button>
                    )
                  })}
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
                const past = isEntryPast(entry, now)
                const history = entry.result === 'completed' || entry.result === 'missed'

                return (
                  <button
                    key={`${entry.id}-${entry.date}`}
                    type="button"
                    className={`week-event${height < 44 ? ' week-event--short' : ''}${
                      past || history ? ' week-event--past' : ''
                    }`}
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
                      <EntryMark result={entry.result} past={past} />
                      {entry.kind === 'habit' && entry.streak ? (
                        <StreakBadge value={entry.streak} />
                      ) : null}
                      {entry.recurrence !== 'none' && (
                        <Repeat
                          className="entry-repeat"
                          size={13}
                          strokeWidth={1.5}
                          role="img"
                          aria-label="Повторяется"
                        />
                      )}
                    </span>
                    <span className="week-event__title">{entry.title}</span>
                  </button>
                )
              })}

              {isToday(day) && (
                <div
                  className="week-now"
                  style={{ transform: `translateY(${(targetSeconds / 3600) * HOUR_HEIGHT}px)` }}
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
