import { Flame } from 'lucide-react'

interface StreakBadgeProps {
  value: number
}

export function StreakBadge({ value }: StreakBadgeProps) {
  if (value <= 0) return null

  return (
    <span className="streak-badge" aria-label={`Серия ${value} дней`}>
      <Flame size={12} strokeWidth={1.5} aria-hidden="true" />
      {value}
    </span>
  )
}
