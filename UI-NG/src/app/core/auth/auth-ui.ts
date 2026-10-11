import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { finalize } from 'rxjs';

import { errorMessage } from '../api/api-response';
import { AuthService } from './auth';
import { Session } from './auth.models';

export type AuthDialog = 'login' | 'register' | 'change-password';

/**
 * UI-side auth flow shared by the top bar, home page, dialogs and guards:
 * which dialog is open, where to go after signing in, guest login and toasts.
 */
@Injectable({ providedIn: 'root' })
export class AuthUi {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);

  private returnUrl: string | null = null;

  readonly openDialog = signal<AuthDialog | null>(null);
  readonly guestLoading = signal(false);

  /** Opens the login dialog; after signing in the user lands on `returnUrl`. */
  openLogin(returnUrl?: string): void {
    this.returnUrl = returnUrl ?? null;
    this.openDialog.set('login');
  }

  openRegister(): void {
    this.openDialog.set('register');
  }

  openChangePassword(): void {
    this.openDialog.set('change-password');
  }

  close(): void {
    this.openDialog.set(null);
  }

  loginAsGuest(): void {
    this.guestLoading.set(true);
    this.auth
      .loginAsGuest()
      .pipe(finalize(() => this.guestLoading.set(false)))
      .subscribe({
        next: () => this.signedIn('Guest session started'),
        error: (error: unknown) => this.error('Guest login failed', error),
      });
  }

  logout(): void {
    this.auth.logout();
    this.messages.add({ severity: 'info', summary: 'Signed out', life: 2500 });
  }

  /** Call after any successful sign-in: closes dialogs, greets, and follows the return URL. */
  signedIn(detail?: string, session?: Session): void {
    this.close();
    const name = session?.first || this.auth.displayName();
    this.messages.add({
      severity: 'success',
      summary: 'Signed in',
      detail: detail ?? `Welcome, ${name}`,
      life: 2500,
    });

    const target = this.returnUrl;
    this.returnUrl = null;
    if (target) {
      void this.router.navigateByUrl(target);
    }
  }

  error(summary: string, error: unknown): void {
    this.messages.add({ severity: 'error', summary, detail: errorMessage(error), life: 5000 });
  }
}
