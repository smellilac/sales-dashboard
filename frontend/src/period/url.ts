import { MAX_RANGE_DAYS } from '@/config'

import { resolvePreset, SELECTABLE_PRESETS } from './presets'
import { DEFAULT_PRESET, type ActivePeriod, type PresetId } from './types'

const DAY_PATTERN = /^\d{4}-\d{2}-\d{2}$/

function isSelectablePreset(value: string | null): value is Exclude<PresetId, 'custom'> {
  return value !== null && (SELECTABLE_PRESETS as readonly string[]).includes(value)
}

/** True when `value` is a real calendar day in `yyyy-MM-dd` form (rejects e.g. 2026-02-31). */
function isValidDay(value: string | null): value is string {
  if (value === null || !DAY_PATTERN.test(value)) return false
  const date = new Date(`${value}T00:00:00Z`)
  return !Number.isNaN(date.getTime()) && value === date.toISOString().slice(0, 10)
}

/** Inclusive day count between two `yyyy-MM-dd` days. */
function inclusiveDays(from: string, to: string): number {
  const ms = Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)
  return Math.round(ms / 86_400_000) + 1
}

/** The default period: the fallback preset resolved against `now`. */
export function defaultPeriod(now: Date = new Date()): ActivePeriod {
  return { preset: DEFAULT_PRESET, ...resolvePreset(DEFAULT_PRESET, now) }
}

/**
 * Reads the active period from a URL query string (D3). `?period=<preset>` resolves live against
 * `now`; `?from&to` is an explicit custom range. Anything invalid — unknown preset, malformed or
 * out-of-order dates, a range longer than the limit — falls back to the default preset. The server
 * remains the source of truth for validation.
 */
export function parsePeriod(search: string, now: Date = new Date()): ActivePeriod {
  const params = new URLSearchParams(search)
  const preset = params.get('period')
  if (isSelectablePreset(preset)) {
    return { preset, ...resolvePreset(preset, now) }
  }

  const from = params.get('from')
  const to = params.get('to')
  if (isValidDay(from) && isValidDay(to)) {
    const days = inclusiveDays(from, to)
    if (days >= 1 && days <= MAX_RANGE_DAYS) {
      return { preset: 'custom', from, to }
    }
  }

  return defaultPeriod(now)
}

/** Serializes the active period into a query string for the address bar. */
export function serializePeriod(period: ActivePeriod): string {
  const params = new URLSearchParams()
  if (period.preset === 'custom') {
    params.set('from', period.from)
    params.set('to', period.to)
  } else {
    params.set('period', period.preset)
  }
  return `?${params.toString()}`
}
