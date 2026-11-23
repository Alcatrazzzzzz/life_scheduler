import { describe, expect, it } from 'vitest'
import {
  adjacentAttachmentId,
  attachmentPreviewKind,
  canPreviewAttachment,
} from './attachmentPreview'

describe('attachmentPreviewKind', () => {
  it('определяет картинки по MIME независимо от расширения', () => {
    expect(attachmentPreviewKind('photo.bin', 'image/png')).toBe('image')
  })

  it('определяет картинки по расширению, если MIME пустой', () => {
    expect(attachmentPreviewKind('photo.JPEG', '')).toBe('image')
  })

  it('определяет PDF по MIME и расширению', () => {
    expect(attachmentPreviewKind('scan', 'application/pdf')).toBe('pdf')
    expect(attachmentPreviewKind('scan.PDF', 'application/octet-stream')).toBe('pdf')
  })

  it('различает docx и legacy doc', () => {
    expect(
      attachmentPreviewKind(
        'report',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      ),
    ).toBe('docx')
    expect(attachmentPreviewKind('report.docx', '')).toBe('docx')
    expect(attachmentPreviewKind('report', 'application/msword')).toBe('doc')
    expect(attachmentPreviewKind('report.DOC', '')).toBe('doc')
  })

  it('остальные файлы не превьюятся', () => {
    expect(attachmentPreviewKind('archive.zip', 'application/zip')).toBe('other')
    expect(canPreviewAttachment('archive.zip', 'application/zip')).toBe(false)
    expect(canPreviewAttachment('notes.txt', 'text/plain')).toBe(false)
  })

  it('не путает похожие расширения', () => {
    expect(attachmentPreviewKind('notes.docx.bak', '')).toBe('other')
    expect(attachmentPreviewKind('documentation', '')).toBe('other')
  })
})

describe('adjacentAttachmentId', () => {
  const files = [{ id: 'a' }, { id: 'b' }, { id: 'c' }]

  it('переключает на следующий и предыдущий файл', () => {
    expect(adjacentAttachmentId(files, 'a', 1)).toBe('b')
    expect(adjacentAttachmentId(files, 'b', -1)).toBe('a')
  })

  it('замыкает список по кругу', () => {
    expect(adjacentAttachmentId(files, 'c', 1)).toBe('a')
    expect(adjacentAttachmentId(files, 'a', -1)).toBe('c')
  })

  it('возвращает первый файл для неизвестного id и null для пустого списка', () => {
    expect(adjacentAttachmentId(files, 'missing', 1)).toBe('a')
    expect(adjacentAttachmentId([], 'a', 1)).toBeNull()
  })
})
