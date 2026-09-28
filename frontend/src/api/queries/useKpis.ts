import { keepPreviousData, useQuery } from '@tanstack/react-query'

import type { Period } from '@/period/types'

import { api } from '../client'
import { toApiError } from '../error'

/** KPI cards for the period vs the previous one (API-KPI). */
export function useKpis({ from, to }: Period) {
  return useQuery({
    queryKey: ['kpis', from, to],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await api.GET('/api/dashboard/kpis', {
        params: { query: { from, to } },
        signal,
      })
      if (error) throw toApiError(error, response)
      return data
    },
    placeholderData: keepPreviousData,
  })
}
