import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ITEM_TYPE, PERIODS } from '../item';
import { itemListPath } from '../item-form';

/**
 * Step one of adding a credit or debit (UI-TSX's SelectPeriodPage): choose how often it repeats.
 * Each card opens the item form for that period.
 */
@Component({
  selector: 'app-period-picker',
  imports: [RouterLink],
  templateUrl: './period-picker.html',
  styleUrls: ['../../item-types/item-types.scss', './period-picker.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PeriodPicker {
  /** 1 = credit, 2 = debit (from the route's data). */
  readonly itemType = input.required<number>();

  protected readonly periods = PERIODS;
  protected readonly listPath = computed(() => itemListPath(this.itemType()));
  protected readonly noun = computed(() => (this.itemType() === ITEM_TYPE.credit ? 'Credit' : 'Debit'));
}
