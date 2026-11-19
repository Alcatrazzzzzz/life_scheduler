import { useState, type ChangeEvent } from 'react'
import { FileText, Paperclip, Repeat, Trash2 } from 'lucide-react'
import { attachmentDownloadUrl } from '../api'
import type { ScheduleEntry, ScheduleRecurrence } from '../api'
import { MAX_FILE_BYTES, MAX_FILES, MAX_TOTAL_BYTES } from '../attachmentLimits'
import { formatFullDate, formatTimeRange, parseDateString } from '../calendar'
import { formatFileSize } from '../format'
import { useAddAttachment, useDeleteAttachment, useDeleteSchedule, useSchedule } from '../queries'
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
  const detail = useSchedule(entry.id, !removeSchedule.isPending)
  const addAttachment = useAddAttachment(entry.id)
  const removeAttachment = useDeleteAttachment(entry.id)
  const [uploadError, setUploadError] = useState<string | null>(null)

  const recurring = entry.recurrence !== 'none'
  const attachments = detail.data?.attachments ?? []
  const attachmentBusy = removeAttachment.isPending || removeSchedule.isPending
  const deletingSchedule = removeSchedule.isPending || removeAttachment.isPending || addAttachment.isPending

  function handleDelete() {
    removeSchedule.mutate(entry.id, { onSuccess: onClose })
  }

  async function handleFiles(event: ChangeEvent<HTMLInputElement>) {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''
    setUploadError(null)

    let count = attachments.length
    let totalBytes = attachments.reduce((sum, attachment) => sum + attachment.sizeBytes, 0)

    for (const file of files) {
      if (file.size > MAX_FILE_BYTES) {
        setUploadError(`«${file.name}» больше 25 МБ.`)
        return
      }

      if (count >= MAX_FILES) {
        setUploadError('К записи можно приложить не больше 10 файлов.')
        return
      }

      if (totalBytes + file.size > MAX_TOTAL_BYTES) {
        setUploadError('Суммарный размер файлов превысит 100 МБ.')
        return
      }

      try {
        await addAttachment.mutateAsync(file)
        count += 1
        totalBytes += file.size
      } catch (error) {
        setUploadError(error instanceof Error ? error.message : 'Не удалось загрузить файл.')
        return
      }
    }
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

        <section className="attachment-section">
          <div className="attachment-section__head">
            <h3 className="attachment-section__title">Файлы</h3>
            {attachments.length > 0 && (
              <span className="attachment-section__count">{attachments.length}</span>
            )}
          </div>

          {detail.isPending && <p className="attachment-hint">Загрузка файлов…</p>}
          {detail.isError && <p className="schedule-error">{detail.error.message}</p>}

          {attachments.length > 0 ? (
            <ul className="attachment-list">
              {attachments.map((attachment) => (
                <li className="attachment-row" key={attachment.id}>
                  <FileText
                    className="attachment-row__icon"
                    size={15}
                    strokeWidth={1.5}
                    aria-hidden="true"
                  />
                  <a
                    className="attachment-row__name"
                    href={attachmentDownloadUrl(entry.id, attachment.id)}
                  >
                    {attachment.fileName}
                  </a>
                  <span className="attachment-row__size">
                    {formatFileSize(attachment.sizeBytes)}
                  </span>
                  <button
                    type="button"
                    className="btn btn--row attachment-row__remove"
                    onClick={() => removeAttachment.mutate(attachment.id)}
                    disabled={attachmentBusy}
                    aria-label={`Удалить ${attachment.fileName}`}
                  >
                    <Trash2 size={14} strokeWidth={1.5} aria-hidden="true" />
                  </button>
                </li>
              ))}
            </ul>
          ) : (
            !detail.isPending && <p className="attachment-hint">Файлов пока нет.</p>
          )}

          <label
            className={`attachment-upload${
              attachmentBusy ? ' attachment-upload--busy' : ''
            }`}
          >
            <Paperclip size={14} strokeWidth={1.5} aria-hidden="true" />
            <span>{addAttachment.isPending ? 'Загружаю…' : 'Приложить файлы'}</span>
            <input
              type="file"
              multiple
              hidden
              onChange={handleFiles}
              disabled={attachmentBusy}
            />
          </label>

          {uploadError && <p className="schedule-error">{uploadError}</p>}
          {removeAttachment.isError && (
            <p className="schedule-error">{removeAttachment.error.message}</p>
          )}
        </section>

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
            disabled={deletingSchedule}
          >
            {removeSchedule.isPending ? 'Удаляю…' : 'Удалить'}
          </button>
        </div>
      </div>
    </Modal>
  )
}
