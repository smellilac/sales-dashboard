import type { ReactNode } from 'react'
import { http, HttpResponse } from 'msw'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { screen, waitFor } from '@testing-library/react'

import { resolvePreset } from '@/period/presets'

// Recharts renders synchronously and slowly in jsdom, blocking the event loop long enough to break
// waitFor timing. The charts aren't under test here (they're covered by their own units), so stub
// them with pass-through elements to keep the full-dashboard render cheap and deterministic.
vi.mock('recharts', () => {
  const Stub = ({ children }: { children?: ReactNode }) => <div>{children}</div>
  return {
    ResponsiveContainer: Stub,
    ComposedChart: Stub,
    Area: Stub,
    Line: Stub,
    Bar: Stub,
    XAxis: Stub,
    YAxis: Stub,
    CartesianGrid: Stub,
    Tooltip: Stub,
    Legend: Stub,
    PieChart: Stub,
    Pie: Stub,
    Cell: Stub,
  }
})

/** A promise a test resolves by hand, to hold a request in its loading state deterministically. */
function deferred(): { promise: Promise<void>; resolve: () => void } {
  let resolve!: () => void
  const promise = new Promise<void>((r) => {
    resolve = r
  })
  return { promise, resolve }
}

import {
  categoriesEmptyResponse,
  kpiEmptyResponse,
  problemDetails,
  rankingEmptyResponse,
  rankingResponse,
  recentSalesEmptyResponse,
  timeseriesEmptyResponse,
  topProductsEmptyResponse,
} from '@/test/fixtures'
import { lastRequest, requests } from '@/test/handlers'
import { renderWithProviders } from '@/test/renderWithProviders'
import { server } from '@/test/server'

import { Dashboard } from './App'

const KPI_PATH = '/api/dashboard/kpis'

beforeEach(() => {
  // Start each test from a clean URL so a previous ?period/?from leak never changes the period.
  window.history.replaceState(null, '', '/')
})

// Faking the clock stalls React Query / MSW async, so instead of pinning time we derive the
// expected days from the same resolvePreset the app uses — deterministic against the real clock and
// independent of the machine time zone (the suite runs under TZ=America/New_York; presets resolve
// in the Moscow business zone).

async function kpiFrom(): Promise<string | null | undefined> {
  await waitFor(() => expect(lastRequest(KPI_PATH)).toBeDefined())
  return lastRequest(KPI_PATH)?.params.get('from')
}

describe('App — period selection (T9 #1, #2)', () => {
  it('clicking a preset updates the URL and sends requests with the right from/to', async () => {
    const user = userEvent.setup()
    const preset30d = resolvePreset('30d')
    const preset7d = resolvePreset('7d')
    renderWithProviders(<Dashboard />)

    // Default preset is 30d.
    await waitFor(() => expect(lastRequest(KPI_PATH)?.params.get('from')).toBe(preset30d.from))
    expect(lastRequest(KPI_PATH)?.params.get('to')).toBe(preset30d.to)

    await user.click(screen.getByRole('radio', { name: '7 дней' }))

    // URL carries the preset; requests now use the 7-day window ending on the same day.
    expect(window.location.search).toBe('?period=7d')
    await waitFor(() => expect(lastRequest(KPI_PATH)?.params.get('from')).toBe(preset7d.from))
    expect(lastRequest(KPI_PATH)?.params.get('to')).toBe(preset7d.to)
  })

  it('picking a custom range writes ?from&to to the URL and to requests', async () => {
    const user = userEvent.setup()
    renderWithProviders(<Dashboard />)

    await waitFor(() => expect(lastRequest(KPI_PATH)).toBeDefined())

    // The calendar opens on the month of the period end (today). Pick two mid-month days (always real
    // in-month cells, never adjacent-month "outside" duplicates) to form a range.
    const monthAnchor = new Date(`${resolvePreset('30d').to}T00:00:00`)
    const d1 = new Date(monthAnchor.getFullYear(), monthAnchor.getMonth(), 10)
    const d2 = new Date(monthAnchor.getFullYear(), monthAnchor.getMonth(), 20)
    const cell = (d: Date) =>
      document.querySelector(`[data-day="${d.toLocaleDateString()}"]`) as HTMLElement

    await user.click(screen.getByRole('button', { name: /Произвольный/ }))
    await user.click(cell(d1))
    // Add the second endpoint only if the first click did not already close the range.
    if (!new URLSearchParams(window.location.search).get('from')) await user.click(cell(d2))

    // Whatever days the calendar resolved to, the period is a custom ?from&to range (not ?period),
    // and the same bounds are sent to the API.
    const params = new URLSearchParams(window.location.search)
    const from = params.get('from')
    const to = params.get('to')
    expect(from).toBeTruthy()
    expect(to).toBeTruthy()
    expect(params.get('period')).toBeNull()
    await waitFor(() => expect(lastRequest(KPI_PATH)?.params.get('from')).toBe(from))
    expect(lastRequest(KPI_PATH)?.params.get('to')).toBe(to)
  })
})

describe('App — API base path', () => {
  it('sends the KPI request once at the exact /api path, never a doubled /api/api', async () => {
    renderWithProviders(<Dashboard />)

    // The dashboard has issued its KPI request.
    await waitFor(() => expect(lastRequest(KPI_PATH)).toBeDefined())

    // Exactly the schema path — a doubled base URL would produce /api/api/dashboard/kpis, which
    // no handler matches (onUnhandledRequest: 'error') and which this assertion would catch anyway.
    expect(requests.filter((r) => r.path === KPI_PATH)).toHaveLength(1)
    expect(requests.some((r) => r.path.startsWith('/api/api'))).toBe(false)
  })
})

describe('App — loading and refetch states (T9 #5, #6)', () => {
  it('shows a skeleton on first load, then the data', async () => {
    // Hold the ranking request open so its block stays in the skeleton state while we assert.
    const gate = deferred()
    server.use(
      http.get('/api/dashboard/managers/ranking', async () => {
        await gate.promise
        return HttpResponse.json(rankingResponse)
      }),
    )

    renderWithProviders(<Dashboard />)

    // While the ranking request is in flight, its block shows a skeleton (data-slot="skeleton").
    await waitFor(() => expect(document.querySelector('[data-slot="skeleton"]')).not.toBeNull())

    // After the response, the ranking data replaces the skeleton.
    gate.resolve()
    await waitFor(() => expect(screen.getByText('Анна Смирнова')).toBeInTheDocument())
  })

  it('on a period change keeps old data with a refresh spinner and no skeleton', async () => {
    const user = userEvent.setup()
    renderWithProviders(<Dashboard />)

    await waitFor(() => expect(screen.getByText('Анна Смирнова')).toBeInTheDocument())

    // Hold the ranking refetch triggered by the period change so we can observe the transition.
    const gate = deferred()
    server.use(
      http.get('/api/dashboard/managers/ranking', async () => {
        await gate.promise
        return HttpResponse.json(rankingResponse)
      }),
    )

    await user.click(screen.getByRole('radio', { name: '7 дней' }))

    // Refresh indicator appears, the previous rows stay visible, and no skeleton flashes.
    await waitFor(() =>
      expect(document.querySelector('[aria-label="Обновление"]')).not.toBeNull(),
    )
    const spinner = document.querySelector('[aria-label="Обновление"]') as HTMLElement
    expect(screen.getByText('Анна Смирнова')).toBeInTheDocument()
    const rankingBlock = spinner.closest('[data-slot="card"]') as HTMLElement
    expect(rankingBlock.querySelector('[data-slot="skeleton"]')).toBeNull()

    gate.resolve()
    await waitFor(() =>
      expect(document.querySelector('[aria-label="Обновление"]')).toBeNull(),
    )
  })
})

describe('App — per-block error isolation (T9 #7)', () => {
  it('shows an error with retry in only the failed block; retry recovers it', async () => {
    server.use(
      http.get('/api/dashboard/managers/ranking', () =>
        HttpResponse.json(problemDetails, { status: 500 }),
      ),
    )

    const user = userEvent.setup()
    renderWithProviders(<Dashboard />)

    // The ranking block is the only one in error: exactly one «Повторить», other blocks have data.
    await waitFor(() =>
      expect(screen.getAllByRole('button', { name: 'Повторить' })).toHaveLength(1),
    )
    expect(await kpiFrom()).toBe(resolvePreset('30d').from)
    expect(screen.getByText('ООО Ромашка')).toBeInTheDocument()

    // Fix the endpoint and retry → the block recovers.
    server.use(
      http.get('/api/dashboard/managers/ranking', () => HttpResponse.json(rankingResponse)),
    )
    await user.click(screen.getByRole('button', { name: 'Повторить' }))

    await waitFor(() => expect(screen.getByText('Анна Смирнова')).toBeInTheDocument())
    expect(screen.queryByRole('button', { name: 'Повторить' })).not.toBeInTheDocument()
  })
})

describe('App — empty period (T9 #8)', () => {
  it('renders «—», «нет данных для сравнения» and per-block empty messages', async () => {
    server.use(
      http.get('/api/dashboard/kpis', () => HttpResponse.json(kpiEmptyResponse)),
      http.get('/api/dashboard/managers/ranking', () =>
        HttpResponse.json(rankingEmptyResponse),
      ),
      http.get('/api/dashboard/timeseries', () => HttpResponse.json(timeseriesEmptyResponse)),
      http.get('/api/dashboard/categories', () => HttpResponse.json(categoriesEmptyResponse)),
      http.get('/api/dashboard/products/top', () =>
        HttpResponse.json(topProductsEmptyResponse),
      ),
      http.get('/api/dashboard/sales/recent', () =>
        HttpResponse.json(recentSalesEmptyResponse),
      ),
    )

    renderWithProviders(<Dashboard />)

    // KPI: null metrics render «—» and null comparisons render the D6 «нет данных» message.
    await waitFor(() =>
      expect(screen.getAllByText('нет данных для сравнения').length).toBeGreaterThan(0),
    )
    expect(screen.getAllByText('—').length).toBeGreaterThan(0)

    // Each block shows its own empty message (D9).
    expect(await screen.findByText('За выбранный период продаж нет')).toBeInTheDocument()
    expect(screen.getByText('Нет сделок за период')).toBeInTheDocument()
    expect(screen.getByText('Нет продаж за период')).toBeInTheDocument()
  })
})
