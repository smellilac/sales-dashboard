import { keepPreviousData, useQuery } from '@tanstack/react-query'

import type { Period } from '@/period/types'

import { api } from '../client'
import { toApiError } from '../error'

/** Revenue/profit time series; the server picks the bucket granularity (TIMESERIES). */
export function useTimeseries({ from, to }: Period) {
  return useQuery({
    queryKey: ['timeseries', from, to],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await api.GET('/api/dashboard/timeseries', {
        params: { query: { from, to } },
        signal,
      })
      if (error) throw toApiError(error, response)
      return data
    },
    placeholderData: keepPreviousData,
  })
}
