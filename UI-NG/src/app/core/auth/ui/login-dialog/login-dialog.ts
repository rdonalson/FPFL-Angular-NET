import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { finalize } from 'rxjs';

import { errorMessage } from '../../../api/api-response';
import { AuthService } from '../../auth';
import { AuthUi } from '../../auth-ui';

@Component({
  selector: 'app-login-dialog',
  imports: [ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, PasswordModule, MessageModule],
  templateUrl: './login-dialog.html',
  styleUrl: '../auth-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginDialog {
  private readonly auth = inject(AuthService);
  protected readonly ui = inject(AuthUi);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  constructor() {
    // Start clean every time the dialog opens.
    effect(() => {
      if (this.ui.openDialog() === 'login') {
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

    const { email, password } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set(null);

    this.auth
      .login(email, password)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (session) => this.ui.signedIn(undefined, session),
        error: (err: unknown) => this.error.set(errorMessage(err)),
      });
  }

  protected visibleChange(visible: boolean): void {
    if (!visible) {
      this.ui.close();
    }
  }
}
