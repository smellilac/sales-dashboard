/**
 * Avatar helpers. Initials and colour are not stored (MODEL): initials come from the name,
 * the colour is a deterministic hash of the manager id so the same person is always the same
 * colour, computed on the frontend.
 */

/** First letters of the first and last name, upper-cased, e.g. «Иван Петров» → «ИП». */
export function initials(firstName: string, lastName: string): string {
  const a = firstName.trim().charAt(0)
  const b = lastName.trim().charAt(0)
  return `${a}${b}`.toUpperCase()
}

/** A stable hue (0–359) from an id, so an avatar keeps its colour across periods and renders. */
export function hueFromId(id: string): number {
  let hash = 0
  for (let i = 0; i < id.length; i++) {
    hash = (hash * 31 + id.charCodeAt(i)) | 0
  }
  return Math.abs(hash) % 360
}

/** Background and foreground colours for an avatar, derived from the id hue. */
export function avatarColors(id: string): { background: string; foreground: string } {
  const hue = hueFromId(id)
  return {
    background: `oklch(0.92 0.05 ${hue})`,
    foreground: `oklch(0.4 0.12 ${hue})`,
  }
}
