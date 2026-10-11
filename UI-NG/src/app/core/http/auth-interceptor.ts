import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';

import { isApiUrl } from '../api/api-response';
import { AuthService } from '../auth/auth';

/** Endpoints that must never carry a bearer token or trigger a refresh. */
const PUBLIC_AUTH_PATHS = ['/auth/login', '/auth/guest', '/auth/register', '/auth/refresh', '/auth/logout'];

/**
 * Adds "Authorization: Bearer <token>" to API-NET requests. On a 401 it refreshes the
 * session once (concurrent 401s share one refresh) and retries; if that fails, the
 * session ends.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApiUrl(req.url) || PUBLIC_AUTH_PATHS.some((path) => req.url.includes(path))) {
    return next(req);
  }

  const auth = inject(AuthService);
  const token = auth.accessToken();

  return next(token ? withToken(req, token) : req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || !auth.canRefresh()) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap((session) => next(withToken(req, session.accessToken))),
        catchError(() => {
          auth.endSession('expired');
          return throwError(() => error);
        }),
      );
    }),
  );
};

function withToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
