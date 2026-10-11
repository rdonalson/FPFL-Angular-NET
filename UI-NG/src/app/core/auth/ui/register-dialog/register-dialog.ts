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
import { passwordsMatch } from '../password-match';

@Component({
  selector: 'app-register-dialog',
  imports: [ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, PasswordModule, MessageModule],
  templateUrl: './register-dialog.html',
  styleUrl: '../auth-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterDialog {
  private readonly auth = inject(AuthService);
  protected readonly ui = inject(AuthUi);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      first: ['', [Validators.required, Validators.maxLength(50)]],
      last: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirm: ['', Validators.required],
    },
    { validators: passwordsMatch('password', 'confirm') },
  );

  constructor() {
    effect(() => {
      if (this.ui.openDialog() === 'register') {
        this.form.reset();
        this.error.set(null);
      }
    });
  }

  protected showError(name: 'first' | 'last' | 'email' | 'password' | 'confirm'): boolean {
    const control = this.form.controls[name];
    return control.touched && control.invalid;
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { first, last, email, password } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set(null);

    this.auth
      .register({ first: first.trim(), last: last.trim(), email: email.trim(), password })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (session) => this.ui.signedIn(`Account created. Welcome, ${session.first}`, session),
        error: (err: unknown) => this.error.set(errorMessage(err)),
      });
  }

  protected visibleChange(visible: boolean): void {
    if (!visible) {
      this.ui.close();
    }
  }
}
