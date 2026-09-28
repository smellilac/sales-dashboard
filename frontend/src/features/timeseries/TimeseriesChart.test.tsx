import { render } from '@testing-library/react'
import { describe, expect, test } from 'vitest'

import { TimeseriesChart, type ChartPoint } from './TimeseriesChart'

// Three daily buckets; container size is mocked globally in src/test/setup.ts.
const points: ChartPoint[] = [
  { bucketStart: '2026-09-22', bucketEnd: '2026-09-22', revenue: 100, grossProfit: 40, salesCount: 3 },
  { bucketStart: '2026-09-23', bucketEnd: '2026-09-23', revenue: 200, grossProfit: 80, salesCount: 5 },
  { bucketStart: '2026-09-24', bucketEnd: '2026-09-24', revenue: 150, grossProfit: 60, salesCount: 2 },
]

describe('TimeseriesChart', () => {
  test('money mode draws line curves with real X coordinates (no NaN)', () => {
    const { container } = render(
      <TimeseriesChart data={points} mode="money" granularity="Day" />,
    )

    const curves = Array.from(container.querySelectorAll('.recharts-line-curve'))
    expect(curves.length).toBeGreaterThan(0)

    for (const curve of curves) {
      const d = curve.getAttribute('d') ?? ''
      expect(d).not.toBe('')
      expect(d).not.toContain('NaN')
    }
  })

  test('sales mode draws one bar rectangle per bucket', () => {
    const { container } = render(
      <TimeseriesChart data={points} mode="sales" granularity="Day" />,
    )

    expect(container.querySelectorAll('.recharts-bar-rectangle')).toHaveLength(3)
  })
})
