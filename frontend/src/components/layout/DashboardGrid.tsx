import { CategoriesBlock } from '@/features/categories/CategoriesBlock'
import { KpiBlock } from '@/features/kpis/KpiBlock'
import { RankingBlock } from '@/features/ranking/RankingBlock'
import { RecentSalesBlock } from '@/features/recent-sales/RecentSalesBlock'
import { TimeseriesBlock } from '@/features/timeseries/TimeseriesBlock'
import { TopProductsBlock } from '@/features/top-products/TopProductsBlock'
import type { ActivePeriod } from '@/period/types'

/**
 * Dashboard blocks in their final layout for 1440×900 (T8): KPI row, dynamics (8) + categories (4),
 * full-width ranking, then top products (5) + recent sales (7). Each block owns its own loading,
 * error and empty states (D9) via DashboardBlock.
 */
export function DashboardGrid({ period }: { period: ActivePeriod }) {
  return (
    <div className="space-y-4">
      <KpiBlock period={period} />

      <div className="grid grid-cols-1 gap-4 xl:grid-cols-12">
        <TimeseriesBlock period={period} className="xl:col-span-8" />
        <CategoriesBlock period={period} className="xl:col-span-4" />
        <RankingBlock period={period} className="xl:col-span-12" />
        <TopProductsBlock period={period} className="xl:col-span-5" />
        <RecentSalesBlock period={period} className="xl:col-span-7" />
      </div>
    </div>
  )
}
