import { BlockCard } from '@/components/layout/BlockCard'
import { KpiCards } from '@/components/layout/KpiCards'
import type { ActivePeriod } from '@/period/types'

/**
 * Dashboard blocks in their final layout (T7). KPI values are live; the remaining blocks are
 * skeleton placeholders filled in T8 (charts, tables, empty/error states).
 */
export function DashboardGrid({ period }: { period: ActivePeriod }) {
  return (
    <div className="space-y-4">
      <KpiCards period={period} />

      <div className="grid grid-cols-1 gap-4 xl:grid-cols-12">
        <BlockCard
          title="Динамика продаж"
          description="Выручка и прибыль по периодам"
          className="xl:col-span-8"
        />
        <BlockCard
          title="Категории"
          description="Доля и маржа по категориям"
          className="xl:col-span-4"
        />
        <BlockCard
          title="Рейтинг менеджеров"
          description="Места по валовой прибыли"
          className="xl:col-span-6"
        />
        <BlockCard
          title="Топ товаров"
          description="10 товаров по валовой прибыли"
          className="xl:col-span-6"
        />
        <BlockCard
          title="Последние продажи"
          description="Все статусы, keyset-пагинация"
          className="xl:col-span-12"
        />
      </div>
    </div>
  )
}
