import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it } from 'vitest'
import { screen, waitFor, within } from '@testing-library/react'

import type { ActivePeriod } from '@/period/types'
import { lastRequest } from '@/test/handlers'
import { renderWithProviders } from '@/test/renderWithProviders'

import { RankingBlock } from './RankingBlock'

const PERIOD: ActivePeriod = { preset: '30d', from: '2026-08-26', to: '2026-09-24' }
const RANKING_PATH = '/api/dashboard/managers/ranking'

beforeEach(() => {
  // Reset the URL so ?rankBy from a previous test never leaks in (useRankBy reads it on mount).
  window.history.replaceState(null, '', '/')
})

/** Manager names in body-row order, so tests can assert the ranking matches the response order. */
function rowNames(): string[] {
  const table = screen.getByRole('table')
  const bodyRows = within(table).getAllByRole('row').slice(1) // drop the header row
  return bodyRows.map((row) => {
    const name = row.querySelector('.font-medium')?.textContent?.trim()
    return name ?? ''
  })
}

describe('RankingBlock — metric toggle (T9 #3)', () => {
  it('switching to «Средний чек» sends rankBy=AverageCheck, puts ?rankBy in the URL, and reorders', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RankingBlock period={PERIOD} />)

    // Default order is by gross profit.
    await waitFor(() => expect(rowNames()[0]).toBe('Анна Смирнова'))
    expect(lastRequest(RANKING_PATH)?.params.get('rankBy')).toBe('GrossProfit')

    await user.click(screen.getByRole('radio', { name: 'Средний чек' }))

    await waitFor(() =>
      expect(lastRequest(RANKING_PATH)?.params.get('rankBy')).toBe('AverageCheck'),
    )
    expect(new URLSearchParams(window.location.search).get('rankBy')).toBe('averageCheck')

    // The order is exactly what the server returned for average check.
    await waitFor(() =>
      expect(rowNames()).toEqual(['Вера Новикова', 'Анна Смирнова', 'Борис Ковалёв']),
    )
  })
})

describe('RankingBlock — sports numbering and no-sales (T9 #4)', () => {
  it('shows tied ranks as one number and a no-sales manager at the bottom', async () => {
    renderWithProviders(<RankingBlock period={PERIOD} />)

    await waitFor(() => expect(screen.getByText('Анна Смирнова')).toBeInTheDocument())

    const table = screen.getByRole('table')
    const bodyRows = within(table).getAllByRole('row').slice(1)

    // Two managers tie for 1st (1, 1, 3 — D7), so exactly two rank badges read «1» and one «3».
    expect(within(table).getAllByText('1')).toHaveLength(2)
    expect(within(table).getByText('3')).toBeInTheDocument()

    // The no-sales manager is last, has no rank number, and reads «Нет продаж» (D7).
    const lastRow = bodyRows[bodyRows.length - 1]
    expect(within(lastRow).getByText('Глеб Морозов')).toBeInTheDocument()
    expect(within(lastRow).getByText('Нет продаж')).toBeInTheDocument()
  })
})
