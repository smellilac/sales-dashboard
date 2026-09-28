/** Which series the timeseries chart shows: money (revenue + profit) or Paid sales count. */
export type TimeseriesMode = 'money' | 'sales'

export const TIMESERIES_MODE_LABELS: Record<TimeseriesMode, string> = {
  money: 'Выручка и прибыль',
  sales: 'Продажи',
}
