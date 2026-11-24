import { Flame, Pencil, Trash2 } from 'lucide-react'
import type { HabitSummary } from './api'
import {
  currentWeekDays,
  habitHeatmapWeeks,
  habitSparkline,
} from './habitStats'
import { HabitHeatmap } from './HabitHeatmap'
import { HabitSparkline } from './HabitSparkline'
import { HabitWeekGrid } from './HabitWeekGrid'

interface HabitCardProps {
  habit: HabitSummary
  today: string
  busy?: boolean
  onToggle: (habit: HabitSummary, date: string, completed: boolean) => void
  onEdit: (habit: HabitSummary) => void
  onDelete: (habit: HabitSummary) => void
}

export function HabitCard({
  habit,
  today,
  busy,
  onToggle,
  onEdit,
  onDelete,
}: HabitCardProps) {
  const atRisk =
    habit.missedTwice ||
    (habit.lastOccurrenceMissed && habit.todayScheduled && !habit.todayDone)

  const warning = habit.missedTwice
    ? 'Пропущено дважды — восстановите цепочку.'
    : atRisk
      ? 'Вчера пропущено — сегодня не пропустите.'
      : null

  return (
    <article className="habit-card">
      <header className="habit-card__head">
        <h3 className="habit-card__title">{habit.title}</h3>
        <div className="habit-card__streak">
          <span className="habit-streak habit-streak--large">
            <Flame size={15} strokeWidth={1.5} aria-hidden="true" />
            {habit.currentStreak}
          </span>
          <span className="habit-card__best">рекорд {habit.longestStreak}</span>
        </div>
      </header>

      <div className="habit-card__meta">
        {habit.stackAfterTitle && (
          <span className="habit-card__tag">После: {habit.stackAfterTitle}</span>
        )}
        {habit.reward && <span className="habit-card__tag">Награда: {habit.reward}</span>}
        {habit.completionRate !== null && (
          <span className="habit-card__tag">
            {Math.round(habit.completionRate * 100)}% за 13 недель
          </span>
        )}
      </div>

      <HabitWeekGrid
        days={currentWeekDays(habit)}
        today={today}
        busy={busy}
        onToggle={(date, completed) => onToggle(habit, date, completed)}
      />

      <div className="habit-card__charts">
        <HabitHeatmap
          weeks={habitHeatmapWeeks(habit, today, 13)}
          today={today}
          label={`Тепловая карта привычки «${habit.title}»`}
        />
        <HabitSparkline
          values={habitSparkline(habit, today, 8)}
          label={`Динамика привычки «${habit.title}»`}
        />
      </div>

      {warning && <p className="habit-card__warning">{warning}</p>}

      <div className="habit-card__actions">
        <button type="button" className="btn btn--ghost btn--row" onClick={() => onEdit(habit)}>
          <Pencil size={14} strokeWidth={1.5} aria-hidden="true" />
          Редактировать
        </button>
        <button
          type="button"
          className="btn btn--ghost btn--row habit-card__delete"
          onClick={() => onDelete(habit)}
        >
          <Trash2 size={14} strokeWidth={1.5} aria-hidden="true" />
          Удалить
        </button>
      </div>
    </article>
  )
}
