import { useState, type FormEvent } from 'react'
import { useCreateSchedule } from '../queries'

interface FormState {
  title: string
  date: string
  start: string
  end: string
  notes: string
}

function emptyForm(): FormState {
  const today = new Date()
  const month = String(today.getMonth() + 1).padStart(2, '0')
  const day = String(today.getDate()).padStart(2, '0')

  return {
    title: '',
    date: `${today.getFullYear()}-${month}-${day}`,
    start: '',
    end: '',
    notes: '',
  }
}

export function CreateScheduleForm() {
  const [form, setForm] = useState<FormState>(emptyForm)
  const createSchedule = useCreateSchedule()

  function update(field: keyof FormState, value: string) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const notes = form.notes.trim()

    createSchedule.mutate(
      {
        title: form.title.trim(),
        date: form.date,
        start: form.start,
        end: form.end,
        notes: notes.length > 0 ? notes : undefined,
      },
      { onSuccess: () => setForm(emptyForm()) },
    )
  }

  return (
    <form className="schedule-form" onSubmit={handleSubmit}>
      <h2>Новая запись</h2>

      <label>
        Название
        <input value={form.title} onChange={(e) => update('title', e.target.value)} required />
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

      <label>
        Заметки
        <textarea value={form.notes} onChange={(e) => update('notes', e.target.value)} rows={2} />
      </label>

      {createSchedule.isError && <p className="schedule-error">{createSchedule.error.message}</p>}

      <button type="submit" className="btn btn--primary" disabled={createSchedule.isPending}>
        {createSchedule.isPending ? 'Сохраняю…' : 'Сохранить'}
      </button>
    </form>
  )
}
