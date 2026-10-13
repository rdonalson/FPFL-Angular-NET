import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { Observable, finalize } from 'rxjs';

import { ApiError, ApiResponse, errorMessage } from '../../core/api/api-response';
import { AuthService } from '../../core/auth/auth';
import { TimePeriodsApi } from './data-access/time-periods-api';
import { TimePeriod } from './time-period';

/** What the single edit dialog is doing right now. */
type DialogState =
  | { kind: 'add' }
  | { kind: 'rename'; row: TimePeriod }
  | { kind: 'delete'; row: TimePeriod };

/** Same character rule as the API's strict sanitizer: letters, digits, space and -_'. */
const NAME_PATTERN = /^[\p{L}0-9 _'.-]+$/u;

/**
 * Lists time periods from API-NET (GET /time-periods). Admins can add, rename and delete;
 * the API enforces the same rule, so the buttons are only a convenience.
 */
@Component({
  selector: 'app-time-periods',
  imports: [RouterLink, ReactiveFormsModule, ButtonModule, DialogModule, InputTextModule, MessageModule, TableModule],
  templateUrl: './time-periods.html',
  styleUrls: ['../item-types/item-types.scss', './time-periods.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TimePeriods {
  private readonly api = inject(TimePeriodsApi);
  private readonly messages = inject(MessageService);
  protected readonly auth = inject(AuthService);

  protected readonly timePeriods = this.api.list();

  protected readonly rows = computed(() => this.timePeriods.value()?.data ?? []);

  protected readonly error = computed(() => {
    const error = this.timePeriods.error();
    if (!error) {
      return null;
    }
    return {
      message: errorMessage(error),
      correlationId: error instanceof ApiError ? error.correlationId : '',
    };
  });

  protected readonly dialog = signal<DialogState | null>(null);
  protected readonly saving = signal(false);
  protected readonly dialogError = signal<string | null>(null);

  protected readonly dialogTitle = computed(() => {
    switch (this.dialog()?.kind) {
      case 'add':
        return 'Add Time Period';
      case 'rename':
        return 'Rename Time Period';
      case 'delete':
        return 'Delete Time Period';
      default:
        return '';
    }
  });

  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly form = this.fb.group({
    id: this.fb.control<number | null>(null, [Validators.required, Validators.min(1)]),
    name: ['', [Validators.required, Validators.maxLength(75), Validators.pattern(NAME_PATTERN)]],
  });

  protected openAdd(): void {
    this.form.reset();
    this.form.controls.id.enable();
    this.open({ kind: 'add' });
  }

  protected openRename(row: TimePeriod): void {
    this.form.reset({ id: row.id, name: row.name });
    this.form.controls.id.disable(); // ids can't change; the route carries it
    this.open({ kind: 'rename', row });
  }

  protected openDelete(row: TimePeriod): void {
    this.open({ kind: 'delete', row });
  }

  protected close(): void {
    this.dialog.set(null);
  }

  protected visibleChange(visible: boolean): void {
    if (!visible) {
      this.close();
    }
  }

  protected submit(): void {
    const state = this.dialog();
    if (!state) {
      return;
    }

    let request: Observable<ApiResponse<unknown>>;
    if (state.kind === 'delete') {
      request = this.api.remove(state.row.id);
    } else {
      if (this.form.invalid) {
        this.form.markAllAsTouched();
        return;
      }
      const { id, name } = this.form.getRawValue();
      const trimmed = name.trim();
      request =
        state.kind === 'add'
          ? this.api.create({ id: id ?? 0, name: trimmed })
          : this.api.rename(state.row.id, trimmed);
    }

    this.saving.set(true);
    this.dialogError.set(null);
    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (response) => {
        this.messages.add({ severity: 'success', summary: response.message, life: 3000 });
        this.close();
        this.timePeriods.reload();
      },
      error: (err: unknown) => this.dialogError.set(errorMessage(err)),
    });
  }

  private open(state: DialogState): void {
    this.dialogError.set(null);
    this.dialog.set(state);
  }
}
