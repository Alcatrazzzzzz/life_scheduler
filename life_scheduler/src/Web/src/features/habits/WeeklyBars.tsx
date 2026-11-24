import { rate, type WeekBucket } from './habitStats'

interface WeeklyBarsProps {
  buckets: WeekBucket[]
}

const BAR_WIDTH = 24
const SLOT = 70
const TOP = 14
const BASE = 150
const PLOT_HEIGHT = BASE - TOP
const HEIGHT = 184
const RADIUS = 4

function shortDate(value: string): string {
  const [, month, day] = value.split('-')
  return `${day}.${month}`
}

function tone(value: number): 'low' | 'mid' | 'high' {
  if (value < 0.4) return 'low'
  if (value < 0.75) return 'mid'
  return 'high'
}

function barPath(x: number, height: number): string {
  const top = BASE - height
  const r = Math.min(RADIUS, height)

  return [
    `M${x},${BASE}`,
    `L${x},${top + r}`,
    `Q${x},${top} ${x + r},${top}`,
    `L${x + BAR_WIDTH - r},${top}`,
    `Q${x + BAR_WIDTH},${top} ${x + BAR_WIDTH},${top + r}`,
    `L${x + BAR_WIDTH},${BASE}`,
    'Z',
  ].join(' ')
}

export function WeeklyBars({ buckets }: WeeklyBarsProps) {
  const width = Math.max(buckets.length * SLOT, SLOT)
  const lastFilled = buckets.reduce(
    (last, bucket, index) => (rate(bucket) !== null ? index : last),
    -1,
  )

  return (
    <svg
      className="habit-bars"
      viewBox={`0 0 ${width} ${HEIGHT}`}
      width="100%"
      height={HEIGHT}
      role="img"
      aria-label="Доля выполненных привычек по неделям"
    >
      {buckets.map((bucket, index) => {
        const value = rate(bucket)
        const x = index * SLOT + (SLOT - BAR_WIDTH) / 2
        const height = value === null ? 0 : value * PLOT_HEIGHT

        return (
          <g key={bucket.start}>
            <title>
              {`Неделя ${shortDate(bucket.start)}: ${bucket.completed} из ${bucket.scheduled}`}
            </title>
            <rect
              className="habit-bars__track"
              x={x}
              y={TOP}
              width={BAR_WIDTH}
              height={PLOT_HEIGHT}
              rx={RADIUS}
            />
            {height > 0 && value !== null && (
              <path
                className={`habit-bars__fill habit-bars__fill--${tone(value)}`}
                d={barPath(x, height)}
              />
            )}
            <text
              className="habit-bars__label"
              x={x + BAR_WIDTH / 2}
              y={HEIGHT - 2}
              textAnchor="middle"
            >
              {shortDate(bucket.start)}
            </text>
            {index === lastFilled && value !== null && (
              <text
                className="habit-bars__value"
                x={x + BAR_WIDTH / 2}
                y={BASE - height - 4}
                textAnchor="middle"
              >
                {Math.round(value * 100)}%
              </text>
            )}
          </g>
        )
      })}
    </svg>
  )
}
