import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ChartModule } from 'primeng/chart';

import { formatDate } from '../../item-detail/schedule';
import { LedgerDay } from '../display.models';
import { DebtPaydown, paydownChart, paydownColor, payoffStatus } from './paydown.models';

/** The Paydown chart tab: a card per debt (owed, paid, interest, payoff) and every debt's balance over time. */
@Component({
  selector: 'app-paydown-chart',
  imports: [CurrencyPipe, ChartModule],
  templateUrl: './paydown-chart.html',
  styleUrl: './paydown.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaydownChart {
  readonly debts = input.required<readonly DebtPaydown[]>();
  readonly days = input.required<readonly LedgerDay[]>();

  protected readonly chart = computed(() => paydownChart(this.debts(), this.days()));
  protected readonly windowEnd = computed(() => this.days().at(-1)?.wDate ?? '');

  protected readonly color = paydownColor;
  protected readonly formatDate = formatDate;
  protected readonly status = payoffStatus;
}
