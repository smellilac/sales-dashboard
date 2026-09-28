import { avatarColors, initials } from '@/lib/avatar'
import { cn } from '@/lib/utils'

interface AvatarProps {
  id: string
  firstName: string
  lastName: string
  className?: string
}

/** Round initials badge, coloured by a stable hash of the manager id (MODEL). No image. */
export function Avatar({ id, firstName, lastName, className }: AvatarProps) {
  const { background, foreground } = avatarColors(id)
  return (
    <span
      className={cn(
        'inline-flex size-8 shrink-0 items-center justify-center rounded-full text-xs font-semibold',
        className,
      )}
      style={{ backgroundColor: background, color: foreground }}
      aria-hidden
    >
      {initials(firstName, lastName)}
    </span>
  )
}
