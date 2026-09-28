import createClient from 'openapi-fetch'

import type { paths } from './schema'

/**
 * Typed API client (OPENAPI). Types come from the committed schema.d.ts, so there are no
 * hand-written response types. The schema paths already carry the /api prefix
 * (e.g. /api/dashboard/kpis), so the base is just the page origin — prepending /api here would
 * double it into /api/api/... and 404. nginx proxies /api in Docker, Vite in dev, jsdom serves
 * it in tests. Using the absolute origin also keeps node's fetch happy (unlike the browser it
 * cannot resolve a relative URL).
 */
export const api = createClient<paths>({
  baseUrl: window.location.origin,
})
