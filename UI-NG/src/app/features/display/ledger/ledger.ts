import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { LedgerDay, longDay } from '../display.models';

/**
 * The Ledger tab: one row per day with its credits, debits, net change and balance; a day with
 * activity expands to list its items (UI-TSX's LedgerPanel).
 */
@Component({
  selector: 'app-ledger',
  imports: [FormsModule, CurrencyPipe, ButtonModule, TableModule, TagModule, ToggleSwitchModule],
  templateUrl: './ledger.html',
  styleUrl: './ledger.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Ledger {
  readonly days = input.required<readonly LedgerDay[]>();

  /** Hide the quiet days, which are most of them in a long forecast. */
  protected readonly activeOnly = signal(false);

  protected readonly rows = computed(() => {
    const days = this.days();
    return this.activeOnly() ? days.filter((day) => day.items.length > 0) : [...days];
  });

  protected readonly expanded = signal<Record<string, boolean>>({});

  protected readonly longDay = longDay;

  protected toggle(day: LedgerDay): void {
    this.expanded.update((current) => {
      const next = { ...current };
      if (next[day.rollupKey]) {
        delete next[day.rollupKey];
      } else {
        next[day.rollupKey] = true;
      }
      return next;
    });
  }

  protected expandAll(): void {
    this.expanded.set(Object.fromEntries(this.rows().filter((d) => d.items.length > 0).map((d) => [d.rollupKey, true])));
  }

  protected collapseAll(): void {
    this.expanded.set({});
  }
}
