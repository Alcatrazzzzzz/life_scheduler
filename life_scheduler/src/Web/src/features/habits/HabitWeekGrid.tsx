import type { HabitDayStatus } from './api'
import { dayState, weekdayShort, type DayState } from './habitStats'

const STATE_LABEL: Record<DayState, string> = {
  off: 'нет в плане',
  planned: 'запланировано',
  done: 'выполнено',
  missed: 'пропущено',
  future: 'будет',
}

interface HabitWeekGridProps {
  days: HabitDayStatus[]
  today: string
  busy?: boolean
  onToggle: (date: string, completed: boolean) => void
}

export function HabitWeekGrid({ days, today, busy, onToggle }: HabitWeekGridProps) {
  return (
    <div className="habit-week" role="group" aria-label="Дни текущей недели">
      {days.map((day) => {
        const state = dayState(day, today)
        const interactive = day.scheduled && day.date <= today

        return (
          <button
            key={day.date}
            type="button"
            className={`habit-week__day habit-week__day--${state}`}
            disabled={!interactive || busy}
            aria-pressed={day.completed}
            aria-label={`${weekdayShort(day.date)} ${day.date}: ${STATE_LABEL[state]}`}
            onClick={() => onToggle(day.date, day.completed)}
          >
            <span className="habit-week__label">{weekdayShort(day.date)}</span>
            <span className="habit-week__mark" aria-hidden="true" />
          </button>
        )
      })}
    </div>
  )
}
