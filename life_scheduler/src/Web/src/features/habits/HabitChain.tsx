import { Check, Circle, Flame } from 'lucide-react'
import type { HabitSummary } from './api'

interface HabitChainProps {
  chain: HabitSummary[]
  today: string
  busyId?: string
  showStep?: boolean
  onToggle: (habit: HabitSummary, date: string, completed: boolean) => void
}

export function HabitChain({ chain, today, busyId, showStep = true, onToggle }: HabitChainProps) {
  return (
    <ol className={`habit-chain${showStep ? '' : ' habit-chain--plain'}`}>
      {chain.map((habit, index) => {
        const blocked = chain.slice(0, index).some((previous) => !previous.todayDone)
        const due = !habit.todayDone && !blocked
        const className = [
          'habit-chain__item',
          habit.todayDone ? 'habit-chain__item--done' : '',
          due ? 'habit-chain__item--due' : '',
          blocked ? 'habit-chain__item--blocked' : '',
        ]
          .filter(Boolean)
          .join(' ')

        return (
          <li className={className} key={habit.id}>
            {showStep && (
              <span className="habit-chain__step" aria-hidden="true">
                {index + 1}
              </span>
            )}

            <div className="habit-chain__body">
              <span className="habit-chain__title">{habit.title}</span>
              <span className="habit-chain__meta">
                {habit.currentStreak > 0 && (
                  <span className="habit-streak">
                    <Flame size={13} strokeWidth={1.5} aria-hidden="true" />
                    {habit.currentStreak}
                  </span>
                )}
                {index > 0 && (
                  <span className="habit-chain__after">после «{chain[index - 1].title}»</span>
                )}
                {blocked && (
                  <span className="habit-chain__hint">сначала закрой предыдущее</span>
                )}
                {due && <span className="habit-chain__hint">пора</span>}
              </span>
            </div>

            <button
              type="button"
              className="habit-chain__toggle"
              aria-pressed={habit.todayDone}
              aria-label={
                habit.todayDone
                  ? `Снять отметку «${habit.title}»`
                  : `Отметить «${habit.title}»`
              }
              disabled={busyId === habit.id}
              onClick={() => onToggle(habit, today, habit.todayDone)}
            >
              {habit.todayDone ? (
                <Check size={16} strokeWidth={1.8} aria-hidden="true" />
              ) : (
                <Circle size={16} strokeWidth={1.5} aria-hidden="true" />
              )}
            </button>
          </li>
        )
      })}
    </ol>
  )
}
