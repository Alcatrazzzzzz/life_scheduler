import { useState } from 'react'
import { useDeleteSchedule, useSchedules } from '../queries'

function formatTime(value: string): string {
  return value.slice(0, 5)
}

function formatTimeRange(start: string | null, end: string | null): string {
  if (start === null || end === null) return 'Без времени'
  return `${formatTime(start)}–${formatTime(end)}`
}

export function ScheduleList() {
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')

  const schedules = useSchedules(from || undefined, to || undefined)
  const removeSchedule = useDeleteSchedule()

  return (
    <section className="schedule-list">
      <h2>Расписание</h2>

      <div className="schedule-filter">
        <label>
          С
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </label>
        <label>
          По
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </label>
      </div>

      {schedules.isPending && <p>Загрузка…</p>}
      {schedules.isError && <p className="schedule-error">{schedules.error.message}</p>}
      {schedules.data?.length === 0 && <p>Записей нет.</p>}

      {schedules.data && schedules.data.length > 0 && (
        <div className="schedule-table">
          <table>
            <thead>
              <tr>
                <th>Дата</th>
                <th>Время</th>
                <th>Название</th>
                <th>Заметки</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {schedules.data.map((entry) => (
                <tr key={entry.id}>
                  <td>{entry.date}</td>
                  <td>{formatTimeRange(entry.start, entry.end)}</td>
                  <td>{entry.title}</td>
                  <td>{entry.notes ?? '—'}</td>
                  <td>
                    <button
                      type="button"
                      className="btn btn--row"
                      onClick={() => removeSchedule.mutate(entry.id)}
                      disabled={removeSchedule.isPending && removeSchedule.variables === entry.id}
                    >
                      Удалить
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {removeSchedule.isError && <p className="schedule-error">{removeSchedule.error.message}</p>}
    </section>
  )
}
