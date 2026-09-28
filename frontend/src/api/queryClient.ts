import { QueryClient } from '@tanstack/react-query'

/**
 * Query client tuned for a read-only dashboard over immutable seed data (READ-ONLY):
 * data is stable for 5 minutes, keep the previous period's data visible while refetching,
 * and retry once (the backend either answers or returns ProblemDetails).
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})
