export type AttachmentPreviewKind = 'image' | 'pdf' | 'docx' | 'doc' | 'other'

const DOCX_MIME = 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
const DOC_MIME = 'application/msword'

const IMAGE_EXTENSIONS = new Set(['avif', 'bmp', 'gif', 'jpeg', 'jpg', 'png', 'svg', 'webp'])

function fileExtension(fileName: string): string {
  const dot = fileName.lastIndexOf('.')
  return dot >= 0 ? fileName.slice(dot + 1).toLowerCase() : ''
}

export function attachmentPreviewKind(
  fileName: string,
  contentType: string,
): AttachmentPreviewKind {
  const extension = fileExtension(fileName)

  // Расширение надёжнее MIME: браузер часто присылает пустой type или octet-stream.
  if (contentType.startsWith('image/') || IMAGE_EXTENSIONS.has(extension)) return 'image'
  if (contentType === 'application/pdf' || extension === 'pdf') return 'pdf'
  if (contentType === DOCX_MIME || extension === 'docx') return 'docx'
  if (contentType === DOC_MIME || extension === 'doc') return 'doc'
  return 'other'
}

export function canPreviewAttachment(fileName: string, contentType: string): boolean {
  return attachmentPreviewKind(fileName, contentType) !== 'other'
}

/** Соседний файл по кругу; null — если список пуст. */
export function adjacentAttachmentId<T extends { id: string }>(
  attachments: T[],
  currentId: string,
  offset: number,
): string | null {
  if (attachments.length === 0) return null

  const index = attachments.findIndex((attachment) => attachment.id === currentId)
  if (index < 0) return attachments[0].id

  return attachments[(index + offset + attachments.length) % attachments.length].id
}
