import { afterEach, describe, expect, it, vi } from 'vitest'

import { resolvePreset } from './presets'

// The whole suite runs under TZ=America/New_York (see package.json "test"), so every passing
// assertion below also proves the result does not depend on the browser's time zone (D4).

afterEach(() => {
  vi.useRealTimers()
})

function pin(iso: string) {
  vi.useFakeTimers()
  vi.setSystemTime(new Date(iso))
}

describe('resolvePreset', () => {
  it('resolves every preset against a mid-day instant in the business zone', () => {
    // 2026-09-24 12:00 UTC = 15:00 Moscow → business day is 2026-09-24.
    pin('2026-09-24T12:00:00Z')

    expect(resolvePreset('today')).toEqual({ from: '2026-09-24', to: '2026-09-24' })
    expect(resolvePreset('7d')).toEqual({ from: '2026-09-18', to: '2026-09-24' })
    expect(resolvePreset('30d')).toEqual({ from: '2026-08-26', to: '2026-09-24' })
    expect(resolvePreset('thisMonth')).toEqual({ from: '2026-09-01', to: '2026-09-24' })
    expect(resolvePreset('lastMonth')).toEqual({ from: '2026-08-01', to: '2026-08-31' })
  })

  it('uses the Moscow day when the browser (New York) is still on the previous day', () => {
    // 03:30 UTC = 23:30 New York (Sep 24) but 06:30 Moscow (Sep 25). Business day is the 25th.
    pin('2026-09-25T03:30:00Z')

    expect(resolvePreset('today')).toEqual({ from: '2026-09-25', to: '2026-09-25' })
  })

  it('rolls last month back across a year boundary in January', () => {
    pin('2026-01-15T12:00:00Z')

    expect(resolvePreset('lastMonth')).toEqual({ from: '2025-12-01', to: '2025-12-31' })
    expect(resolvePreset('thisMonth')).toEqual({ from: '2026-01-01', to: '2026-01-15' })
  })
})
