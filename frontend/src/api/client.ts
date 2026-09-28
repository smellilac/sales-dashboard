import createClient from 'openapi-fetch'

import type { paths } from './schema'

/**
 * Typed API client (OPENAPI). Types come from the committed schema.d.ts, so there are no
 * hand-written response types. Base path is /api — nginx proxies it in Docker, Vite in dev.
 */
export const api = createClient<paths>({ baseUrl: '/api' })
