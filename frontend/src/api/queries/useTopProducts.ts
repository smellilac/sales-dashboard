import { keepPreviousData, useQuery } from '@tanstack/react-query'

import type { Period } from '@/period/types'

import { api } from '../client'
import { toApiError } from '../error'

/** Top 10 products by gross profit (TOP-PRODUCTS). */
export function useTopProducts({ from, to }: Period) {
  return useQuery({
    queryKey: ['top-products', from, to],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await api.GET('/api/dashboard/products/top', {
        params: { query: { from, to } },
        signal,
      })
      if (error) throw toApiError(error, response)
      return data
    },
    placeholderData: keepPreviousData,
  })
}
