import { afterEach, describe, expect, it, vi } from 'vitest'

import { parsePeriod, serializePeriod } from './url'

const NOW = new Date('2026-09-24T12:00:00Z')

afterEach(() => {
  vi.useRealTimers()
})

describe('parsePeriod', () => {
  it('resolves a valid preset from the query string', () => {
    expect(parsePeriod('?period=7d', NOW)).toEqual({
      preset: '7d',
      from: '2026-09-18',
      to: '2026-09-24',
    })
  })

  it('reads a valid custom range verbatim', () => {
    expect(parsePeriod('?from=2026-01-01&to=2026-01-31', NOW)).toEqual({
      preset: 'custom',
      from: '2026-01-01',
      to: '2026-01-31',
    })
  })

  it('falls back to the default preset for garbage and invalid ranges', () => {
    const fallback = { preset: '30d', from: '2026-08-26', to: '2026-09-24' }

    expect(parsePeriod('', NOW)).toEqual(fallback)
    expect(parsePeriod('?period=nonsense', NOW)).toEqual(fallback)
    expect(parsePeriod('?from=2026-02-31&to=2026-03-01', NOW)).toEqual(fallback) // not a real day
    expect(parsePeriod('?from=2026-03-01&to=2026-01-01', NOW)).toEqual(fallback) // from > to
    expect(parsePeriod('?from=2020-01-01&to=2026-01-01', NOW)).toEqual(fallback) // > 731 days
    expect(parsePeriod('?from=2026-01-01', NOW)).toEqual(fallback) // missing `to`
  })
})

describe('serializePeriod', () => {
  it('serializes a preset as ?period', () => {
    expect(serializePeriod({ preset: '7d', from: '2026-09-18', to: '2026-09-24' })).toBe(
      '?period=7d',
    )
  })

  it('serializes a custom range as ?from&to', () => {
    expect(
      serializePeriod({ preset: 'custom', from: '2026-01-01', to: '2026-01-31' }),
    ).toBe('?from=2026-01-01&to=2026-01-31')
  })

  it('round-trips a custom range through parse', () => {
    const period = { preset: 'custom' as const, from: '2026-05-10', to: '2026-06-10' }
    expect(parsePeriod(serializePeriod(period), NOW)).toEqual(period)
  })
})
