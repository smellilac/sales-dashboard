import type { components } from '@/api/schema'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { formatCount, formatMoney, formatPercent } from '@/lib/format'
import { cn } from '@/lib/utils'

import { categoryColor } from './colors'

type CategoryRow = components['schemas']['CategoryRow']

/** Per-category figures (CAT-COUNT): revenue, profit, margin, units. Zero-revenue rows are muted. */
export function CategoriesTable({ items }: { items: CategoryRow[] }) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Категория</TableHead>
          <TableHead className="text-right">Выручка</TableHead>
          <TableHead className="text-right">Прибыль</TableHead>
          <TableHead className="text-right">Маржа</TableHead>
          <TableHead className="text-right">Единицы</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {items.map((item, index) => {
          const muted = Number(item.revenue) === 0
          return (
            <TableRow key={item.categoryId} className={cn(muted && 'text-muted-foreground')}>
              <TableCell>
                <span className="flex items-center gap-2">
                  <span
                    className="size-2.5 shrink-0 rounded-full"
                    style={{ backgroundColor: muted ? 'var(--muted-foreground)' : categoryColor(index) }}
                  />
                  {item.name}
                </span>
              </TableCell>
              <TableCell className="text-right tabular-nums">{formatMoney(item.revenue)}</TableCell>
              <TableCell className="text-right tabular-nums">
                {formatMoney(item.grossProfit)}
              </TableCell>
              <TableCell className="text-right tabular-nums">{formatPercent(item.margin)}</TableCell>
              <TableCell className="text-right tabular-nums">
                {formatCount(item.unitsSold)}
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
