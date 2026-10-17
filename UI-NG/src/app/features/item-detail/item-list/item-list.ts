import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { finalize } from 'rxjs';

import { ApiError, errorMessage } from '../../../core/api/api-response';
import { ItemApi } from '../data-access/item-api';
import { ITEM_TYPE, Item } from '../item';
import { itemListPath } from '../item-form';
import { describeDateRange, describeSchedule } from '../schedule';

/** A table row: the item plus the text the template shows. */
interface ItemRow {
  item: Item;
  name: string;
  amount: number;
  period: string;
  schedule: string;
  dates: string;
}

/**
 * The signed-in user's credits or debits (UI-TSX's ItemsListPage): list, New, Edit and Delete.
 * The route supplies `itemType` (1 = credits, 2 = debits) through component input binding.
 */
@Component({
  selector: 'app-item-list',
  imports: [
    RouterLink,
    CurrencyPipe,
    ButtonModule,
    DialogModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    MessageModule,
    TableModule,
    TagModule,
  ],
  templateUrl: './item-list.html',
  styleUrls: ['../../item-types/item-types.scss', './item-list.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ItemList {
  /** 1 = credits, 2 = debits. */
  readonly itemType = input.required<number>();

  private readonly api = inject(ItemApi);
  private readonly messages = inject(MessageService);

  protected readonly items = this.api.listForCurrentUser(this.itemType);

  protected readonly isCredit = computed(() => this.itemType() === ITEM_TYPE.credit);
  protected readonly title = computed(() => (this.isCredit() ? 'Credits' : 'Debits'));
  protected readonly noun = computed(() => (this.isCredit() ? 'Credit' : 'Debit'));
  protected readonly listPath = computed(() => itemListPath(this.itemType()));
  protected readonly subtitle = computed(() =>
    this.isCredit() ? 'Money coming in: pay, refunds, transfers in.' : 'Money going out: bills, payments, subscriptions.',
  );

  protected readonly rows = computed<ItemRow[]>(() =>
    // value() throws while the resource is in an error state, so read it only when there is one.
    (this.items.hasValue() ? (this.items.value()?.data ?? []) : []).map((item) => ({
      item,
      name: item.name,
      amount: item.amount,
      period: item.timePeriod?.name ?? '—',
      schedule: describeSchedule(item),
      dates: describeDateRange(item),
    })),
  );

  /** What the user typed in the filter box. */
  protected readonly filter = signal('');

  /** Rows whose name, period, schedule, dates or amount contain every word typed (any order, any case). */
  protected readonly filteredRows = computed(() => {
    const words = this.filter().toLowerCase().split(/\s+/).filter(Boolean);
    if (words.length === 0) {
      return this.rows();
    }
    return this.rows().filter((row) => {
      const haystack = [row.name, row.period, row.schedule, row.dates, row.amount.toFixed(2)].join(' ').toLowerCase();
      return words.every((word) => haystack.includes(word));
    });
  });

  protected readonly error = computed(() => {
    const error = this.items.error();
    if (!error) {
      return null;
    }
    return {
      message: errorMessage(error),
      correlationId: error instanceof ApiError ? error.correlationId : '',
    };
  });

  /** The item waiting for delete confirmation. */
  protected readonly pendingDelete = signal<Item | null>(null);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  protected confirmDelete(item: Item): void {
    this.deleteError.set(null);
    this.pendingDelete.set(item);
  }

  protected cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  protected delete(): void {
    const item = this.pendingDelete();
    if (!item) {
      return;
    }

    this.deleting.set(true);
    this.api
      .remove(item.id)
      .pipe(finalize(() => this.deleting.set(false)))
      .subscribe({
        next: (response) => {
          this.messages.add({ severity: 'success', summary: response.message, detail: item.name, life: 3000 });
          this.pendingDelete.set(null);
          this.items.reload();
        },
        error: (error: unknown) => this.deleteError.set(errorMessage(error)),
      });
  }
}
