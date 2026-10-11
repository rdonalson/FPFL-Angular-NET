import { environment } from '../../../environments/environment';

/**
 * Envelope every API-NET endpoint returns (same shape as API-JDK and UI-TSX's ApiResponse).
 */
export interface ApiResponse<T> {
  status: number;
  message: string;
  data: T;
  correlationId: string;
  timestamp: string;
}

/**
 * Error thrown for every failed HTTP call (built by errorInterceptor).
 * `correlationId` matches the API's log entry for 4xx/5xx responses.
 */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly correlationId = '',
    readonly url = '',
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/** A human-readable message for any error value. */
export function errorMessage(error: unknown): string {
  if (error instanceof Error && error.message) {
    return error.message;
  }
  return 'Something went wrong. Please try again.';
}

/** Absolute URL for an API path, e.g. apiUrl('/item-types'). */
export function apiUrl(path: string): string {
  return `${environment.apiUrl}${path}`;
}

/** True when the URL points at API-NET (so tokens are never sent anywhere else). */
export function isApiUrl(url: string): boolean {
  return url.startsWith(environment.apiUrl);
}
