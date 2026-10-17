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

  it('drops sections that end up empty', () => {
    expect(filterMenuByRoles([{ label: 'Empty', items: [] }], [])).toEqual([]);
  });
});
