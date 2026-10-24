import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot, UrlTree } from '@angular/router';

import { appTestProviders, authResponse } from '../../../testing/test-providers';
import { adminGuard } from './admin-guard';
import { AuthUi } from './auth-ui';
import { toSession } from './auth.models';

describe('adminGuard', () => {
  const run: CanActivateFn = (...args) => TestBed.runInInjectionContext(() => adminGuard(...args));
  const state = { url: '/status' } as RouterStateSnapshot;
  const route = {} as ActivatedRouteSnapshot;

  function configure(roles?: string[]): void {
    sessionStorage.clear();
    if (roles) {
      sessionStorage.setItem('fpfl.session', JSON.stringify(toSession(authResponse({ roles }))));
    }
    TestBed.configureTestingModule({ providers: appTestProviders() });
  }

  const home = () => TestBed.inject(Router).serializeUrl(TestBed.inject(Router).createUrlTree(['/']));

  it('lets admins in', () => {
    configure(['ROLE_USER', 'ROLE_ADMIN']);
    expect(run(route, state)).toBe(true);
  });

  it('sends other signed-in users home without a login prompt', () => {
    configure(['ROLE_USER']);
    const openLogin = vi.spyOn(TestBed.inject(AuthUi), 'openLogin');

    const result = run(route, state) as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(result)).toBe(home());
    expect(openLogin).not.toHaveBeenCalled();
  });

  it('asks signed-out visitors to log in, then returns to the page', () => {
    configure();
    const openLogin = vi.spyOn(TestBed.inject(AuthUi), 'openLogin').mockImplementation(() => undefined);

    const result = run(route, state) as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(result)).toBe(home());
    expect(openLogin).toHaveBeenCalledWith('/status');
  });
});
