import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageModule } from 'primeng/message';
import { TabsModule } from 'primeng/tabs';

import { ApiError, errorMessage } from '../../core/api/api-response';
import { formatDate } from '../item-detail/schedule';
import { LedgerChart } from './chart/chart';
import { DisplayApi } from './data-access/display-api';
import {
  LedgerRequest,
  RANGE_PRESETS,
  addDays,
  addMonths,
  summarize,
  toLedgerRequest,
  today,
  validateRange,
} from './display.models';
import { Ledger } from './ledger/ledger';

/**
 * The forecast (UI-TSX's DisplayPage): pick a date range, then see the balance day by day as a chart
 * or a ledger. Runs once on arrival with the default range (today + 90 days, as in UI-TSX) so the
 * page is never empty; after that, Calculate re-runs it.
 */
@Component({
  selector: 'app-display',
  imports: [
    RouterLink,
    FormsModule,
    CurrencyPipe,
    ButtonModule,
    DatePickerModule,
    MessageModule,
    TabsModule,
    LedgerChart,
    Ledger,
  ],
  templateUrl: './display.html',
  styleUrls: ['../item-types/item-types.scss', './display.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DisplayPage {
  private readonly api = inject(DisplayApi);

  protected readonly presets = RANGE_PRESETS;

  protected readonly start = signal<Date | null>(today());
  protected readonly end = signal<Date | null>(addDays(today(), 90));
  protected readonly rangeError = signal<string | null>(null);

  /** What was last sent; the ledger resource re-runs whenever it changes. */
  private readonly query = signal<LedgerRequest | undefined>(toLedgerRequest(this.start()!, this.end()!));

  protected readonly resource = this.api.ledger(() => this.query());

  /** value() throws while the resource is in an error state, so read it only when there is one. */
  protected readonly days = computed(() => (this.resource.hasValue() ? (this.resource.value()?.data ?? []) : []));
  protected readonly summary = computed(() => summarize(this.days()));
  protected readonly formatDate = formatDate;

  protected readonly loadError = computed(() => {
    const error = this.resource.error();
    return error
      ? { message: errorMessage(error), correlationId: error instanceof ApiError ? error.correlationId : '' }
      : null;
  });

  /** The range the results on screen were built for. */
  protected readonly shownRange = computed(() => {
    const q = this.query();
    return q ? `${formatDate(q.ledgerStartDate)} – ${formatDate(q.ledgerEndDate)}` : '';
  });

  protected applyPreset(preset: (typeof RANGE_PRESETS)[number]): void {
    const start = this.start() ?? today();
    this.start.set(start);
    this.end.set(preset.months ? addMonths(start, preset.months) : addDays(start, preset.days ?? 0));
    this.calculate();
  }

  protected calculate(): void {
    const start = this.start();
    const end = this.end();
    const problem = validateRange(start, end);
    this.rangeError.set(problem);
    if (problem) {
      return;
    }

    const request = toLedgerRequest(start!, end!);
    const current = this.query();
    if (current && current.ledgerStartDate === request.ledgerStartDate && current.ledgerEndDate === request.ledgerEndDate) {
      this.resource.reload(); // same range: pick up any items changed since
    } else {
      this.query.set(request);
    }
  }
}
