import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import {
  formatBucketRange,
  formatBucketTick,
  formatCompactMoney,
  formatCount,
  formatMoney,
  type Granularity,
} from '@/lib/format'
import { CHART_DURATION } from '@/lib/motion'

import type { TimeseriesMode } from './mode'

/** A chart row: bucket bounds carried through for the tooltip, plus the numeric series. */
export interface ChartPoint {
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
  granularity: Granularity
}

/** Revenue/profit lines or a Paid-sales bar chart; the server picks the bucket step (TIMESERIES). */
export function TimeseriesChart({ data, mode, granularity }: TimeseriesChartProps) {
  // The X axis is categorical over the string bucketStart (D4: no Date/TZDate in chart data); the tick label
  // is derived by step. bucketEnd is looked up by bucketStart so week ticks can show a range.
  const endByStart = new Map(data.map((p) => [p.bucketStart, p.bucketEnd]))
  const formatTick = (bucketStart: string) =>
    formatBucketTick(bucketStart, endByStart.get(bucketStart) ?? bucketStart, granularity)

  return (
    <div className="space-y-2">
      {mode === 'money' ? (
        <ChartLegend
          items={[
            { label: 'Выручка', color: REVENUE_COLOR },
            { label: 'Валовая прибыль', color: PROFIT_COLOR },
          ]}
        />
      ) : null}
      <ResponsiveContainer width="100%" height={288}>
        <ComposedChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
          <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--border)" />
          <XAxis
            type="category"
            dataKey="bucketStart"
            tickFormatter={formatTick}
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
              <Line
                type="monotone"
                dataKey="revenue"
                name="Выручка"
                stroke={REVENUE_COLOR}
                strokeWidth={2}
                dot={false}
                animationDuration={CHART_DURATION}
              />
              <Line
                type="monotone"
                dataKey="grossProfit"
                name="Валовая прибыль"
                stroke={PROFIT_COLOR}
                strokeWidth={2}
                dot={false}
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
    </div>
  )
}

/** Small legend rendered outside the SVG so it never eats into the plot area (recharts <Legend> does). */
function ChartLegend({ items }: { items: { label: string; color: string }[] }) {
  return (
    <div className="flex items-center justify-center gap-4 text-xs text-muted-foreground">
      {items.map((item) => (
        <span key={item.label} className="flex items-center gap-1.5">
          <span
            className="inline-block h-2 w-2 rounded-full"
            style={{ backgroundColor: item.color }}
          />
          {item.label}
        </span>
      ))}
    </div>
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
