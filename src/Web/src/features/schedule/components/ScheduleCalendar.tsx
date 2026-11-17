import { useState } from 'react'
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import type { ScheduleEntry } from '../api'
import {
  DEFAULT_EVENT_MINUTES,
  HOURS_IN_DAY,
  addDays,
  addMonths,
  formatMonthTitle,
  formatWeekTitle,
  monthGridDays,
  monthGridRange,
  nextHalfHour,
  startOfToday,
  toDateString,
  toMinutes,
  toTimeString,
  weekDays,
  weekRange,
} from '../calendar'
import { useSchedules } from '../queries'
import { CreateScheduleDialog, type CreateDraft } from './CreateScheduleDialog'
import { MonthView } from './MonthView'
import { ScheduleDetailsDialog } from './ScheduleDetailsDialog'
import { WeekView } from './WeekView'

type CalendarView = 'week' | 'month'

const LAST_MINUTE_OF_DAY = HOURS_IN_DAY * 60 - 1

export function ScheduleCalendar() {
  const [view, setView] = useState<CalendarView>('week')
  const [anchor, setAnchor] = useState(startOfToday)
  const [draft, setDraft] = useState<CreateDraft | null>(null)
  const [selectedEntry, setSelectedEntry] = useState<ScheduleEntry | null>(null)

  const range = view === 'week' ? weekRange(anchor) : monthGridRange(anchor)
  const schedules = useSchedules(range.from, range.to)
  const entries = schedules.data ?? []

  function shiftPeriod(direction: -1 | 1) {
    setAnchor((current) =>
      view === 'week' ? addDays(current, direction * 7) : addMonths(current, direction),
    )
  }

  function openCreateAt(date: string, start: string | null) {
    if (start === null) {
      setDraft({ date, start: null, end: null })
      return
    }

    const startMinutes = toMinutes(start)
    setDraft({
      date,
      start,
      end: toTimeString(Math.min(startMinutes + DEFAULT_EVENT_MINUTES, LAST_MINUTE_OF_DAY)),
    })
  }

  function openCreateNow() {
    const now = new Date()
    const startMinutes = nextHalfHour(now)

    setDraft({
      date: toDateString(now),
      start: toTimeString(startMinutes),
      end: toTimeString(Math.min(startMinutes + DEFAULT_EVENT_MINUTES, LAST_MINUTE_OF_DAY)),
    })
  }

  return (
    <section className="calendar">
      <div className="calendar-toolbar">
        <div className="calendar-toolbar__nav">
          <button
            type="button"
            className="btn btn--ghost btn--icon"
            onClick={() => shiftPeriod(-1)}
            aria-label="Предыдущий период"
          >
            <ChevronLeft size={18} strokeWidth={1.5} aria-hidden="true" />
          </button>
          <button type="button" className="btn btn--ghost" onClick={() => setAnchor(startOfToday())}>
            Сегодня
          </button>
          <button
            type="button"
            className="btn btn--ghost btn--icon"
            onClick={() => shiftPeriod(1)}
            aria-label="Следующий период"
          >
            <ChevronRight size={18} strokeWidth={1.5} aria-hidden="true" />
          </button>
        </div>

        <h1 className="calendar-title">
          {view === 'week' ? formatWeekTitle(anchor) : formatMonthTitle(anchor)}
        </h1>

        <div className="calendar-toolbar__right">
          <div className="segmented" role="group" aria-label="Вид календаря">
            <button type="button" aria-pressed={view === 'week'} onClick={() => setView('week')}>
              Неделя
            </button>
            <button type="button" aria-pressed={view === 'month'} onClick={() => setView('month')}>
              Месяц
            </button>
          </div>

          <button type="button" className="btn btn--primary" onClick={openCreateNow}>
            <Plus size={16} strokeWidth={1.5} className="icon-accent" aria-hidden="true" />
            Новая запись
          </button>
        </div>
      </div>

      {schedules.isError && <p className="schedule-error">{schedules.error.message}</p>}
      {schedules.isPending && <p className="calendar-status">Загрузка…</p>}

      <div className="calendar-panel">
        <div className="calendar-panel__scroll">
          {view === 'week' ? (
            <WeekView
              days={weekDays(anchor)}
              entries={entries}
              onCreateAt={openCreateAt}
              onEntryClick={setSelectedEntry}
            />
          ) : (
            <MonthView
              anchor={anchor}
              days={monthGridDays(anchor)}
              entries={entries}
              onCreateAt={openCreateAt}
              onEntryClick={setSelectedEntry}
            />
          )}
        </div>
      </div>

      {draft && (
        <CreateScheduleDialog
          key={`${draft.date}-${draft.start ?? 'all-day'}`}
          draft={draft}
          onClose={() => setDraft(null)}
        />
      )}

      {selectedEntry && (
        <ScheduleDetailsDialog
          entry={selectedEntry}
          onClose={() => setSelectedEntry(null)}
        />
      )}
    </section>
  )
}
