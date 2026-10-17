import { ComponentFixture, TestBed } from '@angular/core/testing';

import { appTestProviders, authResponse } from '../../../../testing/test-providers';
import { toSession } from '../../auth/auth.models';
import { Layout } from '../layout';
import { Sidebar } from './sidebar';

describe('Sidebar', () => {
  let fixture: ComponentFixture<Sidebar>;

  async function create(roles: string[] | null) {
    sessionStorage.clear();
    if (roles) {
      sessionStorage.setItem('fpfl.session', JSON.stringify(toSession(authResponse({ roles }))));
    }
    await TestBed.configureTestingModule({ imports: [Sidebar], providers: appTestProviders() }).compileComponents();
    fixture = TestBed.createComponent(Sidebar);
    TestBed.inject(Layout).menuOpen.set(true);
    await fixture.whenStable();
  }

  const text = () => document.body.textContent ?? '';

  it('lists the actions for a signed-in user, without the Admin section', async () => {
    await create(['ROLE_USER']);

    expect(text()).toContain('Initial Amount');
    expect(text()).toContain('Credits');
    expect(text()).not.toContain('Time Periods');
  });

  it('adds the Admin section for admins', async () => {
    await create(['ROLE_ADMIN', 'ROLE_USER']);

    expect(text()).toContain('Item Types');
    expect(text()).toContain('Time Periods');
  });

  it('stays closed for signed-out visitors', async () => {
    await create(null);

    expect(text()).not.toContain('Credits');
  });
});
