import { useEffect } from 'react'
import { animate, motion, useMotionValue, useReducedMotion, useTransform } from 'motion/react'

import type { ApiNumber } from '@/lib/format'
import { COUNTER_DURATION, EASE_OUT } from '@/lib/motion'

interface AnimatedNumberProps {
  /** Raw API value; `null`/`undefined` renders «—» with no animation (D6). */
  value: ApiNumber
  /** Formatter from lib/format applied on every frame (so «₽», «%» etc. stay correct). */
  format: (value: ApiNumber) => string
}

function toNumber(value: ApiNumber): number | null {
  if (value == null) return null
  const n = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(n) ? n : null
}

/**
 * A KPI counter that tweens from the previously shown value to the new one — not from zero on
 * every period change (T9). The motion value persists across renders, so each change animates from
 * where it left off; the formatter runs per frame via useTransform (no per-frame React re-render).
 * `null` shows «—» immediately, and `prefers-reduced-motion` (or skipped animations in tests) snaps
 * to the final value without a tween.
 */
export function AnimatedNumber({ value, format }: AnimatedNumberProps) {
  const target = toNumber(value)
  const reduce = useReducedMotion()
  const motionValue = useMotionValue(target ?? 0)
  // When the metric is null we format the raw value (yielding «—»); otherwise format the live frame.
  const text = useTransform(motionValue, (latest) => (target == null ? format(value) : format(latest)))

  useEffect(() => {
    if (target == null) return
    if (reduce) {
      motionValue.set(target)
      return
    }
    const controls = animate(motionValue, target, {
      duration: COUNTER_DURATION,
      ease: EASE_OUT,
    })
    return () => controls.stop()
  }, [target, reduce, motionValue])

  return <motion.span className="tabular-nums">{text}</motion.span>
}
