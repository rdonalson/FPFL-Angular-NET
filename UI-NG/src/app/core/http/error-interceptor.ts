import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { ApiError, ApiResponse } from '../api/api-response';

/**
 * Turns every failed request into an ApiError carrying the API's message and correlation ID.
 * Replaces the old GlobalErrorHandlerService/ErrorIntercept pair (one was misused as an
 * ErrorHandler, the other was commented out).
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(catchError((error: unknown) => throwError(() => toApiError(error, req.url))));

function toApiError(error: unknown, url: string): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return new ApiError(0, 'Cannot reach the server. Check that the API is running.', '', url);
    }

    const body = error.error as Partial<ApiResponse<unknown>> | null;
    const message =
      typeof body?.message === 'string' && body.message
        ? body.message
        : `Request failed (${error.status} ${error.statusText})`;
    const correlationId = typeof body?.correlationId === 'string' ? body.correlationId : '';

    if (error.status >= 500) {
      console.error('[API ERROR]', { url, status: error.status, correlationId, message });
    }

    return new ApiError(error.status, message, correlationId, url);
  }

  return new ApiError(0, error instanceof Error ? error.message : 'Unexpected error', '', url);
}
