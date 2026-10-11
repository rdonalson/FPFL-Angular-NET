import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { MessageService } from 'primeng/api';
import { finalize } from 'rxjs';

import { errorMessage } from '../../../api/api-response';
import { AuthService } from '../../auth';
import { AuthUi } from '../../auth-ui';
import { passwordsMatch } from '../password-match';

@Component({
  selector: 'app-change-password-dialog',
  imports: [ReactiveFormsModule, DialogModule, ButtonModule, PasswordModule, MessageModule],
  templateUrl: './change-password-dialog.html',
  styleUrl: '../auth-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordDialog {
  private readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);
  protected readonly ui = inject(AuthUi);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      current: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirm: ['', Validators.required],
    },
    { validators: passwordsMatch('password', 'confirm') },
  );

  constructor() {
    effect(() => {
      if (this.ui.openDialog() === 'change-password') {
        this.form.reset();
        this.error.set(null);
      }
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { current, password } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set(null);

    this.auth
      .changePassword(current, password)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          this.ui.close();
          this.messages.add({
            severity: 'success',
            summary: 'Password changed',
            detail: 'Other devices have been signed out.',
            life: 3000,
          });
        },
        error: (err: unknown) => this.error.set(errorMessage(err)),
      });
  }

  protected visibleChange(visible: boolean): void {
    if (!visible) {
      this.ui.close();
    }
  }
}
