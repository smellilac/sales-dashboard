import type { components } from './schema'

type ProblemDetails = components['schemas']['ProblemDetails']

/**
 * Normalized API error surfaced by every query hook. Fields come from the backend's
 * ProblemDetails (D12); `code` is our domain error code when present. Dashboard blocks (T8)
 * render a single error state from this shape.
 */
export interface ApiError {
  status: number
  title: string
  code?: string
}

/**
 * Every query hook throws ApiError, so register it as the default error type — `query.error` is
 * then typed ApiError everywhere without repeating a generic on each hook.
 */
declare module '@tanstack/react-query' {
  interface Register {
    defaultError: ApiError
  }
}

/** A ProblemDetails body may carry a domain `code` (e.g. period.invalid) beyond the RFC fields. */
type ProblemBody = ProblemDetails & { code?: string }

/** Builds an ApiError from an openapi-fetch error body and HTTP response. */
export function toApiError(error: unknown, response: Response): ApiError {
  const body = (error ?? {}) as ProblemBody
  return {
    status: Number(body.status ?? response.status),
    title: body.title ?? response.statusText ?? 'Ошибка запроса',
    code: body.code,
  }
}
