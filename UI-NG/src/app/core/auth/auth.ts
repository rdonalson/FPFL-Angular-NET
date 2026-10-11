import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, map, shareReplay, tap, throwError } from 'rxjs';

import { ApiError, ApiResponse, apiUrl } from '../api/api-response';
import {
  ADMIN_ROLE,
  AuthResponseDto,
  RegisterRequest,
  Session,
  toSession,
} from './auth.models';

const STORAGE_KEY = 'fpfl.session';

/**
 * Session state as signals, plus the calls to /auth/*.
 * Replaces the old MSAL + LoginUtilService + NgRx "claims" slice.
 * The session lives in sessionStorage (per tab), like UI-TSX.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly state = signal<Session | null>(readStoredSession());
  private refreshInFlight: Observable<Session> | null = null;

  readonly session = this.state.asReadonly();
  readonly isAuthenticated = computed(() => this.state() !== null);
  readonly isGuest = computed(() => this.state()?.isGuest ?? false);
  readonly isAdmin = computed(() => this.state()?.roles.includes(ADMIN_ROLE) ?? false);
  readonly accessToken = computed(() => this.state()?.accessToken ?? null);
  readonly displayName = computed(() => {
    const session = this.state();
    if (!session) {
      return '';
    }
    return [session.first, session.last].filter(Boolean).join(' ') || session.email;
  });

  /** Set when the session ended on its own, so the shell can tell the user why. */
  readonly endedReason = signal<'expired' | null>(null);

  login(email: string, password: string): Observable<Session> {
    return this.startSession(this.http.post<ApiResponse<AuthResponseDto>>(apiUrl('/auth/login'), { email, password }));
  }

  /** Guest (demo) login. The guest password stays on the server. */
  loginAsGuest(): Observable<Session> {
    return this.startSession(this.http.post<ApiResponse<AuthResponseDto>>(apiUrl('/auth/guest'), {}));
  }

  register(request: RegisterRequest): Observable<Session> {
    return this.startSession(this.http.post<ApiResponse<AuthResponseDto>>(apiUrl('/auth/register'), request));
  }

  changePassword(currentPassword: string, newPassword: string): Observable<Session> {
    return this.startSession(
      this.http.post<ApiResponse<AuthResponseDto>>(apiUrl('/auth/change-password'), {
        currentPassword,
        newPassword,
      }),
    );
  }

  canRefresh(): boolean {
    return !!this.state()?.refreshToken;
  }

  /** Swaps the refresh token for a new session. Concurrent callers share one request. */
  refresh(): Observable<Session> {
    const refreshToken = this.state()?.refreshToken;
    if (!refreshToken) {
      return throwError(() => new ApiError(401, 'No active session'));
    }

    this.refreshInFlight ??= this.startSession(
      this.http.post<ApiResponse<AuthResponseDto>>(apiUrl('/auth/refresh'), { refreshToken }),
    ).pipe(
      finalize(() => (this.refreshInFlight = null)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    return this.refreshInFlight;
  }

  /** On app start: renew a stored session whose access token is about to expire (or has). */
  restore(): void {
    const session = this.state();
    if (session && session.accessTokenExpiresAt - Date.now() < 60_000) {
      this.refresh().subscribe({ error: () => this.clear() });
    }
  }

  /** User-initiated logout: revoke the refresh token on the server (best effort) and go home. */
  logout(): void {
    const refreshToken = this.state()?.refreshToken;
    this.clear();
    this.endedReason.set(null);

    if (refreshToken) {
      this.http.post(apiUrl('/auth/logout'), { refreshToken }).subscribe({ error: () => undefined });
    }

    void this.router.navigateByUrl('/');
  }

  /** The session ended without the user asking (token expired and could not be refreshed). */
  endSession(reason: 'expired'): void {
    if (!this.state()) {
      return;
    }
    this.clear();
    this.endedReason.set(reason);
    void this.router.navigateByUrl('/');
  }

  private startSession(request: Observable<ApiResponse<AuthResponseDto>>): Observable<Session> {
    return request.pipe(
      map((response) => toSession(response.data)),
      tap((session) => this.store(session)),
    );
  }

  private store(session: Session): void {
    this.state.set(session);
    this.endedReason.set(null);
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Storage unavailable (private mode); the session still works for this page view.
    }
  }

  private clear(): void {
    this.state.set(null);
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // ignore
    }
  }
}

function readStoredSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const session = JSON.parse(raw) as Session;
    return session.accessToken && session.refreshToken ? session : null;
  } catch {
    return null;
  }
}
