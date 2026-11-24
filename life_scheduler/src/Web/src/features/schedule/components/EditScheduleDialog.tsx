import { useState, type FormEvent } from 'react'
import { DateField } from '../../../components/ui/DateField'
import { HabitExtraFields } from '../../../components/ui/HabitExtraFields'
import { Modal } from '../../../components/ui/Modal'
import { SelectField, type SelectOption } from '../../../components/ui/SelectField'
import { TimeField } from '../../../components/ui/TimeField'
import type {
  ScheduleDetail,
  ScheduleKind,
  ScheduleRecurrence,
} from '../api'
import { useHabitOptions, useUpdateSchedule } from '../queries'

const KIND_OPTIONS: SelectOption<ScheduleKind>[] = [
  { value: 'event', label: 'Событие' },
  { value: 'task', label: 'Задача' },
  { value: 'habit', label: 'Привычка' },
]

const RECURRENCE_OPTIONS: SelectOption<ScheduleRecurrence>[] = [
  { value: 'none', label: 'Не повторять' },
  { value: 'daily', label: 'Каждый день' },
  { value: 'weekly', label: 'Каждую неделю' },
]

const HABIT_RECURRENCE_OPTIONS = RECURRENCE_OPTIONS.filter(
  (option) => option.value !== 'none',
)

interface FormState {
  title: string
  date: string
  start: string
  end: string
  untimed: boolean
  recurrence: ScheduleRecurrence
  kind: ScheduleKind
  reward: string
  stackAfterId: string
  notes: string
}

interface EditScheduleDialogProps {
  detail: ScheduleDetail
  onClose: () => void
}

function formFromDetail(detail: ScheduleDetail): FormState {
  return {
    title: detail.title,
    date: detail.date,
    start: detail.start ?? '',
    end: detail.end ?? '',
    untimed: detail.start === null,
    recurrence: detail.recurrence,
    kind: detail.kind,
    reward: detail.reward ?? '',
    stackAfterId: detail.stackAfterId ?? '',
    notes: detail.notes ?? '',
  }
}

export function EditScheduleDialog({ detail, onClose }: EditScheduleDialogProps) {
  const [form, setForm] = useState<FormState>(() => formFromDetail(detail))
  const [error, setError] = useState<string | null>(null)
  const updateSchedule = useUpdateSchedule()
  const habitOptions = useHabitOptions(form.kind === 'habit')

  function update<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function updateKind(kind: ScheduleKind) {
    setForm((current) => ({
      ...current,
      kind,
      recurrence:
        kind === 'task'
          ? 'none'
          : kind === 'habit' && current.recurrence === 'none'
            ? 'daily'
            : current.recurrence,
      reward: kind === 'habit' ? current.reward : '',
      stackAfterId: kind === 'habit' ? current.stackAfterId : '',
    }))
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

    try {
      await updateSchedule.mutateAsync({
        id: detail.id,
        input: {
          title: form.title.trim(),
          date: form.date,
          start: form.untimed ? null : form.start,
          end: form.untimed ? null : form.end,
          notes: form.notes.trim() || undefined,
          recurrence: form.recurrence,
          kind: form.kind,
          reward: form.kind === 'habit' && form.reward.trim() ? form.reward.trim() : null,
          stackAfterId:
            form.kind === 'habit' && form.stackAfterId ? form.stackAfterId : null,
        },
      })
      onClose()
    } catch {
      // ошибка показывается из updateSchedule.error
    }
  }

  return (
    <Modal title="Редактировать запись" onClose={onClose}>
      <form className="schedule-form" onSubmit={handleSubmit}>
        <fieldset className="schedule-form__fieldset" disabled={updateSchedule.isPending}>
          <label>
            Название
            <input
              value={form.title}
              onChange={(event) => update('title', event.target.value)}
              required
              autoFocus
            />
          </label>

          <SelectField
            label="Тип"
            value={form.kind}
            options={KIND_OPTIONS}
            onChange={updateKind}
          />

          <DateField
            label="Дата"
            value={form.date}
            onChange={(value) => update('date', value)}
          />

          <label className="schedule-form-checkbox">
            <input
              type="checkbox"
              checked={form.untimed}
              onChange={(event) => update('untimed', event.target.checked)}
            />
            Без времени
          </label>

          {!form.untimed && (
            <div className="schedule-form-row">
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

          {form.kind !== 'task' && (
            <SelectField
              label="Повторение"
              value={form.recurrence}
              options={
                form.kind === 'habit' ? HABIT_RECURRENCE_OPTIONS : RECURRENCE_OPTIONS
              }
              onChange={(value) => update('recurrence', value)}
            />
          )}

          {form.kind === 'habit' && (
            <HabitExtraFields
              reward={form.reward}
              stackAfterId={form.stackAfterId}
              habits={habitOptions.data ?? []}
              excludeId={detail.id}
              onChange={(patch) =>
                setForm((current) => ({ ...current, ...patch }))
              }
              disabled={updateSchedule.isPending}
            />
          )}

          <label>
            Заметки
            <textarea
              value={form.notes}
              rows={2}
              onChange={(event) => update('notes', event.target.value)}
            />
          </label>
        </fieldset>

        {(error || updateSchedule.error) && (
          <p className="schedule-error">{error ?? updateSchedule.error?.message}</p>
        )}

        <div className="modal__actions">
          <button type="button" className="btn btn--ghost" onClick={onClose}>
            Отмена
          </button>
          <button
            type="submit"
            className="btn btn--primary"
            disabled={updateSchedule.isPending}
          >
            {updateSchedule.isPending ? 'Сохраняю…' : 'Сохранить'}
          </button>
        </div>
      </form>
    </Modal>
  )
}
