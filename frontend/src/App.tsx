import { QueryClientProvider } from '@tanstack/react-query'

import { queryClient } from '@/api/queryClient'
import { DashboardGrid } from '@/components/layout/DashboardGrid'
import { DashboardHeader } from '@/components/layout/DashboardHeader'
import { TooltipProvider } from '@/components/ui/tooltip'
import { usePeriod } from '@/period/usePeriod'

function Dashboard() {
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
      <TooltipProvider>
        <Dashboard />
      </TooltipProvider>
    </QueryClientProvider>
  )
}
