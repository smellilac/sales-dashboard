import type { components } from '@/api/schema'

/**
 * Typed response fixtures for MSW handlers. Every shape is pinned to the committed OpenAPI schema
 * (`components['schemas'][...]`), so a contract change breaks the test build — the tests can never
 * silently drift from the real API (T9). `status` uses the numeric SaleStatus the document declares
 * (1 Paid / 2 Cancelled / 3 Refunded); `normalizeStatus` maps it just like the live string wire.
 */

type KpiResponse = components['schemas']['KpiResponse']
type RankingResponse = components['schemas']['RankingResponse']
type RankingRow = components['schemas']['RankingRow']
type TimeseriesResponse = components['schemas']['TimeseriesResponse']
type CategoriesResponse = components['schemas']['CategoriesResponse']
type TopProductsResponse = components['schemas']['TopProductsResponse']
type RecentSalesResponse = components['schemas']['RecentSalesResponse']
type RecentSaleRow = components['schemas']['RecentSaleRow']
type ProblemDetails = components['schemas']['ProblemDetails']

const PERIOD = { from: '2026-08-26', to: '2026-09-24' } as const
const PREVIOUS_PERIOD = { from: '2026-07-27', to: '2026-08-25' } as const

/** Full KPI response with sensible non-null values (revenue up, margin up, refunds down). */
export const kpiResponse: KpiResponse = {
  period: { ...PERIOD },
  previousPeriod: { ...PREVIOUS_PERIOD },
  revenue: { current: 1_250_000, previous: 1_100_000, change: 0.1364 },
  grossProfit: { current: 375_000, previous: 320_000, change: 0.1719 },
  salesCount: { current: 128, previous: 112, change: 0.1429 },
  averageCheck: { current: 9765.63, previous: 9821.43, change: -0.0057 },
  margin: { current: 0.3, previous: 0.29, changePoints: 0.01 },
  refunds: {
    amount: { current: 42_000, previous: 51_000, change: -0.1765 },
    // Rate fell (negative points) — with invert this must render green.
    rate: { current: 0.032, previous: 0.045, changePoints: -0.013 },
  },
  bestManager: {
    managerId: '018f0000-0000-7000-8000-000000000001',
    firstName: 'Иван',
    lastName: 'Петров',
    grossProfit: 98_000,
    tiedCount: 0,
  },
}

/** Empty-period KPI (D6): nulls everywhere, no best manager → «—» and «нет данных для сравнения». */
export const kpiEmptyResponse: KpiResponse = {
  period: { ...PERIOD },
  previousPeriod: { ...PREVIOUS_PERIOD },
  revenue: { current: 0, previous: 0, change: null },
  grossProfit: { current: 0, previous: 0, change: null },
  salesCount: { current: 0, previous: 0, change: null },
  averageCheck: { current: null, previous: null, change: null },
  margin: { current: null, previous: null, changePoints: null },
  refunds: {
    amount: { current: 0, previous: 0, change: null },
    rate: { current: null, previous: null, changePoints: null },
  },
  bestManager: null,
}

function rankingRow(overrides: Partial<RankingRow> & Pick<RankingRow, 'managerId'>): RankingRow {
  return {
    rank: 1,
    firstName: 'Имя',
    lastName: 'Фамилия',
    team: 'Команда А',
    isActive: true,
    hasSales: true,
    salesCount: 10,
    revenue: 500_000,
    grossProfit: 150_000,
    averageCheck: 50_000,
    margin: 0.3,
    metricChange: 0.05,
    ...overrides,
  }
}

/**
 * Ranking with a tie for 1st (sports numbering 1,1,3) and a no-sales manager at the bottom (D7).
 * The order here is exactly what the UI must render — the server already sorted it.
 */
export const rankingResponse: RankingResponse = {
  rankBy: 'GrossProfit',
  period: { ...PERIOD },
  previousPeriod: { ...PREVIOUS_PERIOD },
  items: [
    rankingRow({
      managerId: '018f0000-0000-7000-8000-0000000000a1',
      firstName: 'Анна',
      lastName: 'Смирнова',
      rank: 1,
      grossProfit: 200_000,
    }),
    rankingRow({
      managerId: '018f0000-0000-7000-8000-0000000000a2',
      firstName: 'Борис',
      lastName: 'Ковалёв',
      rank: 1,
      grossProfit: 200_000,
    }),
    rankingRow({
      managerId: '018f0000-0000-7000-8000-0000000000a3',
      firstName: 'Вера',
      lastName: 'Новикова',
      rank: 3,
      grossProfit: 120_000,
    }),
    rankingRow({
      managerId: '018f0000-0000-7000-8000-0000000000a4',
      firstName: 'Глеб',
      lastName: 'Морозов',
      rank: null,
      hasSales: false,
      salesCount: 0,
      revenue: 0,
      grossProfit: 0,
      averageCheck: null,
      margin: null,
      metricChange: null,
    }),
  ],
}

/** Same managers ordered by average check — a different order proves the toggle re-queries (T9 #3). */
export const rankingByAverageCheckResponse: RankingResponse = {
  rankBy: 'AverageCheck',
  period: { ...PERIOD },
  previousPeriod: { ...PREVIOUS_PERIOD },
  items: [
    rankingRow({
      managerId: '018f0000-0000-7000-8000-0000000000a3',
      firstName: 'Вера',
      lastName: 'Новикова',
      rank: 1,
      averageCheck: 80_000,
    }),
    rankingRow({
      managerId: '018f0000-0000-7000-8000-0000000000a1',
      firstName: 'Анна',
      lastName: 'Смирнова',
      rank: 2,
      averageCheck: 60_000,
    }),
    rankingRow({
      managerId: '018f0000-0000-7000-8000-0000000000a2',
      firstName: 'Борис',
      lastName: 'Ковалёв',
      rank: 3,
      averageCheck: 40_000,
    }),
  ],
}

export const rankingEmptyResponse: RankingResponse = {
  rankBy: 'GrossProfit',
  period: { ...PERIOD },
  previousPeriod: { ...PREVIOUS_PERIOD },
  items: [],
}

export const timeseriesResponse: TimeseriesResponse = {
  granularity: 'Day',
  period: { ...PERIOD },
  points: [
    { bucketStart: '2026-09-22', bucketEnd: '2026-09-22', revenue: 400_000, grossProfit: 120_000, salesCount: 40 },
    { bucketStart: '2026-09-23', bucketEnd: '2026-09-23', revenue: 450_000, grossProfit: 140_000, salesCount: 44 },
    { bucketStart: '2026-09-24', bucketEnd: '2026-09-24', revenue: 400_000, grossProfit: 115_000, salesCount: 44 },
  ],
}

export const timeseriesEmptyResponse: TimeseriesResponse = {
  granularity: 'Day',
  period: { ...PERIOD },
  points: [],
}

export const categoriesResponse: CategoriesResponse = {
  period: { ...PERIOD },
  items: [
    {
      categoryId: '018f0000-0000-7000-8000-0000000000b1',
      name: 'Дроны',
      revenue: 800_000,
      grossProfit: 240_000,
      margin: 0.3,
      unitsSold: 64,
      revenueShare: 0.64,
    },
    {
      categoryId: '018f0000-0000-7000-8000-0000000000b2',
      name: 'Камеры',
      revenue: 450_000,
      grossProfit: 135_000,
      margin: 0.3,
      unitsSold: 36,
      revenueShare: 0.36,
    },
  ],
}

export const categoriesEmptyResponse: CategoriesResponse = {
  period: { ...PERIOD },
  items: [],
}

export const topProductsResponse: TopProductsResponse = {
  period: { ...PERIOD },
  items: [
    {
      productId: '018f0000-0000-7000-8000-0000000000c1',
      sku: 'DJI-001',
      name: 'Mavic 4 Pro',
      categoryName: 'Дроны',
      revenue: 300_000,
      grossProfit: 90_000,
      margin: 0.3,
      unitsSold: 12,
    },
  ],
}

export const topProductsEmptyResponse: TopProductsResponse = {
  period: { ...PERIOD },
  items: [],
}

function saleRow(overrides: Partial<RecentSaleRow> & Pick<RecentSaleRow, 'saleId'>): RecentSaleRow {
  return {
    soldAt: '2026-09-24T10:30:00Z',
    managerId: '018f0000-0000-7000-8000-0000000000d1',
    managerName: 'Иван Петров',
    customerCompany: 'ООО Ромашка',
    status: 1,
    amount: 50_000,
    grossProfit: 15_000,
    items: [{ productName: 'Mavic 4 Pro', quantity: 1 }],
    ...overrides,
  }
}

/** First page of recent sales with a cursor → the "Показать ещё" button must show (T9 #10). */
export const recentSalesPage1: RecentSalesResponse = {
  nextCursor: 'cursor-page-2',
  items: [
    saleRow({ saleId: '018f0000-0000-7000-8000-0000000000e1', customerCompany: 'ООО Ромашка' }),
    saleRow({ saleId: '018f0000-0000-7000-8000-0000000000e2', customerCompany: 'АО Василёк' }),
  ],
}

/** Last page: nextCursor null → the button must disappear (T9 #10). */
export const recentSalesPage2: RecentSalesResponse = {
  nextCursor: null,
  items: [
    saleRow({ saleId: '018f0000-0000-7000-8000-0000000000e3', customerCompany: 'ЗАО Незабудка' }),
  ],
}

export const recentSalesEmptyResponse: RecentSalesResponse = {
  nextCursor: null,
  items: [],
}

/** RFC 9457 ProblemDetails body used to drive a per-block 500 (D12, T9 #7). */
export const problemDetails: ProblemDetails = {
  type: 'https://tools.ietf.org/html/rfc9110#section-15.6.1',
  title: 'Внутренняя ошибка сервера',
  status: 500,
  detail: 'Что-то пошло не так.',
}
