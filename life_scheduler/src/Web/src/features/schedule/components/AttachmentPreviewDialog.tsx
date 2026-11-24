import { useEffect, useMemo, useRef, useState } from 'react'
import { ChevronLeft, ChevronRight, Download, FileImage, FileText } from 'lucide-react'
import { attachmentDownloadUrl } from '../api'
import type { ScheduleAttachment } from '../api'
import { adjacentAttachmentId, attachmentPreviewKind } from '../attachmentPreview'
import { formatFileSize } from '../format'
import { useAttachmentBlob } from '../queries'
import { Modal } from '../../../components/ui/Modal'

interface AttachmentPreviewDialogProps {
  scheduleId: string
  attachments: ScheduleAttachment[]
  initialAttachmentId: string
  onClose: () => void
}

export function AttachmentPreviewDialog({
  scheduleId,
  attachments,
  initialAttachmentId,
  onClose,
}: AttachmentPreviewDialogProps) {
  const [activeId, setActiveId] = useState(initialAttachmentId)
  const activeIndex = attachments.findIndex((attachment) => attachment.id === activeId)
  const attachment = attachments[activeIndex] ?? attachments[0]
  const displayIndex = activeIndex < 0 ? 0 : activeIndex
  const hasMany = attachments.length > 1

  function select(offset: number) {
    const nextId = adjacentAttachmentId(attachments, attachment.id, offset)
    if (nextId) setActiveId(nextId)
  }

  useEffect(() => {
    if (!hasMany) return

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return

      event.preventDefault()
      setActiveId((currentId) =>
        adjacentAttachmentId(attachments, currentId, event.key === 'ArrowRight' ? 1 : -1) ??
        currentId,
      )
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [attachments, hasMany])

  // Список пришёл пустым — показывать нечего.
  if (!attachment) return null

  const downloadUrl = attachmentDownloadUrl(scheduleId, attachment.id)

  return (
    <Modal title={attachment.fileName} onClose={onClose} className="modal--preview">
      <div className="attachment-preview">
        {hasMany && (
          <div className="attachment-preview__tabs" role="group" aria-label="Файлы записи">
            {attachments.map((item) => {
              const active = item.id === attachment.id
              const Icon =
                attachmentPreviewKind(item.fileName, item.contentType) === 'image'
                  ? FileImage
                  : FileText

              return (
                <button
                  key={item.id}
                  type="button"
                  className={`attachment-preview__tab${active ? ' is-active' : ''}`}
                  onClick={() => setActiveId(item.id)}
                  aria-current={active ? 'true' : undefined}
                  title={item.fileName}
                >
                  <Icon size={14} strokeWidth={1.5} aria-hidden="true" />
                  <span className="attachment-preview__tab-name">{item.fileName}</span>
                </button>
              )
            })}
          </div>
        )}

        <div className="attachment-preview__bar">
          <span className="attachment-preview__meta">
            {hasMany && `${displayIndex + 1} / ${attachments.length} · `}
            {formatFileSize(attachment.sizeBytes)}
            {attachment.contentType && ` · ${attachment.contentType}`}
          </span>

          <div className="attachment-preview__actions">
            {hasMany && (
              <>
                <button
                  type="button"
                  className="btn btn--row"
                  onClick={() => select(-1)}
                  aria-label="Предыдущий файл"
                >
                  <ChevronLeft size={16} strokeWidth={1.5} aria-hidden="true" />
                </button>
                <button
                  type="button"
                  className="btn btn--row"
                  onClick={() => select(1)}
                  aria-label="Следующий файл"
                >
                  <ChevronRight size={16} strokeWidth={1.5} aria-hidden="true" />
                </button>
              </>
            )}

            <a className="btn btn--ghost" href={downloadUrl}>
              <Download size={14} strokeWidth={1.5} aria-hidden="true" />
              Скачать
            </a>
          </div>
        </div>

        <AttachmentPreviewBody
          key={attachment.id}
          scheduleId={scheduleId}
          attachment={attachment}
        />
      </div>
    </Modal>
  )
}

interface AttachmentPreviewBodyProps {
  scheduleId: string
  attachment: ScheduleAttachment
}

function AttachmentPreviewBody({ scheduleId, attachment }: AttachmentPreviewBodyProps) {
  const kind = attachmentPreviewKind(attachment.fileName, attachment.contentType)
  const needsBlob = kind === 'image' || kind === 'pdf' || kind === 'docx'
  const file = useAttachmentBlob(scheduleId, attachment.id, needsBlob)
  const [renderError, setRenderError] = useState<string | null>(null)
  const docxHostRef = useRef<HTMLDivElement>(null)
  const downloadUrl = attachmentDownloadUrl(scheduleId, attachment.id)

  const objectUrl = useMemo(() => {
    if (!file.data || (kind !== 'image' && kind !== 'pdf')) return null
    return URL.createObjectURL(file.data)
  }, [file.data, kind])

  useEffect(() => {
    if (!objectUrl) return
    return () => URL.revokeObjectURL(objectUrl)
  }, [objectUrl])

  useEffect(() => {
    const host = docxHostRef.current
    if (kind !== 'docx' || !file.data || !host) return

    let cancelled = false
    host.replaceChildren()

    void import('docx-preview')
      .then(({ renderAsync }) => {
        if (cancelled) return
        return renderAsync(file.data, host, undefined, {
          className: 'docx',
          inWrapper: true,
          breakPages: true,
        })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        setRenderError(error instanceof Error ? error.message : 'Не удалось показать документ.')
      })

    return () => {
      cancelled = true
      host.replaceChildren()
    }
  }, [kind, file.data])

  if (!needsBlob) {
    return (
      <div className="attachment-preview__fallback">
        <FileText size={28} strokeWidth={1.25} aria-hidden="true" />
        <p className="attachment-preview__fallback-text">
          {kind === 'doc'
            ? 'Файлы .doc в браузере не открываются. Скачайте файл, чтобы посмотреть его в Word.'
            : 'Для этого типа файла предпросмотр не поддерживается.'}
        </p>
        <a className="btn btn--ghost" href={downloadUrl}>
          <Download size={14} strokeWidth={1.5} aria-hidden="true" />
          Скачать
        </a>
      </div>
    )
  }

  if (file.isPending) {
    return <p className="attachment-preview__status">Загружаю файл…</p>
  }

  if (file.isError) {
    return <p className="schedule-error">{file.error.message}</p>
  }

  if (kind === 'image') {
    return objectUrl ? (
      <div className="attachment-preview__image-wrap">
        <img
          className="attachment-preview__image"
          src={objectUrl}
          alt={attachment.fileName}
        />
      </div>
    ) : null
  }

  if (kind === 'pdf') {
    return objectUrl ? (
      <iframe
        className="attachment-preview__frame"
        src={objectUrl}
        title={attachment.fileName}
      />
    ) : null
  }

  return (
    <>
      {renderError && <p className="schedule-error">{renderError}</p>}
      <div className="attachment-preview__docx" ref={docxHostRef} />
    </>
  )
}
