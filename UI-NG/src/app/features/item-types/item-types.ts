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
import { ItemTypesApi } from './data-access/item-types-api';
import { ItemType } from './item-type';

/** What the single edit dialog is doing right now. */
type DialogState =
  | { kind: 'add' }
  | { kind: 'edit'; row: ItemType }
  | { kind: 'delete'; row: ItemType };

/** Same character rule as the API's strict sanitizer: letters, digits, space and -_'. */
const NAME_PATTERN = /^[\p{L}0-9 _'.-]+$/u;

/**
 * Lists item types from API-NET (GET /item-types). Admins can add, edit (name and id) and delete;
 * the API enforces the same rule, so the buttons are only a convenience.
 */
@Component({
  selector: 'app-item-types',
  imports: [RouterLink, ReactiveFormsModule, ButtonModule, DialogModule, InputTextModule, MessageModule, TableModule],
  templateUrl: './item-types.html',
  styleUrl: './item-types.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ItemTypes {
  private readonly api = inject(ItemTypesApi);
  private readonly messages = inject(MessageService);
  protected readonly auth = inject(AuthService);

  protected readonly itemTypes = this.api.list();

  // value() throws while the resource is in an error state, so read it only when there is one.
  protected readonly rows = computed(() => (this.itemTypes.hasValue() ? (this.itemTypes.value()?.data ?? []) : []));

  protected readonly error = computed(() => {
    const error = this.itemTypes.error();
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
        return 'Add Item Type';
      case 'edit':
        return 'Edit Item Type';
      case 'delete':
        return 'Delete Item Type';
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

  protected openEdit(row: ItemType): void {
    this.form.reset({ id: row.id, name: row.name });
    this.open({ kind: 'edit', row });
  }

  protected openDelete(row: ItemType): void {
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
          : this.api.update(state.row.id, { id: id ?? state.row.id, name: trimmed });
    }

    this.saving.set(true);
    this.dialogError.set(null);
    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (response) => {
        this.messages.add({ severity: 'success', summary: response.message, life: 3000 });
        this.close();
        this.itemTypes.reload();
      },
      error: (err: unknown) => this.dialogError.set(errorMessage(err)),
    });
  }

  private open(state: DialogState): void {
    this.dialogError.set(null);
    this.dialog.set(state);
  }
}
