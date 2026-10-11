import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth';
import { AuthUi } from './auth-ui';

/** Pages for ROLE_ADMIN only. Signed-out users get the login dialog; others go home. */
export const adminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.isAdmin()) {
    return true;
  }

  if (!auth.isAuthenticated()) {
    inject(AuthUi).openLogin(state.url);
  }
  return inject(Router).createUrlTree(['/']);
};
