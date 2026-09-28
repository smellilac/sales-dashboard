import { useCallback, useEffect, useState } from 'react'

import { resolvePreset } from './presets'
import type { ActivePeriod, PresetId } from './types'
import { parsePeriod, serializePeriod } from './url'

/**
 * Single source of truth for the selected period, backed by the URL — no router, no store (D3).
 * Reads the period from the query string, writes changes with the History API (so the browser
 * Back button restores the previous period), and re-reads on `popstate`.
 */
export function usePeriod() {
  const [period, setPeriod] = useState<ActivePeriod>(() =>
    parsePeriod(window.location.search),
  )

  useEffect(() => {
    const onPopState = () => setPeriod(parsePeriod(window.location.search))
    window.addEventListener('popstate', onPopState)
    return () => window.removeEventListener('popstate', onPopState)
  }, [])

  const navigate = useCallback((next: ActivePeriod) => {
    window.history.pushState(null, '', serializePeriod(next))
    setPeriod(next)
  }, [])

  const setPreset = useCallback(
    (preset: Exclude<PresetId, 'custom'>) => {
      navigate({ preset, ...resolvePreset(preset) })
    },
    [navigate],
  )

  const setCustomRange = useCallback(
    (from: string, to: string) => {
      navigate({ preset: 'custom', from, to })
    },
    [navigate],
  )

  return { period, setPreset, setCustomRange }
}
