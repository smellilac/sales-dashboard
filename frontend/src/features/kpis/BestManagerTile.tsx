import type { components } from '@/api/schema'
import { Avatar } from '@/components/Avatar'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatMoney } from '@/lib/format'

type BestManager = components['schemas']['BestManagerDto']

/** Best-manager KPI card: the top manager by gross profit (D7), «Нет продаж» when null. */
export function BestManagerTile({ best }: { best: BestManager | null }) {
  return (
    <Card className="gap-2 py-4">
      <CardHeader className="px-4">
        <CardTitle className="text-xs font-medium text-muted-foreground">Лучший менеджер</CardTitle>
      </CardHeader>
      <CardContent className="px-4">
        {best ? (
          <div className="flex items-center gap-2">
            <Avatar id={best.managerId} firstName={best.firstName} lastName={best.lastName} />
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold">
                {best.firstName} {best.lastName}
              </p>
              <p className="text-xs tabular-nums text-muted-foreground">
                {formatMoney(best.grossProfit)}
              </p>
              {Number(best.tiedCount) > 0 ? (
                <p className="text-xs text-muted-foreground">
                  и ещё {best.tiedCount} с тем же результатом
                </p>
              ) : null}
            </div>
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">Нет продаж</p>
        )}
      </CardContent>
    </Card>
  )
}
