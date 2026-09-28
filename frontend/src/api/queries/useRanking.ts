import { keepPreviousData, useQuery } from '@tanstack/react-query'

import type { Period } from '@/period/types'

import { api } from '../client'
import { toApiError } from '../error'

/** Metric the ranking is ordered by (D7, RANKING-CHANGE). Mirrors the backend RankingMetric enum. */
export type RankBy = 'GrossProfit' | 'AverageCheck'

/** Manager ranking for the period, ordered by `rankBy` (default gross profit). */
export function useRanking({ from, to }: Period, rankBy: RankBy = 'GrossProfit') {
  return useQuery({
    queryKey: ['ranking', from, to, rankBy],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await api.GET('/api/dashboard/managers/ranking', {
        params: { query: { from, to, rankBy } },
        signal,
      })
      if (error) throw toApiError(error, response)
      return data
    },
    placeholderData: keepPreviousData,
  })
}
