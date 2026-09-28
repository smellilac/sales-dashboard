import { format } from 'date-fns'
import { CalendarIcon } from 'lucide-react'
import { useState } from 'react'
import type { DateRange } from 'react-day-picker'

import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group'
import { formatDayRange } from '@/lib/format'
import { PRESET_LABELS, SELECTABLE_PRESETS } from '@/period/presets'
import type { ActivePeriod, PresetId } from '@/period/types'

interface PeriodPickerProps {
  period: ActivePeriod
  onPreset: (preset: Exclude<PresetId, 'custom'>) => void
  onCustomRange: (from: string, to: string) => void
}

/** Period control (D3): preset toggles plus a calendar popover for an arbitrary range. */
export function PeriodPicker({ period, onPreset, onCustomRange }: PeriodPickerProps) {
  const [open, setOpen] = useState(false)
  const [range, setRange] = useState<DateRange | undefined>(undefined)

  function handleSelect(next: DateRange | undefined) {
    setRange(next)
    if (next?.from && next.to) {
      onCustomRange(format(next.from, 'yyyy-MM-dd'), format(next.to, 'yyyy-MM-dd'))
      setOpen(false)
    }
  }

  return (
    <div className="flex items-center gap-2">
      <ToggleGroup
        type="single"
        value={period.preset === 'custom' ? '' : period.preset}
        onValueChange={(value) => {
          if (value) onPreset(value as Exclude<PresetId, 'custom'>)
        }}
        variant="outline"
      >
        {SELECTABLE_PRESETS.map((preset) => (
          <ToggleGroupItem key={preset} value={preset} aria-label={PRESET_LABELS[preset]}>
            {PRESET_LABELS[preset]}
          </ToggleGroupItem>
        ))}
      </ToggleGroup>

      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant={period.preset === 'custom' ? 'default' : 'outline'}
            className="gap-2"
          >
            <CalendarIcon className="size-4" />
            {period.preset === 'custom'
              ? formatDayRange(period.from, period.to)
              : PRESET_LABELS.custom}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="end">
          <Calendar
            mode="range"
            numberOfMonths={2}
            defaultMonth={new Date(`${period.to}T00:00:00`)}
            selected={range}
            onSelect={handleSelect}
            autoFocus
          />
        </PopoverContent>
      </Popover>
    </div>
  )
}
