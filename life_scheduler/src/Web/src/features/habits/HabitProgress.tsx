import type { HabitSummary } from './api'
import {
  aggregateDays,
  atRiskHabits,
  bestCurrentStreak,
  currentWeekStats,
  heatmapWeeks,
  previousWeekStats,
  totalCompleted,
  trend,
  weekBuckets,
} from './habitStats'
import { HabitHeatmap } from './HabitHeatmap'
import { WeeklyBars } from './WeeklyBars'

interface HabitProgressProps {
  habits: HabitSummary[]
  today: string
}

export function HabitProgress({ habits, today }: HabitProgressProps) {
  const aggregates = aggregateDays(habits)
  const current = currentWeekStats(aggregates, today)
  const previous = previousWeekStats(aggregates, today)
  const trendValue = trend(current, previous)
  const best = bestCurrentStreak(habits)
  const risk = atRiskHabits(habits).length
  const total = totalCompleted(aggregates, today)

  const tiles = [
    { label: 'За неделю', value: `${current.completed} / ${current.scheduled}` },
    { label: 'Лучшая серия', value: `${best} дн.` },
    {
      label: 'Тренд к прошлой неделе',
      value:
        trendValue === null
          ? '—'
          : `${trendValue > 0 ? '+' : ''}${trendValue} п.п.`,
    },
    { label: 'Требуют внимания', value: String(risk) },
    { label: 'Отметок за 13 недель', value: String(total) },
  ]

  return (
    <section className="habit-progress" aria-label="Прогресс привычек">
      <div className="habit-progress__tiles">
        {tiles.map((tile) => (
          <div className="habit-stat" key={tile.label}>
            <span className="habit-stat__label">{tile.label}</span>
            <strong className="habit-stat__value">{tile.value}</strong>
          </div>
        ))}
      </div>

      <div className="habit-progress__charts">
        <div className="habit-progress__panel">
          <h3 className="habit-progress__title">Тепловая карта за 13 недель</h3>
          <HabitHeatmap
            weeks={heatmapWeeks(aggregates, today, 13)}
            today={today}
            size="lg"
            label="Общая тепловая карта выполнения привычек за 13 недель"
          />
          <div className="habit-heatmap__legend" aria-hidden="true">
            <span>0%</span>
            {[0, 1, 2, 3, 4].map((step) => (
              <span
                key={step}
                className={`habit-heatmap__cell habit-heatmap__cell--heat-${step}`}
              />
            ))}
            <span>100%</span>
          </div>
        </div>

        <div className="habit-progress__panel">
          <h3 className="habit-progress__title">По неделям</h3>
          <WeeklyBars buckets={weekBuckets(aggregates, today, 8)} />
        </div>
      </div>
    </section>
  )
}
