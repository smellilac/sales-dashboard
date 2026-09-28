import { http, HttpResponse } from 'msw'

import {
  categoriesResponse,
  kpiResponse,
  rankingByAverageCheckResponse,
  rankingResponse,
  recentSalesPage1,
  recentSalesPage2,
  timeseriesResponse,
  topProductsResponse,
} from './fixtures'

/** A request the app made, captured so tests can assert on the query string (from/to, rankBy). */
export interface RecordedRequest {
  /** Endpoint path without the query string, e.g. `/api/dashboard/kpis`. */
  path: string
  /** Parsed query parameters of the request. */
  params: URLSearchParams
}

/** Every intercepted request, in order. Reset between tests via `resetRequests()`. */
export const requests: RecordedRequest[] = []

export function resetRequests(): void {
  requests.length = 0
}

/**
 * The most recent request whose path ends with `path`, or undefined if none was made. The client
 * bases requests on the page origin, so the recorded pathname is exactly the schema endpoint
 * (`/api/dashboard/...`); suffix matching lets tests pass that clean endpoint directly.
 */
export function lastRequest(path: string): RecordedRequest | undefined {
  return requests.findLast((r) => r.path.endsWith(path))
}

function record(url: string): URLSearchParams {
  const parsed = new URL(url)
  requests.push({ path: parsed.pathname, params: parsed.searchParams })
  return parsed.searchParams
}

/**
 * Default happy-path handlers for every dashboard endpoint (T9). Each pattern is the exact schema
 * path — no wildcard — so a client that mangles the path (e.g. a doubled /api/api prefix) misses
 * every handler and, with `onUnhandledRequest: 'error'`, fails the test instead of passing on a
 * lenient match. Tests override individual endpoints with `server.use(...)` for delays, errors,
 * and empty periods.
 */
export const handlers = [
  http.get('/api/dashboard/kpis', ({ request }) => {
    record(request.url)
    return HttpResponse.json(kpiResponse)
  }),

  http.get('/api/dashboard/managers/ranking', ({ request }) => {
    const params = record(request.url)
    const body =
      params.get('rankBy') === 'AverageCheck' ? rankingByAverageCheckResponse : rankingResponse
    return HttpResponse.json(body)
  }),

  http.get('/api/dashboard/timeseries', ({ request }) => {
    record(request.url)
    return HttpResponse.json(timeseriesResponse)
  }),

  http.get('/api/dashboard/categories', ({ request }) => {
    record(request.url)
    return HttpResponse.json(categoriesResponse)
  }),

  http.get('/api/dashboard/products/top', ({ request }) => {
    record(request.url)
    return HttpResponse.json(topProductsResponse)
  }),

  http.get('/api/dashboard/sales/recent', ({ request }) => {
    const params = record(request.url)
    const body = params.get('cursor') === 'cursor-page-2' ? recentSalesPage2 : recentSalesPage1
    return HttpResponse.json(body)
  }),
]
