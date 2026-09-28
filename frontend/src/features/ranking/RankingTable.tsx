import { motion } from 'motion/react'

import type { components } from '@/api/schema'
import { Avatar } from '@/components/Avatar'
import { ChangeIndicator } from '@/components/ChangeIndicator'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { formatCount, formatMoney, formatPercent } from '@/lib/format'
import { layoutTransition } from '@/lib/motion'
import { cn } from '@/lib/utils'

type RankingRow = components['schemas']['RankingRow']

/** Ranking row that animates to its new position when the metric/period reorders the list (T9). */
const MotionRow = motion.create(TableRow)

/**
 * Manager ranking table (D7): sports numbering (equal ranks share a number), top-3 highlighted,
 * managers without sales muted at the bottom with «Нет продаж» instead of a rank. The period-over-
 * period change is by metric, not by position (RANKING-CHANGE).
 */
export function RankingTable({ items }: { items: RankingRow[] }) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="w-12">Место</TableHead>
          <TableHead>Менеджер</TableHead>
          <TableHead className="text-right">Продажи</TableHead>
          <TableHead className="text-right">Выручка</TableHead>
          <TableHead className="text-right">Прибыль</TableHead>
          <TableHead className="text-right">Средний чек</TableHead>
          <TableHead className="text-right">Маржа</TableHead>
          <TableHead className="text-right">Изменение</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {items.map((row) => {
          const rank = row.rank == null ? null : Number(row.rank)
          const isTop = rank != null && rank <= 3
          return (
            <MotionRow
              key={row.managerId}
              layout
              transition={layoutTransition}
              className={cn(!row.hasSales && 'text-muted-foreground')}
            >
              <TableCell>
                {rank == null ? (
                  <span className="text-xs text-muted-foreground">Нет продаж</span>
                ) : (
                  <span
                    className={cn(
                      'inline-flex size-6 items-center justify-center rounded-full text-sm font-semibold tabular-nums',
                      isTop && 'bg-primary text-primary-foreground',
                    )}
                  >
                    {rank}
                  </span>
                )}
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-2">
                  <Avatar
                    id={row.managerId}
                    firstName={row.firstName}
                    lastName={row.lastName}
                    className={cn(!row.hasSales && 'opacity-60')}
                  />
                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-foreground">
                        {row.firstName} {row.lastName}
                      </span>
                      {!row.isActive ? (
                        <Badge variant="secondary" className="text-[10px]">
                          Неактивен
                        </Badge>
                      ) : null}
                    </div>
                    <span className="text-xs text-muted-foreground">{row.team}</span>
                  </div>
                </div>
              </TableCell>
              <TableCell className="text-right tabular-nums">
                {formatCount(row.salesCount)}
              </TableCell>
              <TableCell className="text-right tabular-nums">{formatMoney(row.revenue)}</TableCell>
              <TableCell className="text-right tabular-nums">
                {formatMoney(row.grossProfit)}
              </TableCell>
              <TableCell className="text-right tabular-nums">
                {formatMoney(row.averageCheck)}
              </TableCell>
              <TableCell className="text-right tabular-nums">{formatPercent(row.margin)}</TableCell>
              <TableCell className="text-right">
                <span className="inline-flex justify-end">
                  <ChangeIndicator value={row.metricChange} />
                </span>
              </TableCell>
            </MotionRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
