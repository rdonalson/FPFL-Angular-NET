import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { appTestProviders, authResponse, envelope } from '../../../testing/test-providers';
import { apiUrl } from '../api/api-response';
import { AuthService } from './auth';
import { Session } from './auth.models';

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: appTestProviders() });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function signIn(overrides = {}): void {
    auth.login('rick@example.com', 'secret-pw').subscribe();
    http.expectOne(apiUrl('/auth/login')).flush(envelope(authResponse(overrides)));
  }

  it('starts signed out', () => {
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.accessToken()).toBeNull();
  });

  it('login posts credentials and stores the session', () => {
    let session: Session | undefined;
    auth.login('rick@example.com', 'secret-pw').subscribe((s) => (session = s));

    const req = http.expectOne(apiUrl('/auth/login'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'rick@example.com', password: 'secret-pw' });
    req.flush(envelope(authResponse()));

    expect(session?.email).toBe('rick@example.com');
    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.displayName()).toBe('Rick Donalson');
    expect(auth.isGuest()).toBe(false);
    expect(sessionStorage.getItem('fpfl.session')).toContain('access-1');
  });

  it('guest login sends no credentials and flags the session as guest', () => {
    auth.loginAsGuest().subscribe();

    const req = http.expectOne(apiUrl('/auth/guest'));
    expect(req.request.body).toEqual({});
    req.flush(envelope(authResponse({ email: 'guest.user@gmail.com', first: 'Guest', last: 'User', isGuest: true })));

    expect(auth.isGuest()).toBe(true);
  });

  it('isAdmin follows ROLE_ADMIN', () => {
    signIn({ roles: ['ROLE_ADMIN', 'ROLE_USER'] });
    expect(auth.isAdmin()).toBe(true);
  });

  it('logout clears the session and revokes the refresh token', () => {
    signIn();

    auth.logout();

    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('fpfl.session')).toBeNull();
    const req = http.expectOne(apiUrl('/auth/logout'));
    expect(req.request.body).toEqual({ refreshToken: 'refresh-1' });
    req.flush(envelope(null));
  });

  it('concurrent refresh calls share one request', () => {
    signIn();
    let first: Session | undefined;
    let second: Session | undefined;

    auth.refresh().subscribe((s) => (first = s));
    auth.refresh().subscribe((s) => (second = s));

    const req = http.expectOne(apiUrl('/auth/refresh'));
    expect(req.request.body).toEqual({ refreshToken: 'refresh-1' });
    req.flush(envelope(authResponse({ accessToken: 'access-2', refreshToken: 'refresh-2' })));

    expect(first?.accessToken).toBe('access-2');
    expect(second?.accessToken).toBe('access-2');
    expect(auth.accessToken()).toBe('access-2');
  });

  it('endSession signs out and records why', () => {
    signIn();

    auth.endSession('expired');

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.endedReason()).toBe('expired');
  });
});
