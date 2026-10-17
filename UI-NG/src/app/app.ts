import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MessageModule } from 'primeng/message';
import { ProgressBarModule } from 'primeng/progressbar';
import { ToastModule } from 'primeng/toast';

import { AuthService } from './core/auth/auth';
import { AuthUi } from './core/auth/auth-ui';
import { ChangePasswordDialog } from './core/auth/ui/change-password-dialog/change-password-dialog';
import { LoginDialog } from './core/auth/ui/login-dialog/login-dialog';
import { RegisterDialog } from './core/auth/ui/register-dialog/register-dialog';
import { SessionExpiringDialog } from './core/auth/ui/session-expiring-dialog/session-expiring-dialog';
import { LoadingService } from './core/http/loading';
import { Footer } from './core/layout/footer/footer';
import { Sidebar } from './core/layout/sidebar/sidebar';
import { Topbar } from './core/layout/topbar/topbar';

/**
 * Application shell: progress bar, top bar, routed page, footer, toasts and the auth dialogs.
 */
@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    Topbar,
    Sidebar,
    Footer,
    ToastModule,
    ProgressBarModule,
    MessageModule,
    LoginDialog,
    RegisterDialog,
    ChangePasswordDialog,
    SessionExpiringDialog,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly ui = inject(AuthUi);
  protected readonly loading = inject(LoadingService);

  protected loginAgain(): void {
    this.auth.endedReason.set(null);
    this.ui.openLogin();
  }
}
