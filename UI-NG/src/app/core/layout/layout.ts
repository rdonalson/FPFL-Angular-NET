import { Injectable, signal } from '@angular/core';

/** App-shell state shared by the top bar (menu button) and the sidebar (drawer). */
@Injectable({ providedIn: 'root' })
export class Layout {
  readonly menuOpen = signal(false);

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }
}
