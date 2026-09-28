import { keepPreviousData, useInfiniteQuery } from '@tanstack/react-query'

import type { Period } from '@/period/types'

import { api } from '../client'
import { toApiError } from '../error'

/** Page size for the recent-sales feed (RECENT-SALES); UI drives it with "Показать ещё". */
const PAGE_SIZE = 20

/** Recent sales feed with keyset pagination over the opaque cursor (RECENT-SALES). */
export function useRecentSales({ from, to }: Period) {
  return useInfiniteQuery({
    queryKey: ['recent-sales', from, to],
    queryFn: async ({ pageParam, signal }) => {
      const { data, error, response } = await api.GET('/api/dashboard/sales/recent', {
        params: {
          query: { from, to, limit: PAGE_SIZE, cursor: pageParam ?? undefined },
        },
        signal,
      })
      if (error) throw toApiError(error, response)
      return data
    },
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
    placeholderData: keepPreviousData,
  })
}
