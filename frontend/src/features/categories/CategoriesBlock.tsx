import { useCategories } from '@/api/queries/useCategories'
import { DashboardBlock } from '@/components/block/DashboardBlock'
import { Skeleton } from '@/components/ui/skeleton'
import type { ActivePeriod } from '@/period/types'

import { CategoriesDonut } from './CategoriesDonut'
import { CategoriesTable } from './CategoriesTable'

/** Category mix (CAT-COUNT): a revenue-share donut plus a per-category table. */
export function CategoriesBlock({
  period,
  className,
}: {
  period: ActivePeriod
  className?: string
}) {
  const { data, isPending, isError, isFetching, error, refetch } = useCategories(period)

  const items = data?.items ?? []
  const isEmpty = items.length === 0 || items.every((i) => Number(i.revenue) === 0)

  return (
    <DashboardBlock
      title="Категории"
      className={className}
      status={isPending ? 'pending' : isError || !data ? 'error' : 'ready'}
      isFetching={isFetching}
      isEmpty={isEmpty}
      error={error}
      onRetry={() => void refetch()}
      skeleton={
        <div className="space-y-4">
          <Skeleton className="mx-auto size-44 rounded-full" />
          <Skeleton className="h-40 w-full" />
        </div>
      }
      empty="Нет продаж по категориям за период"
    >
      <div className="space-y-4">
        <CategoriesDonut items={items} />
        <CategoriesTable items={items} />
      </div>
    </DashboardBlock>
  )
}
