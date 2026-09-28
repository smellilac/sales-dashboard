/** The six period choices (D3). `custom` is an arbitrary from/to range. */
export type PresetId = 'today' | '7d' | '30d' | 'thisMonth' | 'lastMonth' | 'custom'

/** The default preset when the URL carries no valid period. */
export const DEFAULT_PRESET: Exclude<PresetId, 'custom'> = '30d'

/** Inclusive day bounds as `yyyy-MM-dd` strings — exactly what the API's from/to expect (D4). */
export interface Period {
  from: string
  to: string
}

/** The resolved period plus which preset produced it (`custom` for an explicit range). */
export interface ActivePeriod extends Period {
  preset: PresetId
}
