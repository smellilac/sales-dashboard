import { useKpis } from '@/api/queries/useKpis'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  formatChange,
  formatCount,
  formatMoney,
  formatPercent,
  formatPoints,
} from '@/lib/format'
import type { ActivePeriod } from '@/period/types'

/**
 * KPI row (API-KPI). Real values wired from the single kpis query — this is the T7 end-to-end
 * check. Every tile is Paid-only (D2); Возвраты is the separate Refunded view (D1). Richer
 * treatment (trends, colours) lands in T8.
 */
export function KpiCards({ period }: { period: ActivePeriod }) {
  const { data, isPending, isError } = useKpis(period)

  return (
    <div className="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-6">
      <StatTile
        title="Выручка"
        value={data && formatMoney(data.revenue.current)}
        sub={data && formatChange(data.revenue.change)}
        loading={isPending}
        error={isError}
      />
      <StatTile
        title="Валовая прибыль"
        value={data && formatMoney(data.grossProfit.current)}
        sub={data && formatChange(data.grossProfit.change)}
        loading={isPending}
        error={isError}
      />
      <StatTile
        title="Продажи"
        value={data && formatCount(data.salesCount.current)}
        sub={data && formatChange(data.salesCount.change)}
        loading={isPending}
        error={isError}
      />
      <StatTile
        title="Средний чек"
        value={data && formatMoney(data.averageCheck.current)}
        sub={data && formatChange(data.averageCheck.change)}
        loading={isPending}
        error={isError}
      />
      <StatTile
        title="Маржа"
        value={data && formatPercent(data.margin.current)}
        sub={data && formatPoints(data.margin.changePoints)}
        loading={isPending}
        error={isError}
      />
      <StatTile
        title="Возвраты"
        value={data && formatMoney(data.refunds.amount.current)}
        sub={data && `доля ${formatPercent(data.refunds.rate.current)}`}
        loading={isPending}
        error={isError}
      />
    </div>
  )
}

interface StatTileProps {
  title: string
  value: string | null | undefined
  sub: string | null | undefined
  loading: boolean
  error: boolean
}

function StatTile({ title, value, sub, loading, error }: StatTileProps) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
      </CardHeader>
      <CardContent>
        {loading ? (
          <div className="space-y-2">
            <Skeleton className="h-7 w-24" />
            <Skeleton className="h-4 w-16" />
          </div>
        ) : error ? (
          <p className="text-sm text-destructive">Не удалось загрузить</p>
        ) : (
          <>
            <p className="text-2xl font-semibold tabular-nums">{value}</p>
            <p className="text-xs text-muted-foreground">{sub}</p>
          </>
        )}
      </CardContent>
    </Card>
  )
}
