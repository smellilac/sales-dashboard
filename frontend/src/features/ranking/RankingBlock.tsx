import { useRanking } from '@/api/queries/useRanking'
import { DashboardBlock } from '@/components/block/DashboardBlock'
import { Skeleton } from '@/components/ui/skeleton'
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group'
import type { ActivePeriod } from '@/period/types'

import { RANK_BY_LABELS, toApiRankBy, type RankByUrl } from './rankBy'
import { RankingTable } from './RankingTable'
import { useRankBy } from './useRankBy'

/** Manager ranking (D7, RANKING-CHANGE): metric toggle in the URL, sports numbering, top-3. */
export function RankingBlock({
  period,
  className,
}: {
  period: ActivePeriod
  className?: string
}) {
  const { rankBy, setRankBy } = useRankBy()
  const { data, isPending, isError, isFetching, error, refetch } = useRanking(
    period,
    toApiRankBy(rankBy),
  )

  const items = data?.items ?? []
  const noSales = items.length > 0 && items.every((i) => !i.hasSales)

  return (
    <DashboardBlock
      title="Рейтинг менеджеров"
      className={className}
      status={isPending ? 'pending' : isError || !data ? 'error' : 'ready'}
      isFetching={isFetching}
      isEmpty={items.length === 0}
      error={error}
      onRetry={() => void refetch()}
      skeleton={<Skeleton className="h-64 w-full" />}
      empty="За выбранный период продаж нет"
      headerRight={
        <ToggleGroup
          type="single"
          size="sm"
          variant="outline"
          value={rankBy}
          onValueChange={(v) => {
            if (v) setRankBy(v as RankByUrl)
          }}
        >
          <ToggleGroupItem value="grossProfit">{RANK_BY_LABELS.grossProfit}</ToggleGroupItem>
          <ToggleGroupItem value="averageCheck">{RANK_BY_LABELS.averageCheck}</ToggleGroupItem>
        </ToggleGroup>
      }
    >
      <div className="space-y-3">
        {noSales ? (
          <p className="text-sm text-muted-foreground">За выбранный период продаж нет</p>
        ) : null}
        <RankingTable items={items} />
      </div>
    </DashboardBlock>
  )
}
