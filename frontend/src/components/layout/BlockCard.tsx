import type { ReactNode } from 'react'

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'

interface BlockCardProps {
  title: string
  description?: string
  className?: string
  children?: ReactNode
}

/**
 * Placeholder for a dashboard block in its final grid position (T7). Renders a titled Card whose
 * body is a skeleton until the real block lands in T8. Pass `children` to show real content.
 */
export function BlockCard({ title, description, className, children }: BlockCardProps) {
  return (
    <Card className={cn('flex flex-col', className)}>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        {description ? <CardDescription>{description}</CardDescription> : null}
      </CardHeader>
      <CardContent className="flex-1">
        {children ?? <BlockSkeleton />}
      </CardContent>
    </Card>
  )
}

function BlockSkeleton() {
  return (
    <div className="space-y-3">
      <Skeleton className="h-8 w-2/3" />
      <Skeleton className="h-4 w-full" />
      <Skeleton className="h-4 w-5/6" />
      <Skeleton className="h-4 w-4/6" />
    </div>
  )
}
