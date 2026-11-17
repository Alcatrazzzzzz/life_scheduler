import type { ScheduleEntry } from '../api'
import { formatFullDate, formatTimeRange, parseDateString } from '../calendar'
import { useDeleteSchedule } from '../queries'
import { Modal } from './Modal'

interface ScheduleDetailsDialogProps {
  entry: ScheduleEntry
  onClose: () => void
}

export function ScheduleDetailsDialog({ entry, onClose }: ScheduleDetailsDialogProps) {
  const removeSchedule = useDeleteSchedule()

  function handleDelete() {
    removeSchedule.mutate(entry.id, { onSuccess: onClose })
  }

  return (
    <Modal title={entry.title} onClose={onClose}>
      <div className="entry-details">
        <dl className="entry-details__meta">
          <div>
            <dt>Дата</dt>
            <dd>{formatFullDate(parseDateString(entry.date))}</dd>
          </div>
          <div>
            <dt>Время</dt>
            <dd>{formatTimeRange(entry.start, entry.end)}</dd>
          </div>
        </dl>

        {entry.notes && <p className="entry-details__notes">{entry.notes}</p>}

        {removeSchedule.isError && (
          <p className="schedule-error">{removeSchedule.error.message}</p>
        )}

        <div className="modal__actions">
          <button type="button" className="btn btn--ghost" onClick={onClose}>
            Закрыть
          </button>
          <button
            type="button"
            className="btn btn--danger"
            onClick={handleDelete}
            disabled={removeSchedule.isPending}
          >
            {removeSchedule.isPending ? 'Удаляю…' : 'Удалить'}
          </button>
        </div>
      </div>
    </Modal>
  )
}
