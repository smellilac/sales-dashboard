import { TZDate } from '@date-fns/tz'
import { format, startOfMonth, subDays, subMonths } from 'date-fns'

import { BUSINESS_TIME_ZONE } from '@/config'

import type { Period, PresetId } from './types'

/** The presets shown as toggles, in display order. `custom` is handled by the calendar, not here. */
export const SELECTABLE_PRESETS: readonly Exclude<PresetId, 'custom'>[] = [
  'today',
  '7d',
  '30d',
  'thisMonth',
  'lastMonth',
]

/** Russian labels for the period toggle group. */
export const PRESET_LABELS: Record<PresetId, string> = {
  today: 'Сегодня',
  '7d': '7 дней',
  '30d': '30 дней',
  thisMonth: 'Этот месяц',
  lastMonth: 'Прошлый месяц',
  custom: 'Произвольный',
}

/** Formats a business-zone date as the API's inclusive `yyyy-MM-dd` day (D4). */
export function toApiDay(date: Date): string {
  return format(date, 'yyyy-MM-dd')
}

/**
 * Resolves a preset to inclusive from/to days in the business time zone (D3, D4), not the
 * browser's zone. `now` is injectable so tests can pin the clock. "Today" is whatever day it is
 * in BUSINESS_TIME_ZONE, so at 23:30 Moscow time the presets already point at that day.
 */
export function resolvePreset(preset: Exclude<PresetId, 'custom'>, now: Date = new Date()): Period {
  const today = new TZDate(now, BUSINESS_TIME_ZONE)

  switch (preset) {
    case 'today':
      return { from: toApiDay(today), to: toApiDay(today) }
    case '7d':
      return { from: toApiDay(subDays(today, 6)), to: toApiDay(today) }
    case '30d':
      return { from: toApiDay(subDays(today, 29)), to: toApiDay(today) }
    case 'thisMonth':
      return { from: toApiDay(startOfMonth(today)), to: toApiDay(today) }
    case 'lastMonth': {
      const lastMonth = subMonths(today, 1)
      const start = startOfMonth(lastMonth)
      // End of last month = the day before the first of this month.
      const end = subDays(startOfMonth(today), 1)
      return { from: toApiDay(start), to: toApiDay(end) }
    }
  }
}
