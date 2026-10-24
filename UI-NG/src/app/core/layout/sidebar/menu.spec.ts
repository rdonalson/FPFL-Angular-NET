import { routes } from '../../../app.routes';
import { MENU, filterMenuByRoles } from './menu';

describe('filterMenuByRoles', () => {
  const labels = (roles: string[]) => filterMenuByRoles(MENU, roles).map((section) => section.label);

  it('shows only the Actions section to regular users and guests', () => {
    expect(labels(['ROLE_USER'])).toEqual(['Actions']);
    expect(labels([])).toEqual(['Actions']);
  });

  it('adds the Admin section for admins', () => {
    expect(labels(['ROLE_ADMIN', 'ROLE_USER'])).toEqual(['Actions', 'Admin']);
  });

  it('lists Docs for everyone and System Status for admins', () => {
    const links = (roles: string[]) =>
      filterMenuByRoles(MENU, roles).flatMap((section) => section.items.map((item) => item.label));
    expect(links(['ROLE_USER'])).toContain('Docs');
    expect(links(['ROLE_USER'])).not.toContain('System Status');
    expect(links(['ROLE_ADMIN'])).toContain('System Status');
  });

  it('every menu link has a route', () => {
    const paths = routes.map((route) => `/${route.path ?? ''}`);
    for (const link of MENU.flatMap((section) => section.items)) {
      expect(paths).toContain(link.to);
    }
  });

  it('drops sections that end up empty', () => {
    expect(filterMenuByRoles([{ label: 'Empty', items: [] }], [])).toEqual([]);
  });
});
