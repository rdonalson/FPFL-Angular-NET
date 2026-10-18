import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';

import { longDay } from '../display.models';
import { DebtPaydown, PaydownEvent, describeEvent, paydownColor } from './paydown.models';

interface PaydownRow {
  key: string;
  debtIndex: number;
  debtName: string;
  event: PaydownEvent;
  activity: string;
}

/** The Paydown ledger tab: every payment, allocation and interest charge, with the balance left. */
@Component({
  selector: 'app-paydown-ledger',
  imports: [FormsModule, CurrencyPipe, SelectModule, TableModule],
  templateUrl: './paydown-ledger.html',
  styleUrl: './paydown.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaydownLedger {
  readonly debts = input.required<readonly DebtPaydown[]>();

  /** null = every debt. */
  protected readonly debtFilter = signal<number | null>(null);

  protected readonly filterOptions = computed(() => [
    { label: 'All debts', value: null },
    ...this.debts().map((debt) => ({ label: debt.name ?? 'Debt', value: debt.debtId })),
  ]);

  protected readonly rows = computed<PaydownRow[]>(() => {
    const filter = this.debtFilter();
    return this.debts()
      .flatMap((debt, debtIndex) =>
        filter !== null && debt.debtId !== filter
          ? []
          : debt.events.map((event, i) => ({
              key: `${debt.debtId}-${i}`,
              debtIndex,
              debtName: debt.name ?? 'Debt',
              event,
              activity: describeEvent(event),
            })),
      )
      .sort((a, b) => a.event.date.localeCompare(b.event.date) || a.debtIndex - b.debtIndex);
  });

  protected readonly color = paydownColor;
  protected readonly longDay = longDay;
  protected readonly abs = Math.abs;
}
