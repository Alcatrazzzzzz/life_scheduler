import type { DayAggregate } from './habitStats'

interface HabitHeatmapProps {
  weeks: DayAggregate[][]
  today: string
  label?: string
  size?: 'md' | 'lg'
}

function level(scheduled: number, completed: number): number {
  if (scheduled === 0) return 0
  return Math.ceil((completed / scheduled) * 4)
}

export function HabitHeatmap({ weeks, today, label, size = 'md' }: HabitHeatmapProps) {
  return (
    <div
      className={`habit-heatmap habit-heatmap--${size}`}
      role="img"
      aria-label={label ?? 'Тепловая карта выполнения привычек'}
    >
      {weeks.map((week) => (
        <div className="habit-heatmap__week" key={week[0]?.date ?? ''}>
          {week.map((day) => {
            const future = day.date > today
            const heat = future ? 0 : level(day.scheduled, day.completed)
            const state = future ? 'future' : day.scheduled === 0 ? 'off' : `heat-${heat}`

            return (
              <span
                key={day.date}
                className={`habit-heatmap__cell habit-heatmap__cell--${state}`}
                title={`${day.date}: ${day.completed} из ${day.scheduled}`}
              />
            )
          })}
        </div>
      ))}
    </div>
  )
}
