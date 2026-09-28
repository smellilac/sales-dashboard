import { Loader2 } from 'lucide-react'

import { useRecentSales } from '@/api/queries/useRecentSales'
import type { components } from '@/api/schema'
import { DashboardBlock } from '@/components/block/DashboardBlock'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { formatDateTime, formatMoney } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { ActivePeriod } from '@/period/types'

import {
  countsTowardTotals,
  normalizeStatus,
  STATUS_BADGE_CLASS,
  STATUS_LABELS,
} from './status'

type RecentSaleRow = components['schemas']['RecentSaleRow']

/** Recent sales feed (RECENT-SALES, D2): all statuses, keyset pagination via "Показать ещё". */
export function RecentSalesBlock({
  period,
  className,
}: {
  period: ActivePeriod
  className?: string
}) {
  const {
    data,
    isPending,
    isError,
    isFetching,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
    error,
    refetch,
  } = useRecentSales(period)

  const rows = data?.pages.flatMap((page) => page.items) ?? []
  // Only the initial load should dim on refetch — not the "load more" fetch.
  const backgroundFetching = isFetching && !isFetchingNextPage

  return (
    <DashboardBlock
      title="Последние продажи"
      className={className}
      status={isPending ? 'pending' : isError || !data ? 'error' : 'ready'}
      isFetching={backgroundFetching}
      isEmpty={rows.length === 0}
      error={error}
      onRetry={() => void refetch()}
      skeleton={<Skeleton className="h-96 w-full" />}
      empty="Нет сделок за период"
    >
      <div className="space-y-3">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Дата</TableHead>
              <TableHead>Менеджер</TableHead>
              <TableHead>Клиент</TableHead>
              <TableHead>Товары</TableHead>
              <TableHead>Статус</TableHead>
              <TableHead className="text-right">Сумма</TableHead>
              <TableHead className="text-right">Прибыль</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.map((row) => (
              <SaleRow key={row.saleId} row={row} />
            ))}
          </TableBody>
        </Table>

        {hasNextPage ? (
          <div className="flex justify-center">
            <Button
              variant="outline"
              size="sm"
              onClick={() => void fetchNextPage()}
              disabled={isFetchingNextPage}
            >
              {isFetchingNextPage ? <Loader2 className="size-4 animate-spin" /> : null}
              Показать ещё
            </Button>
          </div>
        ) : null}
      </div>
    </DashboardBlock>
  )
}

function SaleRow({ row }: { row: RecentSaleRow }) {
  const status = normalizeStatus(row.status)
  const greyAmount = !countsTowardTotals(status)

  return (
    <TableRow>
      <TableCell className="tabular-nums">{formatDateTime(row.soldAt)}</TableCell>
      <TableCell>{row.managerName}</TableCell>
      <TableCell>{row.customerCompany}</TableCell>
      <TableCell>
        <SaleItems items={row.items} />
      </TableCell>
      <TableCell>
        <Badge className={STATUS_BADGE_CLASS[status]}>{STATUS_LABELS[status]}</Badge>
      </TableCell>
      <TableCell className={cn('text-right tabular-nums', greyAmount && 'text-muted-foreground')}>
        {formatMoney(row.amount)}
      </TableCell>
      <TableCell className={cn('text-right tabular-nums', greyAmount && 'text-muted-foreground')}>
        {formatMoney(row.grossProfit)}
      </TableCell>
    </TableRow>
  )
}

function SaleItems({ items }: { items: RecentSaleRow['items'] }) {
  if (items.length === 0) return <span className="text-muted-foreground">—</span>

  const first = items[0]
  const rest = items.length - 1
  const summary =
    rest > 0
      ? `${first.productName} ×${first.quantity} и ещё ${rest}`
      : `${first.productName} ×${first.quantity}`

  if (rest === 0) return <span>{summary}</span>

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <span className="cursor-help underline decoration-dotted underline-offset-2">{summary}</span>
      </TooltipTrigger>
      <TooltipContent>
        <ul className="space-y-0.5">
          {items.map((item, i) => (
            <li key={i}>
              {item.productName} ×{item.quantity}
            </li>
          ))}
        </ul>
      </TooltipContent>
    </Tooltip>
  )
}
