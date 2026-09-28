import createClient from 'openapi-fetch'

import type { paths } from './schema'

/**
 * Typed API client (OPENAPI). Types come from the committed schema.d.ts, so there are no
 * hand-written response types. Base path is /api — nginx proxies it in Docker, Vite in dev.
 * The base is overridable via VITE_API_BASE_URL so tests can point it at an absolute origin
 * (node's fetch, unlike the browser, cannot resolve a relative URL).
 */
export const api = createClient<paths>({
  baseUrl: import.meta.env.VITE_API_BASE_URL ?? '/api',
})
