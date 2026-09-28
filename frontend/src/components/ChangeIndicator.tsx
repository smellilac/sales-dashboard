import { ArrowDown, ArrowUp, Minus } from 'lucide-react'

import { formatChange, formatPoints, type ApiNumber } from '@/lib/format'
import { cn } from '@/lib/utils'

interface ChangeIndicatorProps {
  /** Relative change (0.12 = +12%) or a margin change in points (0.02 = +2 п.п.). */
  value: ApiNumber
  /** `relative` formats as a percent, `points` as percentage points (API-KPI). */
  kind?: 'relative' | 'points'
  /** When true, growth is bad (refunds): the colour is inverted, the arrow direction is not. */
  invert?: boolean
  className?: string
}

function toNumber(value: ApiNumber): number | null {
  if (value == null) return null
  const n = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(n) ? n : null
}

/**
 * A period-over-period change: an arrow plus the signed value, coloured green/red. Colour is
 * always mirrored by the arrow direction (D14). `null` renders the D6 "no comparison" text with
 * no arrow or colour.
 */
export function ChangeIndicator({
  value,
  kind = 'relative',
  invert = false,
  className,
}: ChangeIndicatorProps) {
  const n = toNumber(value)
  const text = kind === 'points' ? formatPoints(value) : formatChange(value)

  if (n == null || n === 0) {
    return (
      <span className={cn('inline-flex items-center gap-1 text-muted-foreground', className)}>
        {n === 0 ? <Minus className="size-3" /> : null}
        <span className="tabular-nums">{text}</span>
      </span>
    )
  }

  const isGood = invert ? n < 0 : n > 0
  const Arrow = n > 0 ? ArrowUp : ArrowDown

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 tabular-nums',
        isGood ? 'text-positive' : 'text-negative',
        className,
      )}
    >
      <Arrow className="size-3" />
      {text}
    </span>
  )
}
