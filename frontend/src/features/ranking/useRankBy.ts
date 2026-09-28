import { useCallback, useEffect, useState } from 'react'

import { DEFAULT_RANK_BY, parseRankBy, type RankByUrl } from './rankBy'

/**
 * The ranking metric, backed by the `?rankBy=` URL param (D14) — shareable, restored on Back.
 * Writes preserve the period params, and the default value is kept out of the URL.
 */
export function useRankBy() {
  const [rankBy, setState] = useState<RankByUrl>(() =>
    parseRankBy(new URLSearchParams(window.location.search).get('rankBy')),
  )

  useEffect(() => {
    const onPopState = () =>
      setState(parseRankBy(new URLSearchParams(window.location.search).get('rankBy')))
    window.addEventListener('popstate', onPopState)
    return () => window.removeEventListener('popstate', onPopState)
  }, [])

  const setRankBy = useCallback((next: RankByUrl) => {
    const params = new URLSearchParams(window.location.search)
    if (next === DEFAULT_RANK_BY) {
      params.delete('rankBy')
    } else {
      params.set('rankBy', next)
    }
    const qs = params.toString()
    window.history.pushState(null, '', qs ? `?${qs}` : window.location.pathname)
    setState(next)
  }, [])

  return { rankBy, setRankBy }
}
