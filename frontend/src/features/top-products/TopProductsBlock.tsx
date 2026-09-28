import { useTopProducts } from '@/api/queries/useTopProducts'
import { DashboardBlock } from '@/components/block/DashboardBlock'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { formatCount, formatMoney, formatPercent } from '@/lib/format'
import type { ActivePeriod } from '@/period/types'

/** Top 10 products by gross profit (TOP-PRODUCTS), each with a profit bar vs the leader. */
export function TopProductsBlock({
  period,
  className,
}: {
  period: ActivePeriod
  className?: string
}) {
  const { data, isPending, isError, isFetching, error, refetch } = useTopProducts(period)

  const items = data?.items ?? []
  const maxProfit = items.reduce((max, i) => Math.max(max, Number(i.grossProfit)), 0)

  return (
    <DashboardBlock
      title="Топ товаров"
      className={className}
      status={isPending ? 'pending' : isError || !data ? 'error' : 'ready'}
      isFetching={isFetching}
      isEmpty={items.length === 0}
      error={error}
      onRetry={() => void refetch()}
      skeleton={<Skeleton className="h-80 w-full" />}
      empty="Нет оплаченных продаж"
    >
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-8">#</TableHead>
            <TableHead>Товар</TableHead>
            <TableHead className="text-right">Прибыль</TableHead>
            <TableHead className="text-right">Маржа</TableHead>
            <TableHead className="text-right">Единицы</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item, index) => {
            const profit = Number(item.grossProfit)
            const width = maxProfit > 0 ? (profit / maxProfit) * 100 : 0
            return (
              <TableRow key={item.productId}>
                <TableCell className="tabular-nums text-muted-foreground">{index + 1}</TableCell>
                <TableCell>
                  <div className="space-y-1">
                    <div className="font-medium">{item.name}</div>
                    <div className="text-xs text-muted-foreground">{item.categoryName}</div>
                    <div className="h-1 w-full overflow-hidden rounded-full bg-muted">
                      <div
                        className="h-full rounded-full bg-primary"
                        style={{ width: `${width}%` }}
                      />
                    </div>
                  </div>
                </TableCell>
                <TableCell className="text-right tabular-nums">{formatMoney(item.grossProfit)}</TableCell>
                <TableCell className="text-right tabular-nums">{formatPercent(item.margin)}</TableCell>
                <TableCell className="text-right tabular-nums">{formatCount(item.unitsSold)}</TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </DashboardBlock>
  )
}
