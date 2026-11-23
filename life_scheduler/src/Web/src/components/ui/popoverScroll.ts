/** Прокручивает выбранный пункт к середине колонки при открытии списка. */
export function centerSelected(container: HTMLElement | null) {
  if (!container) return

  const selected = container.querySelector<HTMLElement>('[aria-selected="true"]')
  if (!selected) return

  container.scrollTop =
    selected.offsetTop - container.clientHeight / 2 + selected.clientHeight / 2
}
