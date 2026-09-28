import { setupServer } from 'msw/node'

import { handlers } from './handlers'

/** Node MSW server shared by all tests; lifecycle is wired in test/setup.ts. */
export const server = setupServer(...handlers)
