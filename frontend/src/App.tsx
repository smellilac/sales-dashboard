import { QueryClientProvider } from '@tanstack/react-query'
import { MotionConfig } from 'motion/react'

import { queryClient } from '@/api/queryClient'
import { DashboardGrid } from '@/components/layout/DashboardGrid'
import { DashboardHeader } from '@/components/layout/DashboardHeader'
import { TooltipProvider } from '@/components/ui/tooltip'
import { usePeriod } from '@/period/usePeriod'

/** The dashboard itself, without the app-level providers — exported so tests can wrap it in their
 * own QueryClient (the default App below uses the shared singleton). */
export function Dashboard() {
  const { period, setPreset, setCustomRange } = usePeriod()

  return (
    <div className="mx-auto max-w-[1440px] space-y-6 p-6">
      <DashboardHeader
        period={period}
        onPreset={setPreset}
        onCustomRange={setCustomRange}
      />
      <DashboardGrid period={period} />
    </div>
  )
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <MotionConfig reducedMotion="user">
        <TooltipProvider>
          <Dashboard />
        </TooltipProvider>
      </MotionConfig>
    </QueryClientProvider>
  )
}
