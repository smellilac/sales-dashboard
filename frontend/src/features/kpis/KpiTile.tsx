import type { ReactNode } from 'react'

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

interface KpiTileProps {
  title: string
  /** Formatted main value; already «—» when the metric is null (D6). */
  value: string
  /** Change row (ChangeIndicator) or an extra caption. */
  footer: ReactNode
}

/** One compact KPI card: title, value, change (API-KPI). */
export function KpiTile({ title, value, footer }: KpiTileProps) {
  return (
    <Card className="gap-2 py-4">
      <CardHeader className="px-4">
        <CardTitle className="text-xs font-medium text-muted-foreground">{title}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-1 px-4">
        <p className="text-xl font-semibold tabular-nums">{value}</p>
        <div className="text-xs">{footer}</div>
      </CardContent>
    </Card>
  )
}
