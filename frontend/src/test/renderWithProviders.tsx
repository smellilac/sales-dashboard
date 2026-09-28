import type { ReactElement, ReactNode } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { MotionConfig } from 'motion/react'

import { TooltipProvider } from '@/components/ui/tooltip'

/**
 * Renders a component tree with the same providers as the app, but a fresh QueryClient per test
 * (no retries, no cross-test cache bleed) so each test starts from a clean query state (T9).
 */
export function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <MotionConfig reducedMotion="user">
          <TooltipProvider>{children}</TooltipProvider>
        </MotionConfig>
      </QueryClientProvider>
    )
  }

  return { ...render(ui, { wrapper: Wrapper }), queryClient }
}
