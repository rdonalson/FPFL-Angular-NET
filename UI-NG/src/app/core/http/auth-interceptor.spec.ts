import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';

import { appTestProviders, authResponse, envelope } from '../../../testing/test-providers';
import { ApiError, apiUrl } from '../api/api-response';
import { AuthService } from '../auth/auth';

describe('authInterceptor', () => {
  let client: HttpClient;
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: appTestProviders() });
    client = TestBed.inject(HttpClient);
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);

    auth.login('rick@example.com', 'secret-pw').subscribe();
    http.expectOne(apiUrl('/auth/login')).flush(envelope(authResponse()));
  });

  afterEach(() => http.verify());

  it('adds the bearer token to API requests', () => {
    client.get(apiUrl('/item-types')).subscribe();

    const req = http.expectOne(apiUrl('/item-types'));
    expect(req.request.headers.get('Authorization')).toBe('Bearer access-1');
    req.flush(envelope([]));
  });

  it('never sends the token to other hosts', () => {
    client.get('https://example.com/data').subscribe();

    const req = http.expectOne('https://example.com/data');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('refreshes once on 401 and retries with the new token', () => {
    let body: unknown;
    client.get(apiUrl('/item-types')).subscribe((b) => (body = b));

    http
      .expectOne(apiUrl('/item-types'))
      .flush(envelope(null, 401, 'expired'), { status: 401, statusText: 'Unauthorized' });
    http
      .expectOne(apiUrl('/auth/refresh'))
      .flush(envelope(authResponse({ accessToken: 'access-2', refreshToken: 'refresh-2' })));

    const retry = http.expectOne(apiUrl('/item-types'));
    expect(retry.request.headers.get('Authorization')).toBe('Bearer access-2');
    retry.flush(envelope([{ id: 1, name: 'Credit' }]));

    expect(body).toBeTruthy();
    expect(auth.accessToken()).toBe('access-2');
  });

  it('ends the session when the refresh fails', () => {
    let error: unknown;
    client.get(apiUrl('/item-types')).subscribe({ error: (e: unknown) => (error = e) });

    http
      .expectOne(apiUrl('/item-types'))
      .flush(envelope(null, 401, 'expired'), { status: 401, statusText: 'Unauthorized' });
    http
      .expectOne(apiUrl('/auth/refresh'))
      .flush(envelope(null, 401, 'Refresh token expired'), { status: 401, statusText: 'Unauthorized' });

    expect(error).toBeInstanceOf(ApiError);
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.endedReason()).toBe('expired');
  });
});
