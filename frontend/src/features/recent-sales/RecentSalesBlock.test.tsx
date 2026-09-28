import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'

import type { ActivePeriod } from '@/period/types'
import { lastRequest } from '@/test/handlers'
import { renderWithProviders } from '@/test/renderWithProviders'

import { RecentSalesBlock } from './RecentSalesBlock'

const PERIOD: ActivePeriod = { preset: '30d', from: '2026-08-26', to: '2026-09-24' }
const RECENT_PATH = '/api/dashboard/sales/recent'

describe('RecentSalesBlock — pagination (T9 #10)', () => {
  it('«Показать ещё» loads the next page and disappears when nextCursor is null', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RecentSalesBlock period={PERIOD} />)

    // First page: two rows and a "load more" button (page 1 carries a cursor).
    await waitFor(() => expect(screen.getByText('ООО Ромашка')).toBeInTheDocument())
    expect(screen.getByText('АО Василёк')).toBeInTheDocument()
    const loadMore = screen.getByRole('button', { name: 'Показать ещё' })

    await user.click(loadMore)

    // Next page requested with the opaque cursor, its row appended.
    await waitFor(() => expect(screen.getByText('ЗАО Незабудка')).toBeInTheDocument())
    expect(lastRequest(RECENT_PATH)?.params.get('cursor')).toBe('cursor-page-2')

    // Page 2 has nextCursor = null → the button is gone; earlier rows remain.
    expect(screen.queryByRole('button', { name: 'Показать ещё' })).not.toBeInTheDocument()
    expect(screen.getByText('ООО Ромашка')).toBeInTheDocument()
  })
})
