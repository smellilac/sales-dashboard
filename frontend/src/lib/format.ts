import { TZDate } from '@date-fns/tz'
import { format } from 'date-fns'
import { ru } from 'date-fns/locale'

import { BUSINESS_TIME_ZONE } from '@/config'

/** Placeholder for a value that does not exist, per D6 (no revenue, no sales, no comparison). */
export const EM_DASH = '—'

/**
 * API numbers. The OpenAPI document types money/counts as `number | string` (large decimals may
 * arrive as strings), so every formatter coerces before rendering.
 */
export type ApiNumber = number | string | null | undefined

function toNumber(value: ApiNumber): number | null {
  if (value == null) return null
  const n = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(n) ? n : null
}

const money = new Intl.NumberFormat('ru-RU', {
  style: 'currency',
  currency: 'RUB',
  maximumFractionDigits: 0,
})

const percent = new Intl.NumberFormat('ru-RU', {
  style: 'percent',
  maximumFractionDigits: 1,
})

const signedPercent = new Intl.NumberFormat('ru-RU', {
  style: 'percent',
  maximumFractionDigits: 1,
  signDisplay: 'exceptZero',
})

const points = new Intl.NumberFormat('ru-RU', {
  maximumFractionDigits: 1,
  signDisplay: 'exceptZero',
})

/** Rubles, no kopecks — rounding happens only on screen (D5). */
export function formatMoney(value: ApiNumber): string {
  const n = toNumber(value)
  return n == null ? EM_DASH : money.format(n)
}

/** A fraction (0.22) as a percent; `—` when null (D6). */
export function formatPercent(value: ApiNumber): string {
  const n = toNumber(value)
  return n == null ? EM_DASH : percent.format(n)
}

/** A relative change (0.12 → +12 %) with an explicit sign; `нет данных` when null (D6). */
export function formatChange(value: ApiNumber): string {
  const n = toNumber(value)
  return n == null ? 'нет данных для сравнения' : signedPercent.format(n)
}

/** A margin change in percentage points (0.02 → +2,0 п.п.); `нет данных` when null (D6, API-KPI). */
export function formatPoints(value: ApiNumber): string {
  const n = toNumber(value)
  return n == null ? 'нет данных для сравнения' : `${points.format(n * 100)} п.п.`
}

/** Whole units count in the ru locale. */
export function formatCount(value: ApiNumber): string {
  const n = toNumber(value)
  return n == null ? EM_DASH : new Intl.NumberFormat('ru-RU').format(n)
}

/** An inclusive API day (`yyyy-MM-dd`) as a short business-zone date, e.g. «5 сент.». */
export function formatDay(day: string): string {
  return format(new TZDate(`${day}T00:00:00`, BUSINESS_TIME_ZONE), 'd MMM', { locale: ru })
}

/** A period's inclusive bounds as «from – to» in the business zone. */
export function formatDayRange(from: string, to: string): string {
  return `${formatDay(from)} – ${formatDay(to)}`
}
