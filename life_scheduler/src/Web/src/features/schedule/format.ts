const KILOBYTE = 1024

export function formatFileSize(bytes: number): string {
  if (bytes < KILOBYTE) return `${bytes} Б`
  if (bytes < KILOBYTE * KILOBYTE) return `${Math.round(bytes / KILOBYTE)} КБ`
  return `${(bytes / (KILOBYTE * KILOBYTE)).toFixed(1)} МБ`
}
