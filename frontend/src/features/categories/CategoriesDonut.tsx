import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'

import type { components } from '@/api/schema'
import { formatMoney, formatPercent } from '@/lib/format'

import { categoryColor } from './colors'

type CategoryRow = components['schemas']['CategoryRow']

interface Slice {
  categoryId: string
  name: string
  value: number
  share: components['schemas']['CategoryRow']['revenueShare']
  color: string
}

/** Revenue-share donut for categories (CAT-COUNT: share of revenue, not sales count). */
export function CategoriesDonut({ items }: { items: CategoryRow[] }) {
  const slices: Slice[] = items
    .map((item, index) => ({
      categoryId: item.categoryId,
      name: item.name,
      value: Number(item.revenue),
      share: item.revenueShare,
      color: categoryColor(index),
    }))
    .filter((s) => s.value > 0)

  return (
    <ResponsiveContainer width="100%" height={180}>
      <PieChart>
        <Pie
          data={slices}
          dataKey="value"
          nameKey="name"
          innerRadius={45}
          outerRadius={80}
          paddingAngle={2}
          stroke="var(--card)"
        >
          {slices.map((slice) => (
            <Cell key={slice.categoryId} fill={slice.color} />
          ))}
        </Pie>
        <Tooltip content={<DonutTooltip />} />
      </PieChart>
    </ResponsiveContainer>
  )
}

interface DonutTooltipProps {
  active?: boolean
  payload?: { payload: Slice }[]
}

function DonutTooltip({ active, payload }: DonutTooltipProps) {
  if (!active || !payload?.length) return null
  const slice = payload[0].payload
  return (
    <div className="rounded-md border bg-popover px-3 py-2 text-xs shadow-md">
      <p className="font-medium">{slice.name}</p>
      <p className="tabular-nums">
        {formatMoney(slice.value)} · {formatPercent(slice.share)}
      </p>
    </div>
  )
}
