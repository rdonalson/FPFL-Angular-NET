import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { finalize } from 'rxjs';

import { ApiError, errorMessage } from '../../core/api/api-response';
import { ItemApi } from '../item-detail/data-access/item-api';
import { ITEM_TYPE, Item } from '../item-detail/item';
import { describeSchedule, formatDate } from '../item-detail/schedule';
import { DebtApi } from './data-access/debt-api';
import {
  AllocationForm,
  Debt,
  DebtForm,
  MAX_ALLOCATIONS,
  allocatedElsewhere,
  emptyDebtForm,
  formatPercent,
  newAllocation,
  perOccurrence,
  toDebtForm,
  toDebtRequest,
  validateDebtForm,
} from './debt-paydown.models';

/**
 * Debt Paydown setup: choose debits to pay down, what's owed on each, and which credits send a
 * percentage of every occurrence to them. The forecast (Display) deducts those shares and charts
 * each debt's payoff.
 */
@Component({
  selector: 'app-debt-paydown',
  imports: [
    RouterLink,
    FormsModule,
    CurrencyPipe,
    ButtonModule,
    DatePickerModule,
    DialogModule,
    InputNumberModule,
    MessageModule,
    SelectModule,
    TagModule,
  ],
  templateUrl: './debt-paydown.html',
  styleUrls: ['../item-types/item-types.scss', './debt-paydown.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DebtPaydownPage {
  private readonly debtApi = inject(DebtApi);
  private readonly itemApi = inject(ItemApi);
  private readonly messages = inject(MessageService);

  protected readonly debtsResource = this.debtApi.list();
  protected readonly debitsResource = this.itemApi.listForCurrentUser(() => ITEM_TYPE.debit);
  protected readonly creditsResource = this.itemApi.listForCurrentUser(() => ITEM_TYPE.credit);

  /** value() throws while a resource is in an error state, so read it only when there is one. */
  protected readonly debts = computed(() => (this.debtsResource.hasValue() ? (this.debtsResource.value()?.data ?? []) : []));
  protected readonly debits = computed(() => (this.debitsResource.hasValue() ? (this.debitsResource.value()?.data ?? []) : []));
  protected readonly credits = computed(() => (this.creditsResource.hasValue() ? (this.creditsResource.value()?.data ?? []) : []));

  protected readonly loading = computed(
    () => this.debtsResource.isLoading() || this.debitsResource.isLoading() || this.creditsResource.isLoading(),
  );

  protected readonly loadError = computed(() => {
    const error = this.debtsResource.error() ?? this.debitsResource.error() ?? this.creditsResource.error();
    return error
      ? { message: errorMessage(error), correlationId: error instanceof ApiError ? error.correlationId : '' }
      : null;
  });

  private readonly itemsById = computed(() => new Map<number, Item>([...this.debits(), ...this.credits()].map((i) => [i.id, i])));

  /** Debits not tracked yet: the choices for a new debt. */
  protected readonly untrackedDebits = computed(() => {
    const tracked = new Set(this.debts().map((d) => d.itemId));
    return this.debits().filter((d) => !tracked.has(d.id));
  });

  protected readonly formatDate = formatDate;
  protected readonly formatPercent = formatPercent;
  protected readonly maxAllocations = MAX_ALLOCATIONS;

  // ---- editor --------------------------------------------------------------------------------

  /** null = closed; 'new' = tracking a debit; a Debt = editing it. */
  protected readonly editing = signal<Debt | 'new' | null>(null);
  protected readonly form = signal<DebtForm>(emptyDebtForm(new Date()));
  protected readonly saving = signal(false);
  protected readonly problem = signal<string | null>(null);

  protected readonly editorTitle = computed(() => {
    const editing = this.editing();
    return editing === 'new' || editing === null ? 'Track a debt' : `Edit ${editing.name ?? 'debt'}`;
  });

  /** Shares of each credit taken by the other debts. */
  protected readonly elsewhere = computed(() => {
    const editing = this.editing();
    return allocatedElsewhere(this.debts(), editing && editing !== 'new' ? editing.id : null);
  });

  /** The debit currently chosen in the editor. */
  protected readonly chosenDebit = computed(() => {
    const id = this.form().itemId;
    return id === null ? null : (this.itemsById().get(id) ?? null);
  });

  /** Debit choices: untracked ones, plus the one being edited. */
  protected readonly debitOptions = computed(() => {
    const editing = this.editing();
    if (editing && editing !== 'new') {
      return this.debits().filter((d) => d.id === editing.itemId);
    }
    return this.untrackedDebits();
  });

  protected openNew(): void {
    this.form.set(emptyDebtForm(new Date()));
    this.problem.set(null);
    this.editing.set('new');
  }

  protected openEdit(debt: Debt): void {
    this.form.set(toDebtForm(debt));
    this.problem.set(null);
    this.editing.set(debt);
  }

  protected closeEditor(): void {
    if (!this.saving()) {
      this.editing.set(null);
    }
  }

  protected patch(changes: Partial<DebtForm>): void {
    this.form.update((form) => ({ ...form, ...changes }));
    this.problem.set(null);
  }

  protected patchAllocation(key: number, changes: Partial<AllocationForm>): void {
    this.form.update((form) => ({
      ...form,
      allocations: form.allocations.map((a) => (a.key === key ? { ...a, ...changes } : a)),
    }));
    this.problem.set(null);
  }

  protected addAllocation(): void {
    this.form.update((form) => ({ ...form, allocations: [...form.allocations, newAllocation()] }));
  }

  protected removeAllocation(key: number): void {
    this.form.update((form) => ({ ...form, allocations: form.allocations.filter((a) => a.key !== key) }));
    this.problem.set(null);
  }

  protected item(id: number | null): Item | null {
    return id === null ? null : (this.itemsById().get(id) ?? null);
  }

  protected schedule(id: number | null): string {
    const item = this.item(id);
    return item ? describeSchedule(item) : '';
  }

  protected share(allocation: AllocationForm): number {
    return perOccurrence(this.item(allocation.creditItemId)?.amount, allocation.percent);
  }

  protected takenElsewhere(creditItemId: number | null): number {
    return creditItemId === null ? 0 : (this.elsewhere().get(creditItemId) ?? 0);
  }

  protected save(): void {
    const editing = this.editing();
    if (!editing) {
      return;
    }

    const problem = validateDebtForm(this.form(), this.elsewhere(), (id) => this.item(id)?.name ?? 'That credit');
    if (problem) {
      this.problem.set(problem);
      return;
    }

    const request = toDebtRequest(this.form());
    const call = editing === 'new' ? this.debtApi.create(request) : this.debtApi.update(editing.id, request);

    this.saving.set(true);
    call.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (response) => {
        this.messages.add({ severity: 'success', summary: response.message, life: 3000 });
        this.editing.set(null);
        this.debtsResource.reload();
      },
      error: (error: unknown) => this.problem.set(errorMessage(error)),
    });
  }

  // ---- delete --------------------------------------------------------------------------------

  protected readonly pendingDelete = signal<Debt | null>(null);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  protected confirmDelete(debt: Debt): void {
    this.deleteError.set(null);
    this.pendingDelete.set(debt);
  }

  protected cancelDelete(): void {
    if (!this.deleting()) {
      this.pendingDelete.set(null);
    }
  }

  protected delete(): void {
    const debt = this.pendingDelete();
    if (!debt) {
      return;
    }

    this.deleting.set(true);
    this.debtApi
      .remove(debt.id)
      .pipe(finalize(() => this.deleting.set(false)))
      .subscribe({
        next: (response) => {
          this.messages.add({ severity: 'success', summary: response.message, life: 3000 });
          this.pendingDelete.set(null);
          this.debtsResource.reload();
        },
        error: (error: unknown) => this.deleteError.set(errorMessage(error)),
      });
  }
}
