import '@testing-library/jest-dom/vitest'

import { afterAll, afterEach } from 'vitest'
import { MotionGlobalConfig } from 'motion/react'

import { resetRequests } from './handlers'
import { server } from './server'

// The API client derives its base from window.location.origin (jsdom serves an absolute origin,
// so node's fetch can resolve it) and the schema paths carry the /api prefix — no env override.
// Start MSW here at module top-level — before any test file imports the API client. openapi-fetch
// captures globalThis.fetch when the client is created, so the interceptor must already be in place.
server.listen({ onUnhandledRequest: 'error' })

// Animations off in tests (T9): assertions run against final DOM, no fake-timer juggling for tweens.
MotionGlobalConfig.skipAnimations = true

// Recharts' ResponsiveContainer needs a sized box and a ResizeObserver, neither of which jsdom
// provides. Stub both so charts render their series into the DOM at a fixed size.
class ResizeObserverStub {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}
globalThis.ResizeObserver = ResizeObserverStub as unknown as typeof ResizeObserver

const CONTAINER_WIDTH = 800
const CONTAINER_HEIGHT = 300

Object.defineProperty(HTMLElement.prototype, 'offsetWidth', {
  configurable: true,
  get() {
    return CONTAINER_WIDTH
  },
})
Object.defineProperty(HTMLElement.prototype, 'offsetHeight', {
  configurable: true,
  get() {
    return CONTAINER_HEIGHT
  },
})
Object.defineProperty(HTMLElement.prototype, 'getBoundingClientRect', {
  configurable: true,
  value() {
    return {
      width: CONTAINER_WIDTH,
      height: CONTAINER_HEIGHT,
      top: 0,
      left: 0,
      right: CONTAINER_WIDTH,
      bottom: CONTAINER_HEIGHT,
      x: 0,
      y: 0,
      toJSON() {},
    }
  },
})

// Clear per-test handler overrides and the recorded-request log after every test; tear down at end.
afterEach(() => {
  server.resetHandlers()
  resetRequests()
})
afterAll(() => server.close())
