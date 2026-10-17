import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';
import { finalize } from 'rxjs';

import { ApiError, errorMessage } from '../../../core/api/api-response';
import { InitialAmountApi } from '../data-access/initial-amount-api';
import { formatDate } from '../schedule';

/**
 * The starting balance the ledger forecasts from (UI-TSX's InitialAmountPage). Every user has one:
 * the API creates $0.00 at registration or on the first visit here, so there is no "create" step.
 */
@Component({
  selector: 'app-initial-amount',
  imports: [RouterLink, FormsModule, CurrencyPipe, ButtonModule, InputNumberModule, MessageModule],
  templateUrl: './initial-amount.html',
  styleUrls: ['../../item-types/item-types.scss', './initial-amount.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InitialAmountPage {
  private readonly api = inject(InitialAmountApi);
  private readonly messages = inject(MessageService);

  protected readonly resource = this.api.current();

  /** value() throws while the resource is in an error state, so read it only when there is one. */
  protected readonly current = computed(() => (this.resource.hasValue() ? (this.resource.value()?.data ?? null) : null));
  protected readonly setOn = computed(() => formatDate(this.current()?.beginDate ?? null));

  protected readonly loadError = computed(() => {
    const error = this.resource.error();
    return error
      ? { message: errorMessage(error), correlationId: error instanceof ApiError ? error.correlationId : '' }
      : null;
  });

  protected readonly editing = signal(false);
  protected readonly draft = signal<number | null>(null);
  protected readonly saving = signal(false);
  protected readonly problem = signal<string | null>(null);

  protected startEdit(): void {
    this.draft.set(this.current()?.amount ?? 0);
    this.problem.set(null);
    this.editing.set(true);
  }

  protected cancel(): void {
    this.editing.set(false);
    this.problem.set(null);
  }

  protected save(): void {
    const current = this.current();
    const amount = this.draft();
    if (!current) {
      return;
    }
    if (amount === null || !Number.isFinite(amount) || amount < 0) {
      this.problem.set('Enter an amount of $0.00 or more.');
      return;
    }

    this.saving.set(true);
    this.problem.set(null);
    this.api
      .update(current.id, amount)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (response) => {
          this.messages.add({ severity: 'success', summary: response.message, life: 3000 });
          this.editing.set(false);
          this.resource.reload();
        },
        error: (error: unknown) => this.problem.set(errorMessage(error)),
      });
  }
}
