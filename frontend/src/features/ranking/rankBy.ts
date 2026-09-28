import type { RankBy } from '@/api/queries/useRanking'

/** Ranking metric as it appears in the URL (`?rankBy=`), kept camelCase for clean links. */
export type RankByUrl = 'grossProfit' | 'averageCheck'

/** Default metric (D7): gross profit. Omitted from the URL when active. */
export const DEFAULT_RANK_BY: RankByUrl = 'grossProfit'

export const RANK_BY_LABELS: Record<RankByUrl, string> = {
  grossProfit: 'Валовая прибыль',
  averageCheck: 'Средний чек',
}

/** Maps the URL value to the backend RankingMetric name the API expects. */
export function toApiRankBy(value: RankByUrl): RankBy {
  return value === 'averageCheck' ? 'AverageCheck' : 'GrossProfit'
}

/** Reads a valid RankByUrl from a raw query value, falling back to the default. */
export function parseRankBy(value: string | null): RankByUrl {
  return value === 'averageCheck' ? 'averageCheck' : 'grossProfit'
}
