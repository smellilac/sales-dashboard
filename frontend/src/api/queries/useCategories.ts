import { keepPreviousData, useQuery } from '@tanstack/react-query'

import type { Period } from '@/period/types'

import { api } from '../client'
import { toApiError } from '../error'

/** Per-category revenue, profit, margin, units and revenue share (CAT-COUNT). */
export function useCategories({ from, to }: Period) {
  return useQuery({
    queryKey: ['categories', from, to],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await api.GET('/api/dashboard/categories', {
        params: { query: { from, to } },
        signal,
      })
      if (error) throw toApiError(error, response)
      return data
    },
    placeholderData: keepPreviousData,
  })
}
