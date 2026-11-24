import { useState, type FormEvent } from 'react'
import { DateField } from '../../components/ui/DateField'
import { HabitExtraFields } from '../../components/ui/HabitExtraFields'
import { Modal } from '../../components/ui/Modal'
import { SelectField, type SelectOption } from '../../components/ui/SelectField'
import { TimeField } from '../../components/ui/TimeField'
import type { HabitInput, HabitRecurrence, HabitSummary } from './api'
import { useCreateHabit, useUpdateHabit } from './queries'

const RECURRENCE_OPTIONS: SelectOption<HabitRecurrence>[] = [
  { value: 'daily', label: 'Каждый день' },
  { value: 'weekly', label: 'Каждую неделю' },
]

interface FormState {
  title: string
  date: string
  start: string
  end: string
  untimed: boolean
  recurrence: HabitRecurrence
  reward: string
  stackAfterId: string
  notes: string
}

interface HabitFormDialogProps {
  habit: HabitSummary | null
  habits: HabitSummary[]
  today: string
  onClose: () => void
}

function formFromHabit(habit: HabitSummary | null, today: string): FormState {
  return {
    title: habit?.title ?? '',
    date: habit?.date ?? today,
    start: habit?.start ?? '',
    end: habit?.end ?? '',
    untimed: habit === null ? true : habit.start === null,
    recurrence: habit?.recurrence ?? 'daily',
    reward: habit?.reward ?? '',
    stackAfterId: habit?.stackAfterId ?? '',
    notes: habit?.notes ?? '',
  }
}

export function HabitFormDialog({ habit, habits, today, onClose }: HabitFormDialogProps) {
  const [form, setForm] = useState<FormState>(() => formFromHabit(habit, today))
  const [error, setError] = useState<string | null>(null)
  const createHabit = useCreateHabit()
  const updateHabit = useUpdateHabit()
  const busy = createHabit.isPending || updateHabit.isPending
  const mutationError = createHabit.error ?? updateHabit.error

  function update<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    if (form.title.trim().length === 0) {
      setError('Название обязательно.')
      return
    }

    if (!form.untimed && form.end <= form.start) {
      setError('Время окончания должно быть позже начала.')
      return
    }

    const input: HabitInput = {
      title: form.title.trim(),
      date: form.date,
      start: form.untimed ? null : form.start,
      end: form.untimed ? null : form.end,
      notes: form.notes.trim() || null,
      recurrence: form.recurrence,
      reward: form.reward.trim() || null,
      stackAfterId: form.stackAfterId || null,
    }

    try {
      if (habit) {
        await updateHabit.mutateAsync({ id: habit.id, input })
      } else {
        await createHabit.mutateAsync(input)
      }
      onClose()
    } catch {
      // ошибка показывается из mutationError
    }
  }

  return (
    <Modal title={habit ? 'Редактировать привычку' : 'Новая привычка'} onClose={onClose}>
      <form className="habit-form" onSubmit={handleSubmit}>
        <fieldset className="habit-form__fieldset" disabled={busy}>
          <label>
            Название
            <input
              value={form.title}
              onChange={(event) => update('title', event.target.value)}
              required
              autoFocus
            />
          </label>

          <DateField
            label="Старт серии"
            value={form.date}
            onChange={(value) => update('date', value)}
          />
          <p className="habit-form__hint">
            С этой даты считается серия; более ранние дни в статистику не попадут.
          </p>

          <label className="habit-form__checkbox">
            <input
              type="checkbox"
              checked={form.untimed}
              onChange={(event) => update('untimed', event.target.checked)}
            />
            Без времени
          </label>

          {!form.untimed && (
            <div className="habit-form__row">
              <TimeField
                label="Начало"
                value={form.start}
                onChange={(value) => update('start', value)}
              />
              <TimeField
                label="Окончание"
                value={form.end}
                onChange={(value) => update('end', value)}
              />
            </div>
          )}

          <SelectField
            label="Повторение"
            value={form.recurrence}
            options={RECURRENCE_OPTIONS}
            onChange={(value) => update('recurrence', value)}
          />

          <label>
            Заметки
            <textarea
              value={form.notes}
              rows={2}
              onChange={(event) => update('notes', event.target.value)}
            />
          </label>

          <HabitExtraFields
            reward={form.reward}
            stackAfterId={form.stackAfterId}
            habits={habits}
            excludeId={habit?.id}
            onChange={(patch) =>
              setForm((current) => ({ ...current, ...patch }))
            }
          />
        </fieldset>

        {(error || mutationError) && (
          <p className="schedule-error">{error ?? mutationError?.message}</p>
        )}

        <div className="modal__actions">
          <button type="button" className="btn btn--ghost" onClick={onClose}>
            Отмена
          </button>
          <button type="submit" className="btn btn--primary" disabled={busy}>
            {busy ? 'Сохраняю…' : 'Сохранить'}
          </button>
        </div>
      </form>
    </Modal>
  )
}
