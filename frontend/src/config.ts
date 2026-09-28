/**
 * The business time zone the dashboard reports in (D4). Period presets and calendar dates are
 * computed in this zone, never in the browser's zone, so a manager in another time zone sees the
 * same "today" as the server. Overridable via VITE_BUSINESS_TIME_ZONE; defaults to Europe/Moscow.
 */
export const BUSINESS_TIME_ZONE =
  import.meta.env.VITE_BUSINESS_TIME_ZONE ?? 'Europe/Moscow'

/** Maximum custom range length the server accepts (D4: no more than 2 years). */
export const MAX_RANGE_DAYS = 731
