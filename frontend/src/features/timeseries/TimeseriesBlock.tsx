import { useState } from 'react'

import { useTimeseries } from '@/api/queries/useTimeseries'
import { DashboardBlock } from '@/components/block/DashboardBlock'
import { Skeleton } from '@/components/ui/skeleton'
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group'
import type { Granularity } from '@/lib/format'
import type { ActivePeriod } from '@/period/types'

import { TIMESERIES_MODE_LABELS, type TimeseriesMode } from './mode'
import { TimeseriesChart, type ChartPoint } from './TimeseriesChart'

/** Sales dynamics over time (TIMESERIES): revenue/profit or Paid sales, with a mode toggle. */
export function TimeseriesBlock({
  period,
  className,
}: {
  period: ActivePeriod
  className?: string
}) {
  const [mode, setMode] = useState<TimeseriesMode>('money')
  const { data, isPending, isError, isFetching, error, refetch } = useTimeseries(period)

  const granularity = (data?.granularity ?? 'Day') as Granularity
  const points: ChartPoint[] = (data?.points ?? []).map((p) => ({
    bucketStart: p.bucketStart,
    bucketEnd: p.bucketEnd,
    revenue: Number(p.revenue),
    grossProfit: Number(p.grossProfit),
    salesCount: Number(p.salesCount),
  }))
  const allZero =
    points.length === 0 ||
    points.every((p) => p.revenue === 0 && p.grossProfit === 0 && p.salesCount === 0)

  return (
    <DashboardBlock
      title="Динамика продаж"
      className={className}
      status={isPending ? 'pending' : isError || !data ? 'error' : 'ready'}
      isFetching={isFetching}
      error={error}
      onRetry={() => void refetch()}
      skeleton={<Skeleton className="h-72 w-full" />}
      empty={null}
      headerRight={
        <ToggleGroup
          type="single"
          size="sm"
          variant="outline"
          value={mode}
          onValueChange={(v) => {
            if (v) setMode(v as TimeseriesMode)
          }}
        >
          <ToggleGroupItem value="money">{TIMESERIES_MODE_LABELS.money}</ToggleGroupItem>
          <ToggleGroupItem value="sales">{TIMESERIES_MODE_LABELS.sales}</ToggleGroupItem>
        </ToggleGroup>
      }
    >
      <div className="relative">
        <TimeseriesChart data={points} mode={mode} granularity={granularity} />
        {allZero ? (
          <div className="absolute inset-0 flex items-center justify-center">
            <p className="rounded-md bg-background/80 px-3 py-1.5 text-sm text-muted-foreground">
              Нет продаж за период
            </p>
          </div>
        ) : null}
      </div>
    </DashboardBlock>
  )
}
