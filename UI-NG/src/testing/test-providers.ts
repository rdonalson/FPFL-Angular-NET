import { EnvironmentProviders, Provider } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { MessageService } from 'primeng/api';

import { ApiResponse } from '../app/core/api/api-response';
import { AuthResponseDto } from '../app/core/auth/auth.models';
import { authInterceptor } from '../app/core/http/auth-interceptor';
import { errorInterceptor } from '../app/core/http/error-interceptor';

/** Router, HttpClient (with the real interceptors) backed by HttpTestingController, and toasts. */
export function appTestProviders(): (Provider | EnvironmentProviders)[] {
  return [
    provideRouter([]),
    provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])),
    provideHttpClientTesting(),
    MessageService,
  ];
}

export function envelope<T>(data: T, status = 200, message = 'OK', correlationId = ''): ApiResponse<T> {
  return { status, message, data, correlationId, timestamp: new Date().toISOString() };
}

export function authResponse(overrides: Partial<AuthResponseDto> = {}): AuthResponseDto {
  return {
    accessToken: 'access-1',
    accessTokenExpiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
    refreshToken: 'refresh-1',
    id: 7,
    email: 'rick@example.com',
    userId: '8fdbe29e-f25f-450d-b179-92973e2bf7ba',
    first: 'Rick',
    last: 'Donalson',
    roles: ['ROLE_USER'],
    isGuest: false,
    ...overrides,
  };
}
