import {
  Area,
  Bar,
  CartesianGrid,
  ComposedChart,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import {
  formatBucketRange,
  formatCompactMoney,
  formatCount,
  formatMoney,
} from '@/lib/format'
import { CHART_DURATION } from '@/lib/motion'

import type { TimeseriesMode } from './mode'

/** A chart row: bucket bounds carried through for the tooltip, plus the numeric series. */
export interface ChartPoint {
  label: string
  bucketStart: string
  bucketEnd: string
  revenue: number
  grossProfit: number
  salesCount: number
}

const REVENUE_COLOR = 'var(--chart-1)'
const PROFIT_COLOR = 'var(--chart-3)'
const SALES_COLOR = 'var(--chart-1)'

interface TimeseriesChartProps {
  data: ChartPoint[]
  mode: TimeseriesMode
}

/** Revenue/profit areas or a Paid-sales bar chart; the server picks the bucket step (TIMESERIES). */
export function TimeseriesChart({ data, mode }: TimeseriesChartProps) {
  return (
    <ResponsiveContainer width="100%" height={288}>
      <ComposedChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
        <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--border)" />
        <XAxis
          dataKey="label"
          tick={{ fontSize: 12, fill: 'var(--muted-foreground)' }}
          tickLine={false}
          axisLine={{ stroke: 'var(--border)' }}
          minTickGap={16}
        />
        <YAxis
          width={64}
          tick={{ fontSize: 12, fill: 'var(--muted-foreground)' }}
          tickLine={false}
          axisLine={false}
          tickFormatter={(v: number) =>
            mode === 'money' ? formatCompactMoney(v) : formatCount(v)
          }
        />
        <Tooltip content={<TimeseriesTooltip mode={mode} />} />
        {mode === 'money' ? (
          <>
            <Legend />
            <Area
              type="monotone"
              dataKey="revenue"
              name="Выручка"
              stroke={REVENUE_COLOR}
              fill={REVENUE_COLOR}
              fillOpacity={0.15}
              strokeWidth={2}
              animationDuration={CHART_DURATION}
            />
            <Area
              type="monotone"
              dataKey="grossProfit"
              name="Валовая прибыль"
              stroke={PROFIT_COLOR}
              fill={PROFIT_COLOR}
              fillOpacity={0.15}
              strokeWidth={2}
              animationDuration={CHART_DURATION}
            />
          </>
        ) : (
          <Bar
            dataKey="salesCount"
            name="Продажи"
            fill={SALES_COLOR}
            radius={[4, 4, 0, 0]}
            animationDuration={CHART_DURATION}
          />
        )}
      </ComposedChart>
    </ResponsiveContainer>
  )
}

interface TooltipProps {
  active?: boolean
  payload?: { payload: ChartPoint }[]
  mode: TimeseriesMode
}

function TimeseriesTooltip({ active, payload, mode }: TooltipProps) {
  if (!active || !payload?.length) return null
  const point = payload[0].payload
  return (
    <div className="rounded-md border bg-popover px-3 py-2 text-xs shadow-md">
      <p className="mb-1 font-medium">{formatBucketRange(point.bucketStart, point.bucketEnd)}</p>
      {mode === 'money' ? (
        <div className="space-y-0.5 tabular-nums">
          <p>Выручка: {formatMoney(point.revenue)}</p>
          <p>Прибыль: {formatMoney(point.grossProfit)}</p>
        </div>
      ) : (
        <p className="tabular-nums">Продажи: {formatCount(point.salesCount)}</p>
      )}
    </div>
  )
}
