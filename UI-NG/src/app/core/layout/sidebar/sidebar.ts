import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { DrawerModule } from 'primeng/drawer';

import { AuthService } from '../../auth/auth';
import { Layout } from '../layout';
import { MENU, filterMenuByRoles } from './menu';

/**
 * Main navigation (UI-TSX's AppSidebar + AppMenu): a fixed side panel on desktop, and a
 * slide-out drawer opened from the top bar's menu button below 992px.
 * Only rendered for signed-in users; admin-only sections are filtered out by role.
 */
@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, NgTemplateOutlet, DrawerModule],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Sidebar {
  protected readonly auth = inject(AuthService);
  protected readonly layout = inject(Layout);

  protected readonly sections = computed(() =>
    filterMenuByRoles(MENU, this.auth.session()?.roles ?? []),
  );
  protected readonly visible = computed(
    () => this.layout.menuOpen() && this.auth.isAuthenticated(),
  );

  /** "Rick Donalson" -> "RD"; falls back to the first letter of the email. */
  protected readonly initials = computed(() => {
    const name = this.auth.displayName().trim();
    const parts = name.split(/\s+/).filter((p) => p && !p.includes('@'));
    const letters = parts.length ? parts.slice(0, 2).map((p) => p[0]) : [name[0] ?? '?'];
    return letters.join('').toUpperCase();
  });

  protected visibleChange(visible: boolean): void {
    if (!visible) {
      this.layout.closeMenu();
    }
  }
}
