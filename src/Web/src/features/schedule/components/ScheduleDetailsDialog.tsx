import { Repeat } from 'lucide-react'
import type { ScheduleEntry, ScheduleRecurrence } from '../api'
import { formatFullDate, formatTimeRange, parseDateString } from '../calendar'
import { useDeleteSchedule } from '../queries'
import { Modal } from './Modal'

const RECURRENCE_LABELS: Record<ScheduleRecurrence, string> = {
  none: 'Не повторять',
  daily: 'Каждый день',
  weekly: 'Каждую неделю',
}

interface ScheduleDetailsDialogProps {
  entry: ScheduleEntry
  onClose: () => void
}

export function ScheduleDetailsDialog({ entry, onClose }: ScheduleDetailsDialogProps) {
  const removeSchedule = useDeleteSchedule()
  const recurring = entry.recurrence !== 'none'

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
          {recurring && (
            <div>
              <dt>Повторение</dt>
              <dd>{RECURRENCE_LABELS[entry.recurrence]}</dd>
            </div>
          )}
        </dl>

        {recurring && (
          <p className="entry-details__hint">
            <Repeat size={14} strokeWidth={1.5} aria-hidden="true" />
            <span>Запись повторяется: удаление уберёт все повторы.</span>
          </p>
        )}

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
