import type { Transition, Variants } from 'motion/react'

/**
 * Shared motion timings (T9). The dashboard should feel alive, not like a presentation:
 * short (150–400 ms), ease-out, no infinite loops. Values live here so every block uses the
 * same rhythm and `prefers-reduced-motion` is honoured centrally via MotionConfig in App.
 */

/** Standard ease-out curve for entrances and value tweens. */
export const EASE_OUT = [0.16, 1, 0.3, 1] as const

/** Fade for skeleton → content and similar swaps. */
export const FADE_DURATION = 0.15

/** Row appearance (recent sales, ranking rows entering). */
export const ROW_DURATION = 0.2

/** KPI counter tween from the previous value to the new one. */
export const COUNTER_DURATION = 0.4

/** Recharts built-in series animation (TIMESERIES). */
export const CHART_DURATION = 350

/** Stagger between KPI cards on the first load only. */
const STAGGER = 0.04

export const kpiContainer: Variants = {
  hidden: {},
  show: { transition: { staggerChildren: STAGGER } },
}

export const kpiCard: Variants = {
  hidden: { opacity: 0, y: 8 },
  show: { opacity: 1, y: 0, transition: { duration: ROW_DURATION, ease: EASE_OUT } },
}

/** Short fade used when a block swaps between skeleton / error / empty / content. */
export const fade: Variants = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { duration: FADE_DURATION, ease: EASE_OUT } },
}

/** A table row entering (new page in recent sales, first render). */
export const rowEnter: Variants = {
  hidden: { opacity: 0, y: 4 },
  show: { opacity: 1, y: 0, transition: { duration: ROW_DURATION, ease: EASE_OUT } },
}

/** Layout transition for ranking rows reordering on metric/period change. */
export const layoutTransition: Transition = { duration: ROW_DURATION, ease: EASE_OUT }
