import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth';
import { AuthUi } from './auth-ui';

/**
 * Pages that need a signed-in user. Otherwise: go home, open the login dialog,
 * and come back to the requested page after signing in.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  if (inject(AuthService).isAuthenticated()) {
    return true;
  }

  inject(AuthUi).openLogin(state.url);
  return inject(Router).createUrlTree(['/']);
};
