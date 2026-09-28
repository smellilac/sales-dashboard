import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'

import type { ActivePeriod } from '@/period/types'
import { kpiEmptyResponse, kpiResponse } from '@/test/fixtures'
import { renderWithProviders } from '@/test/renderWithProviders'
import { server } from '@/test/server'

import { KpiBlock } from './KpiBlock'

const PERIOD: ActivePeriod = { preset: '30d', from: '2026-08-26', to: '2026-09-24' }

/** The card whose title matches `title`, so assertions can scope to one KPI tile. */
function tile(title: string): HTMLElement {
  const heading = screen.getByText(title)
  const card = heading.closest('[data-slot="card"]')
  if (!(card instanceof HTMLElement)) throw new Error(`No card for tile "${title}"`)
  return card
}

describe('KpiBlock — Возвраты change colour (T9 #9)', () => {
  it('renders a falling refund rate as green (growth is bad, so a fall is good)', async () => {
    renderWithProviders(<KpiBlock period={PERIOD} />)

    // Fixture: refunds.rate.changePoints = -0.013 → with invert this is an improvement.
    await waitFor(() => expect(tile('Возвраты').querySelector('.text-positive')).not.toBeNull())
    expect(tile('Возвраты').querySelector('.text-negative')).toBeNull()
  })

  it('renders a rising refund rate as red', async () => {
    server.use(
      http.get('*/api/dashboard/kpis', () =>
        HttpResponse.json({
          ...kpiResponse,
          refunds: {
            amount: { current: 60_000, previous: 51_000, change: 0.1765 },
            rate: { current: 0.06, previous: 0.045, changePoints: 0.015 },
          },
        }),
      ),
    )

    renderWithProviders(<KpiBlock period={PERIOD} />)

    await waitFor(() => expect(tile('Возвраты').querySelector('.text-negative')).not.toBeNull())
    expect(tile('Возвраты').querySelector('.text-positive')).toBeNull()
  })
})

describe('KpiBlock — empty period (T9 #8)', () => {
  it('shows «—» values and «нет данных для сравнения» when everything is null (D6)', async () => {
    server.use(http.get('*/api/dashboard/kpis', () => HttpResponse.json(kpiEmptyResponse)))

    renderWithProviders(<KpiBlock period={PERIOD} />)

    // Margin and average check are null → «—»; comparisons are null → the D6 message.
    await waitFor(() => expect(screen.getByText('Маржа')).toBeInTheDocument())
    expect(tile('Маржа').querySelector('.tabular-nums')?.textContent).toBe('—')
    expect(tile('Средний чек').querySelector('.tabular-nums')?.textContent).toBe('—')
    expect(screen.getAllByText('нет данных для сравнения').length).toBeGreaterThan(0)
    expect(screen.getByText('Нет продаж')).toBeInTheDocument()
  })
})
