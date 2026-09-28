/** Donut palette, keyed off the accent (--chart-1). Shared by the donut and the table legend dots. */
export const CATEGORY_COLORS = [
  'var(--chart-1)',
  'var(--chart-2)',
  'var(--chart-3)',
  'var(--chart-4)',
  'var(--chart-5)',
  'var(--chart-6)',
]

/** Colour for a category by its index in the (revenue-sorted) list. */
export function categoryColor(index: number): string {
  return CATEGORY_COLORS[index % CATEGORY_COLORS.length]
}
