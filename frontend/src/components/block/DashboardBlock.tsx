import type { ReactNode } from 'react'
import { Loader2, RotateCw } from 'lucide-react'
import { AnimatePresence, motion } from 'motion/react'

import type { ApiError } from '@/api/error'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { fade } from '@/lib/motion'
import { cn } from '@/lib/utils'

/** Query state a dashboard block can be in, mapped from a TanStack Query result by each feature. */
export type BlockStatus = 'pending' | 'error' | 'ready'

interface DashboardBlockProps {
  title: string
  className?: string
  /** `pending` = first load, no data yet; `error` = current request failed; `ready` = has data. */
  status: BlockStatus
  /** Background refetch (e.g. period change): dim the body and show a header spinner. */
  isFetching?: boolean
  /** Data is present but the block has nothing to show for this period. */
  isEmpty?: boolean
  error?: ApiError | null
  onRetry: () => void
  /** Block-shaped skeleton, matching the ready height so first load causes no layout shift. */
  skeleton: ReactNode
  /** Block-specific "no data" message (D6 — a real message, not a blank space). */
  empty: ReactNode
  /** Controls shown on the right of the header (mode toggles). Hidden while pending/error. */
  headerRight?: ReactNode
  children: ReactNode
}

/**
 * Single place for the four UX states of every dashboard block (T8): first-load skeleton,
 * period-change (old data dimmed + a header spinner, no skeleton flash), per-block error with a
 * retry, and empty. One block's error never affects the others (D9).
 */
export function DashboardBlock({
  title,
  className,
  status,
  isFetching = false,
  isEmpty = false,
  error,
  onRetry,
  skeleton,
  empty,
  headerRight,
  children,
}: DashboardBlockProps) {
  const showSpinner = isFetching && status === 'ready'
  const showControls = status === 'ready' && headerRight

  return (
    <Card className={cn('flex flex-col gap-4 py-4', className)}>
      <CardHeader className="flex flex-row items-center justify-between gap-2 space-y-0">
        <CardTitle className="flex items-center gap-2 text-base">
          {title}
          {showSpinner ? (
            <Loader2 className="size-4 animate-spin text-muted-foreground" aria-label="Обновление" />
          ) : null}
        </CardTitle>
        {showControls ? headerRight : null}
      </CardHeader>
      <CardContent className="flex-1">
        {/* Short fade between the four states; skeleton height matches content, so no layout shift. */}
        <AnimatePresence mode="wait" initial={false}>
          <motion.div key={status} variants={fade} initial="hidden" animate="show" exit="hidden">
            {status === 'pending' ? (
              skeleton
            ) : status === 'error' ? (
              <BlockError error={error} onRetry={onRetry} />
            ) : isEmpty ? (
              <div className="flex min-h-24 items-center justify-center text-center text-sm text-muted-foreground">
                {empty}
              </div>
            ) : (
              <div
                className={cn(
                  'transition-opacity',
                  isFetching && 'pointer-events-none opacity-60',
                )}
              >
                {children}
              </div>
            )}
          </motion.div>
        </AnimatePresence>
      </CardContent>
    </Card>
  )
}

function BlockError({ error, onRetry }: { error?: ApiError | null; onRetry: () => void }) {
  return (
    <div className="flex min-h-24 flex-col items-center justify-center gap-3 text-center">
      <p className="text-sm text-destructive">{error?.title ?? 'Не удалось загрузить блок'}</p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        <RotateCw className="size-4" />
        Повторить
      </Button>
    </div>
  )
}
