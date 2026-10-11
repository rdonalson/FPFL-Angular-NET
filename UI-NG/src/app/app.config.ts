import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';

import { routes } from './app.routes';

/**
 * Application-wide providers. Replaces the old AppModule / CoreModule / SharedModule.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    providePrimeNG({
      ripple: true,
      theme: {
        preset: Aura,
        // Light theme only for now; add the class to <html> later to enable dark mode.
        options: { darkModeSelector: '.app-dark' },
      },
    }),
  ],
};
