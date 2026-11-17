import { useState, type FormEvent } from 'react'
import type { ScheduleRecurrence } from '../api'
import { useCreateSchedule } from '../queries'
import { Modal } from './Modal'

export interface CreateDraft {
  date: string
  start: string | null
  end: string | null
}

interface FormState {
  title: string
  date: string
  start: string
  end: string
  notes: string
  untimed: boolean
  recurrence: ScheduleRecurrence
}

interface CreateScheduleDialogProps {
  draft: CreateDraft
  onClose: () => void
}

function formFromDraft(draft: CreateDraft): FormState {
  return {
    title: '',
    date: draft.date,
    start: draft.start ?? '',
    end: draft.end ?? '',
    notes: '',
    untimed: draft.start === null,
    recurrence: 'none',
  }
}

function isRecurrence(value: string): value is ScheduleRecurrence {
  return value === 'none' || value === 'daily' || value === 'weekly'
}

export function CreateScheduleDialog({ draft, onClose }: CreateScheduleDialogProps) {
  const [form, setForm] = useState<FormState>(() => formFromDraft(draft))
  const createSchedule = useCreateSchedule()

  function update<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const notes = form.notes.trim()

    createSchedule.mutate(
      {
        title: form.title.trim(),
        date: form.date,
        start: form.untimed ? null : form.start,
        end: form.untimed ? null : form.end,
        notes: notes.length > 0 ? notes : undefined,
        recurrence: form.recurrence,
      },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal title="Новая запись" onClose={onClose}>
      <form className="schedule-form" onSubmit={handleSubmit}>
        <label>
          Название
          <input
            value={form.title}
            onChange={(e) => update('title', e.target.value)}
            required
          />
        </label>

        <label>
          Дата
          <input
            type="date"
            value={form.date}
            onChange={(e) => update('date', e.target.value)}
            required
          />
        </label>

        <label className="schedule-form-checkbox">
          <input
            type="checkbox"
            checked={form.untimed}
            onChange={(e) => update('untimed', e.target.checked)}
          />
          Без времени
        </label>

        {!form.untimed && (
          <div className="schedule-form-row">
            <label>
              Начало
              <input
                type="time"
                value={form.start}
                onChange={(e) => update('start', e.target.value)}
                required
              />
            </label>

            <label>
              Окончание
              <input
                type="time"
                value={form.end}
                onChange={(e) => update('end', e.target.value)}
                required
              />
            </label>
          </div>
        )}

        <label>
          Повторение
          <select
            value={form.recurrence}
            onChange={(e) => {
              if (isRecurrence(e.target.value)) update('recurrence', e.target.value)
            }}
          >
            <option value="none">Не повторять</option>
            <option value="daily">Каждый день</option>
            <option value="weekly">Каждую неделю</option>
          </select>
        </label>

        <label>
          Заметки
          <textarea
            value={form.notes}
            onChange={(e) => update('notes', e.target.value)}
            rows={2}
          />
        </label>

        {createSchedule.isError && (
          <p className="schedule-error">{createSchedule.error.message}</p>
        )}

        <div className="modal__actions">
          <button type="button" className="btn btn--ghost" onClick={onClose}>
            Отмена
          </button>
          <button type="submit" className="btn btn--primary" disabled={createSchedule.isPending}>
            {createSchedule.isPending ? 'Сохраняю…' : 'Сохранить'}
          </button>
        </div>
      </form>
    </Modal>
  )
}
