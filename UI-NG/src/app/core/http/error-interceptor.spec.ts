import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';

import { appTestProviders, envelope } from '../../../testing/test-providers';
import { ApiError, apiUrl } from '../api/api-response';

describe('errorInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: appTestProviders() });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('turns an ApiResponse error into an ApiError with message and correlation id', () => {
    let error: ApiError | undefined;
    client.get(apiUrl('/item-types/999')).subscribe({ error: (e: ApiError) => (error = e) });

    http
      .expectOne(apiUrl('/item-types/999'))
      .flush(envelope(null, 404, 'ItemType 999 not found', 'abc-123'), { status: 404, statusText: 'Not Found' });

    expect(error).toBeInstanceOf(ApiError);
    expect(error?.status).toBe(404);
    expect(error?.message).toBe('ItemType 999 not found');
    expect(error?.correlationId).toBe('abc-123');
  });

  it('explains when the server cannot be reached', () => {
    let error: ApiError | undefined;
    client.get(apiUrl('/item-types')).subscribe({ error: (e: ApiError) => (error = e) });

    http.expectOne(apiUrl('/item-types')).error(new ProgressEvent('error'));

    expect(error?.status).toBe(0);
    expect(error?.message).toContain('Cannot reach the server');
  });
});
