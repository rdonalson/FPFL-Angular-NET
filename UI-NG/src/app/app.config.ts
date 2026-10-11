import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';

import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { AuthService } from './core/auth/auth';
import { authInterceptor } from './core/http/auth-interceptor';
import { errorInterceptor } from './core/http/error-interceptor';
import { loadingInterceptor } from './core/http/loading-interceptor';

/**
 * Application-wide providers. Replaces the old AppModule / CoreModule / SharedModule.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // Order matters: errorInterceptor sits outside authInterceptor so the auth retry
    // sees the raw 401 and everything after it is normalized to ApiError.
    provideHttpClient(withFetch(), withInterceptors([loadingInterceptor, errorInterceptor, authInterceptor])),
    MessageService,
    providePrimeNG({
      ripple: true,
      license: environment.primeuiLicense || undefined,
      theme: {
        preset: Aura,
        // Light theme only for now; add the class to <html> later to enable dark mode.
        options: { darkModeSelector: '.app-dark' },
      },
    }),
    // Renew a stored session that is about to expire (doesn't block startup).
    provideAppInitializer(() => inject(AuthService).restore()),
  ],
};
