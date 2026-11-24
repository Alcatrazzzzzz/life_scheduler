interface HabitSparklineProps {
  values: Array<number | null>
  label?: string
}

const WIDTH = 180
const HEIGHT = 56
const PAD = 6

function tone(value: number): 'low' | 'mid' | 'high' {
  if (value < 0.4) return 'low'
  if (value < 0.75) return 'mid'
  return 'high'
}

export function HabitSparkline({ values, label }: HabitSparklineProps) {
  const points = values
    .map((value, index) => ({ value, index }))
    .filter((point): point is { value: number; index: number } => point.value !== null)

  if (points.length === 0) {
    return <span className="habit-sparkline habit-sparkline--empty">—</span>
  }

  const step = values.length > 1 ? (WIDTH - PAD * 2) / (values.length - 1) : 0
  const coords = points.map((point) => ({
    x: PAD + point.index * step,
    y: HEIGHT - PAD - point.value * (HEIGHT - PAD * 2),
  }))
  const path = coords
    .map((point, index) => `${index === 0 ? 'M' : 'L'}${point.x},${point.y}`)
    .join(' ')
  const last = coords[coords.length - 1]
  const lastValue = points[points.length - 1].value

  return (
    <svg
      className={`habit-sparkline habit-sparkline--${tone(lastValue)}`}
      viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
      width={WIDTH}
      height={HEIGHT}
      role="img"
      aria-label={label ?? 'Динамика выполнения по неделям'}
    >
      {coords.length > 1 && <path className="habit-sparkline__line" d={path} />}
      <circle
        className="habit-sparkline__dot"
        cx={last.x}
        cy={last.y}
        r={5}
      />
    </svg>
  )
}
