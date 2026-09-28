/**
 * Sale status handling for the recent-sales table (D2).
 *
 * NOTE: the OpenAPI document types SaleStatus as an integer, but the backend serializes it as a
 * string via JsonStringEnumConverter — so at runtime we receive "Paid"/"Cancelled"/"Refunded".
 * This normalizes both a string name and the numeric enum value, so the badge is correct whatever
 * the wire actually carries. The document itself should be fixed on the backend later.
 */
export type NormalizedStatus = 'Paid' | 'Cancelled' | 'Refunded' | 'Unknown'

const BY_NUMBER: Record<number, NormalizedStatus> = {
  1: 'Paid',
  2: 'Cancelled',
  3: 'Refunded',
}

export function normalizeStatus(raw: string | number): NormalizedStatus {
  if (typeof raw === 'number') return BY_NUMBER[raw] ?? 'Unknown'
  if (raw === 'Paid' || raw === 'Cancelled' || raw === 'Refunded') return raw
  return 'Unknown'
}

export const STATUS_LABELS: Record<NormalizedStatus, string> = {
  Paid: 'Оплачена',
  Cancelled: 'Отменена',
  Refunded: 'Возврат',
  Unknown: '—',
}

/** Tailwind classes for the status badge; Cancelled is grey, Refunded amber, Paid green. */
export const STATUS_BADGE_CLASS: Record<NormalizedStatus, string> = {
  Paid: 'bg-positive/15 text-positive',
  Cancelled: 'bg-secondary text-secondary-foreground',
  Refunded: 'bg-amber-100 text-amber-700',
  Unknown: 'bg-muted text-muted-foreground',
}

/** Only Paid amounts count toward totals; Cancelled/Refunded are shown grey (D2). */
export function countsTowardTotals(status: NormalizedStatus): boolean {
  return status === 'Paid'
}
