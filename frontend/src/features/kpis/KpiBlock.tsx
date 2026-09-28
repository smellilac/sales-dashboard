import { RotateCw } from 'lucide-react'

import { useKpis } from '@/api/queries/useKpis'
import { ChangeIndicator } from '@/components/ChangeIndicator'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { formatCount, formatMoney, formatPercent } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { ActivePeriod } from '@/period/types'

import { BestManagerTile } from './BestManagerTile'
import { KpiTile } from './KpiTile'

const ROW = 'grid grid-cols-2 gap-3 md:grid-cols-4 xl:grid-cols-7'

/**
 * KPI row (API-KPI): 7 compact cards from the single kpis query. Every value is Paid-only (D2);
 * Возвраты is the separate Refunded view (D1) with inverted change colours (growth is bad).
 * States (skeleton / period-change dim / error) are handled at the row level since this is a row
 * of cards, not a single titled block.
 */
export function KpiBlock({ period }: { period: ActivePeriod }) {
  const { data, isPending, isError, isFetching, error, refetch } = useKpis(period)

  if (isPending) {
    return (
      <div className={ROW}>
        {Array.from({ length: 7 }).map((_, i) => (
          <Card key={i} className="gap-2 py-4">
            <CardHeader className="px-4">
              <Skeleton className="h-3 w-20" />
            </CardHeader>
            <CardContent className="space-y-2 px-4">
              <Skeleton className="h-6 w-24" />
              <Skeleton className="h-3 w-16" />
            </CardContent>
          </Card>
        ))}
      </div>
    )
  }

  if (isError || !data) {
    return (
      <Card className="py-4">
        <CardContent className="flex flex-col items-center justify-center gap-3 py-6 text-center">
          <p className="text-sm text-destructive">{error?.title ?? 'Не удалось загрузить KPI'}</p>
          <Button variant="outline" size="sm" onClick={() => void refetch()}>
            <RotateCw className="size-4" />
            Повторить
          </Button>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className={cn(ROW, 'transition-opacity', isFetching && 'pointer-events-none opacity-60')}>
      <KpiTile
        title="Выручка"
        value={formatMoney(data.revenue.current)}
        footer={<ChangeIndicator value={data.revenue.change} />}
      />
      <KpiTile
        title="Валовая прибыль"
        value={formatMoney(data.grossProfit.current)}
        footer={<ChangeIndicator value={data.grossProfit.change} />}
      />
      <KpiTile
        title="Маржа"
        value={formatPercent(data.margin.current)}
        footer={<ChangeIndicator value={data.margin.changePoints} kind="points" />}
      />
      <KpiTile
        title="Продажи"
        value={formatCount(data.salesCount.current)}
        footer={<ChangeIndicator value={data.salesCount.change} />}
      />
      <KpiTile
        title="Средний чек"
        value={formatMoney(data.averageCheck.current)}
        footer={<ChangeIndicator value={data.averageCheck.change} />}
      />
      <KpiTile
        title="Возвраты"
        value={formatMoney(data.refunds.amount.current)}
        footer={
          <div className="space-y-0.5">
            <p className="text-muted-foreground">доля {formatPercent(data.refunds.rate.current)}</p>
            <ChangeIndicator value={data.refunds.rate.changePoints} kind="points" invert />
          </div>
        }
      />
      <BestManagerTile best={data.bestManager} />
    </div>
  )
}
