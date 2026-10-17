import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { DrawerModule } from 'primeng/drawer';

import { AuthService } from '../../auth/auth';
import { Layout } from '../layout';
import { MENU, filterMenuByRoles } from './menu';

/**
 * Slide-out navigation (UI-TSX's AppSidebar + AppMenu), opened from the top bar's menu button.
 * Only rendered for signed-in users; admin-only sections are filtered out by role.
 */
@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, DrawerModule],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Sidebar {
  protected readonly auth = inject(AuthService);
  protected readonly layout = inject(Layout);

  protected readonly sections = computed(() => filterMenuByRoles(MENU, this.auth.session()?.roles ?? []));
  protected readonly visible = computed(() => this.layout.menuOpen() && this.auth.isAuthenticated());

  protected visibleChange(visible: boolean): void {
    if (!visible) {
      this.layout.closeMenu();
    }
  }
}
