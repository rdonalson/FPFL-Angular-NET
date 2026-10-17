import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ChartModule } from 'primeng/chart';

import { LedgerDay } from '../display.models';
import { balanceChart, flowChart } from './chart-data';

/** The Chart tab: the balance over time, and each day's credits and debits underneath (UI-TSX's ChartPanel). */
@Component({
  selector: 'app-ledger-chart',
  imports: [ChartModule],
  templateUrl: './chart.html',
  styleUrl: './chart.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LedgerChart {
  readonly days = input.required<readonly LedgerDay[]>();

  protected readonly balance = computed(() => balanceChart(this.days()));
  protected readonly flow = computed(() => flowChart(this.days()));
}
