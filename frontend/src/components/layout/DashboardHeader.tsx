import { useKpis } from '@/api/queries/useKpis'
import { PeriodPicker } from '@/components/PeriodPicker'
import { formatDayRange } from '@/lib/format'
import type { ActivePeriod, PresetId } from '@/period/types'

interface DashboardHeaderProps {
  period: ActivePeriod
  onPreset: (preset: Exclude<PresetId, 'custom'>) => void
  onCustomRange: (from: string, to: string) => void
}

/** Page header: title, period control, and the "vs previous period" caption from the KPI response. */
export function DashboardHeader({ period, onPreset, onCustomRange }: DashboardHeaderProps) {
  const { data } = useKpis(period)
  const previous = data?.previousPeriod

  return (
    <header className="flex flex-col gap-4 border-b pb-6 sm:flex-row sm:items-start sm:justify-between">
      <div className="space-y-1">
        <h1 className="text-2xl font-semibold tracking-tight">Продажи менеджеров</h1>
        <p className="text-sm text-muted-foreground">
          {previous
            ? `vs предыдущий период · ${formatDayRange(previous.from, previous.to)}`
            : 'Аналитика продаж B2B'}
        </p>
      </div>
      <PeriodPicker period={period} onPreset={onPreset} onCustomRange={onCustomRange} />
    </header>
  )
}
