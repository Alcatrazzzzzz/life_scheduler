import { useEffect, useRef, type MouseEvent, type ReactNode } from 'react'
import { X } from 'lucide-react'

interface ModalProps {
  title: string
  onClose: () => void
  className?: string
  children: ReactNode
}

export function Modal({ title, onClose, className, children }: ModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return

    if (!dialog.open) dialog.showModal()
    dialog.querySelector<HTMLElement>('input, textarea, select')?.focus()
  }, [])

  function handleClick(event: MouseEvent<HTMLDialogElement>) {
    if (event.target === dialogRef.current) onClose()
  }

  return (
    <dialog
      ref={dialogRef}
      className={className ? `modal ${className}` : 'modal'}
      onClose={onClose}
      onClick={handleClick}
    >
      <div className="modal__header">
        <h2 className="modal__title">{title}</h2>
        <button type="button" className="modal__close" onClick={onClose} aria-label="Закрыть">
          <X size={18} strokeWidth={1.5} aria-hidden="true" />
        </button>
      </div>
      <div className="modal__body">{children}</div>
    </dialog>
  )
}
