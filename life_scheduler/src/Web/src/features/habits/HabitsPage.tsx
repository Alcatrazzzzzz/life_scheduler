import { useState } from 'react'
import { Plus } from 'lucide-react'
import type { HabitSummary } from './api'
import { HabitCard } from './HabitCard'
import { HabitChain } from './HabitChain'
import { HabitFormDialog } from './HabitFormDialog'
import { HabitProgress } from './HabitProgress'
import { orderByChain, todayChains } from './habitStats'
import { useCompleteHabit, useDeleteHabit, useHabits, useUncompleteHabit } from './queries'
import './habits.css'

interface FormState {
  habit: HabitSummary | null
}

export function HabitsPage() {
  const habits = useHabits()
  const complete = useCompleteHabit()
  const uncomplete = useUncompleteHabit()
  const remove = useDeleteHabit()
  const [form, setForm] = useState<FormState | null>(null)

  const busyId = complete.isPending
    ? complete.variables?.id
    : uncomplete.isPending
      ? uncomplete.variables?.id
      : undefined
  const actionError = complete.error ?? uncomplete.error ?? remove.error

  function toggle(habit: HabitSummary, date: string, completed: boolean) {
    if (completed) {
      uncomplete.mutate({ id: habit.id, date })
    } else {
      complete.mutate({ id: habit.id, date })
    }
  }

  function handleDelete(habit: HabitSummary) {
    if (window.confirm(`Удалить привычку «${habit.title}» и все её отметки?`)) {
      remove.mutate(habit.id)
    }
  }

  const data = habits.data
  const today = data?.today ?? ''
  const chains = data ? todayChains(data.habits) : { chains: [], singles: [] }
  const ordered = data ? orderByChain(data.habits) : []

  return (
    <main className="habits-page">
      <div className="habits-toolbar">
        <h1 className="habits-title">Привычки</h1>
        <button
          type="button"
          className="btn btn--primary"
          onClick={() => setForm({ habit: null })}
        >
          <Plus size={16} strokeWidth={1.5} className="icon-accent" aria-hidden="true" />
          Новая привычка
        </button>
      </div>

      {habits.isPending && <p className="habits-hint">Загрузка привычек…</p>}
      {habits.isError && <p className="schedule-error">{habits.error.message}</p>}
      {actionError && <p className="schedule-error">{actionError.message}</p>}

      {data && data.habits.length === 0 && (
        <div className="habits-empty">
          <p>Привычек пока нет.</p>
          <p className="habits-hint">
            Создайте первую привычку, задайте повторение и, если хотите, поставьте её
            в цепочку после другой.
          </p>
        </div>
      )}

      {data && data.habits.length > 0 && (
        <>
          <section className="habits-section" aria-labelledby="habits-today">
            <div className="habits-section__head">
              <h2 className="habits-section__title" id="habits-today">
                Цепочки на сегодня
              </h2>
              <span className="habits-section__hint">сегодня</span>
            </div>

            {chains.chains.length === 0 && chains.singles.length === 0 ? (
              <p className="habits-hint">На сегодня ничего не запланировано.</p>
            ) : (
              <div className="habit-chains">
                {chains.chains.map((chain) => (
                  <HabitChain
                    key={chain[0].id}
                    chain={chain}
                    today={today}
                    busyId={busyId}
                    onToggle={toggle}
                  />
                ))}

                {chains.singles.length > 0 && (
                  <div className="habit-chains__single">
                    <h3 className="habit-chains__subtitle">Вне цепочек</h3>
                    {chains.singles.map((habit) => (
                      <HabitChain
                        key={habit.id}
                        chain={[habit]}
                        today={today}
                        busyId={busyId}
                        showStep={false}
                        onToggle={toggle}
                      />
                    ))}
                  </div>
                )}
              </div>
            )}
          </section>

          <section className="habits-section" aria-labelledby="habits-progress">
            <div className="habits-section__head">
              <h2 className="habits-section__title" id="habits-progress">
                Прогресс
              </h2>
              <span className="habits-section__hint">система важнее цели</span>
            </div>
            <HabitProgress habits={data.habits} today={today} />
          </section>

          <section className="habits-section" aria-labelledby="habits-scorecard">
            <div className="habits-section__head">
              <h2 className="habits-section__title" id="habits-scorecard">
                Все привычки
              </h2>
            </div>
            <div className="habit-cards">
              {ordered.map((habit) => (
                <HabitCard
                  key={habit.id}
                  habit={habit}
                  today={today}
                  busy={busyId === habit.id}
                  onToggle={toggle}
                  onEdit={(item) => setForm({ habit: item })}
                  onDelete={handleDelete}
                />
              ))}
            </div>
          </section>
        </>
      )}

      {form && data && (
        <HabitFormDialog
          habit={form.habit}
          habits={data.habits}
          today={today}
          onClose={() => setForm(null)}
        />
      )}
    </main>
  )
}
