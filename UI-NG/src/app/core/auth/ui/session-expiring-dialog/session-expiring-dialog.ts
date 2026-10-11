import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { finalize } from 'rxjs';

import { AuthService } from '../../auth';
import { AuthUi } from '../../auth-ui';

/** Show the warning this long before the access token expires. */
const WARNING_MS = 2 * 60_000;

/**
 * Port of UI-TSX's useTokenWatcher + SessionExpireDialog: warns before the access token
 * expires, offers "Stay logged in" (refresh) or "Logout", and ends the session at expiry.
 * Timers are rescheduled automatically whenever the session changes.
 */
@Component({
  selector: 'app-session-expiring-dialog',
  imports: [DialogModule, ButtonModule],
  template: `
    <p-dialog
      header="Session expiring"
      [visible]="visible()"
      [modal]="true"
      [closable]="false"
      [draggable]="false"
      [style]="{ width: '24rem' }"
      [breakpoints]="{ '640px': '95vw' }"
    >
      <p>Your session is about to expire. Would you like to stay logged in?</p>
      <ng-template #footer>
        <p-button label="Logout" severity="danger" [text]="true" (onClick)="logout()" />
        <p-button label="Stay logged in" icon="pi pi-refresh" [loading]="refreshing()" (onClick)="stay()" />
      </ng-template>
    </p-dialog>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SessionExpiringDialog {
  private readonly auth = inject(AuthService);
  private readonly ui = inject(AuthUi);

  protected readonly visible = signal(false);
  protected readonly refreshing = signal(false);

  constructor() {
    effect((onCleanup) => {
      const session = this.auth.session();
      this.visible.set(false);
      if (!session) {
        return;
      }

      const untilExpiry = session.accessTokenExpiresAt - Date.now();
      const warn = setTimeout(() => this.visible.set(true), Math.max(0, untilExpiry - WARNING_MS));
      const expire = setTimeout(() => this.auth.endSession('expired'), Math.max(0, untilExpiry));

      onCleanup(() => {
        clearTimeout(warn);
        clearTimeout(expire);
      });
    });
  }

  protected stay(): void {
    this.refreshing.set(true);
    this.auth
      .refresh()
      .pipe(finalize(() => this.refreshing.set(false)))
      .subscribe({
        // The new session reschedules the timers and hides the dialog.
        error: () => this.auth.endSession('expired'),
      });
  }

  protected logout(): void {
    this.visible.set(false);
    this.ui.logout();
  }
}
