import { useState, type ChangeEvent, type FormEvent } from 'react'
import { FileText, Paperclip, Trash2 } from 'lucide-react'
import type { ScheduleKind, ScheduleRecurrence } from '../api'
import { MAX_FILE_BYTES, MAX_FILES, MAX_TOTAL_BYTES } from '../attachmentLimits'
import { formatFileSize } from '../format'
import { useAddAttachments, useCreateSchedule } from '../queries'
import { DateField } from '../../../components/ui/DateField'
import { SelectField, type SelectOption } from '../../../components/ui/SelectField'
import { TimeField } from '../../../components/ui/TimeField'
import { Modal } from './Modal'

const KIND_OPTIONS: SelectOption<ScheduleKind>[] = [
  { value: 'event', label: 'Событие' },
  { value: 'task', label: 'Задача' },
  { value: 'habit', label: 'Привычка' },
]

const EVENT_RECURRENCE_OPTIONS: SelectOption<ScheduleRecurrence>[] = [
  { value: 'none', label: 'Не повторять' },
  { value: 'daily', label: 'Каждый день' },
  { value: 'weekly', label: 'Каждую неделю' },
]

const HABIT_RECURRENCE_OPTIONS = EVENT_RECURRENCE_OPTIONS.filter(
  (option) => option.value !== 'none',
)

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
  kind: ScheduleKind
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
    kind: 'event',
  }
}

export function CreateScheduleDialog({ draft, onClose }: CreateScheduleDialogProps) {
  const [form, setForm] = useState<FormState>(() => formFromDraft(draft))
  const [files, setFiles] = useState<File[]>([])
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [createdId, setCreatedId] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const createSchedule = useCreateSchedule()
  const addAttachments = useAddAttachments()

  const busy = isSubmitting || createSchedule.isPending || addAttachments.isPending
  const locked = busy || createdId !== null
  const mutationError =
    submitError ?? createSchedule.error?.message ?? addAttachments.error?.message

  function update<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function updateKind(kind: ScheduleKind) {
    setForm((current) => ({
      ...current,
      kind,
      // Задача разовая, привычка обязана повторяться — не даём несогласованное состояние.
      recurrence:
        kind === 'task'
          ? 'none'
          : kind === 'habit' && current.recurrence === 'none'
            ? 'daily'
            : current.recurrence,
    }))
  }

  function addFiles(event: ChangeEvent<HTMLInputElement>) {
    const selected = Array.from(event.target.files ?? [])
    event.target.value = ''
    setSubmitError(null)

    const next = [...files]

    for (const file of selected) {
      if (file.size > MAX_FILE_BYTES) {
        setSubmitError(`«${file.name}» больше 25 МБ.`)
        continue
      }

      if (next.length >= MAX_FILES) {
        setSubmitError('К записи можно приложить не больше 10 файлов.')
        break
      }

      const totalBytes = next.reduce((sum, item) => sum + item.size, 0)
      if (totalBytes + file.size > MAX_TOTAL_BYTES) {
        setSubmitError('Суммарный размер файлов превысит 100 МБ.')
        break
      }

      next.push(file)
    }

    setFiles(next)
  }

  function removeFile(index: number) {
    setFiles((current) => current.filter((_, currentIndex) => currentIndex !== index))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (locked) return

    setSubmitError(null)
    setIsSubmitting(true)

    const notes = form.notes.trim()
    let scheduleId: string | null = createdId

    try {
      if (scheduleId === null) {
        const created = await createSchedule.mutateAsync({
          title: form.title.trim(),
          date: form.date,
          start: form.untimed ? null : form.start,
          end: form.untimed ? null : form.end,
          notes: notes.length > 0 ? notes : undefined,
          recurrence: form.recurrence,
          kind: form.kind,
        })

        scheduleId = created.id
        setCreatedId(created.id)
      }

      if (files.length > 0) {
        await addAttachments.mutateAsync({ scheduleId, files })
      }

      onClose()
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Не удалось сохранить запись.'
      setSubmitError(
        scheduleId === null ? message : `Запись создана, но файлы не загружены: ${message}`,
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="Новая запись" onClose={onClose}>
      <form className="schedule-form" onSubmit={handleSubmit}>
        <fieldset className="schedule-form__fieldset" disabled={locked}>
          <label>
            Название
            <input
              value={form.title}
              onChange={(e) => update('title', e.target.value)}
              required
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
              onChange={(e) => update('untimed', e.target.checked)}
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
                form.kind === 'habit' ? HABIT_RECURRENCE_OPTIONS : EVENT_RECURRENCE_OPTIONS
              }
              onChange={(value) => update('recurrence', value)}
            />
          )}

          <label>
            Заметки
            <textarea
              value={form.notes}
              onChange={(e) => update('notes', e.target.value)}
              rows={2}
            />
          </label>

          <div className="schedule-form-files">
            <span className="schedule-form-files__label">Файлы</span>

            {files.length > 0 && (
              <ul className="attachment-list">
                {files.map((file, index) => (
                  <li className="attachment-row" key={`${file.name}-${file.size}-${index}`}>
                    <FileText
                      className="attachment-row__icon"
                      size={15}
                      strokeWidth={1.5}
                      aria-hidden="true"
                    />
                    <span className="attachment-row__name">{file.name}</span>
                    <span className="attachment-row__size">{formatFileSize(file.size)}</span>
                    <button
                      type="button"
                      className="btn btn--row attachment-row__remove"
                      onClick={() => removeFile(index)}
                      aria-label={`Убрать ${file.name}`}
                    >
                      <Trash2 size={14} strokeWidth={1.5} aria-hidden="true" />
                    </button>
                  </li>
                ))}
              </ul>
            )}

            <label className="attachment-upload">
              <Paperclip size={14} strokeWidth={1.5} aria-hidden="true" />
              <span>{files.length > 0 ? 'Добавить ещё' : 'Приложить файлы'}</span>
              <input type="file" multiple hidden onChange={addFiles} />
            </label>

            {files.length > 0 && (
              <p className="attachment-hint">Файлы загрузятся после сохранения записи.</p>
            )}
          </div>
        </fieldset>

        {mutationError && <p className="schedule-error">{mutationError}</p>}

        <div className="modal__actions">
          {createdId !== null ? (
            <button type="button" className="btn btn--primary" onClick={onClose}>
              Закрыть
            </button>
          ) : (
            <>
              <button type="button" className="btn btn--ghost" onClick={onClose} disabled={busy}>
                Отмена
              </button>
              <button type="submit" className="btn btn--primary" disabled={busy}>
                {busy ? 'Сохраняю…' : 'Сохранить'}
              </button>
            </>
          )}
        </div>
      </form>
    </Modal>
  )
}
