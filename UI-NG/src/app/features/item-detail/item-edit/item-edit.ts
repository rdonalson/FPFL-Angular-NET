import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { finalize } from 'rxjs';

import { ApiError, errorMessage } from '../../../core/api/api-response';
import { ItemApi } from '../data-access/item-api';
import {
  DAY_OPTIONS,
  ITEM_TYPE,
  MONTH_OPTIONS,
  NTH_OPTIONS,
  Option,
  PERIOD,
  PERIODS,
  PeriodOption,
  WEEKDAY_OPTIONS,
} from '../item';
import {
  ItemFormValue,
  emptyItemForm,
  itemListPath,
  toFormValue,
  toItemRequest,
  validateItemForm,
} from '../item-form';

type ItemFormControls = { [K in keyof ItemFormValue]: FormControl<ItemFormValue[K]> };

type MonthDayControl = { label: string; month: keyof ItemFormValue; day: keyof ItemFormValue };

/** Route params arrive as strings; empty means "not on this route". */
const toOptionalNumber = (value: unknown): number | undefined =>
  value === undefined || value === null || value === '' ? undefined : Number(value);

/**
 * Add or edit a credit or debit (one form instead of UI-TSX's twenty add and edit pages). The period selector
 * decides which schedule fields show; only those are sent, and the API validates them again.
 * Routes: /credits/new/:periodId, /credits/:id/edit (and the same under /debits).
 */
@Component({
  selector: 'app-item-edit',
  imports: [
    RouterLink,
    ReactiveFormsModule,
    ButtonModule,
    DatePickerModule,
    InputNumberModule,
    InputTextModule,
    MessageModule,
    SelectModule,
    ToggleSwitchModule,
  ],
  templateUrl: './item-edit.html',
  styleUrls: ['../../item-types/item-types.scss', './item-edit.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ItemEdit {
  /** 1 = credit, 2 = debit (route data). */
  readonly itemType = input.required<number>();
  /** Preselected period on the "new" route. */
  readonly periodId = input<number | undefined, unknown>(undefined, { transform: toOptionalNumber });
  /** The item being edited on the "edit" route. */
  readonly id = input<number | undefined, unknown>(undefined, { transform: toOptionalNumber });

  private readonly api = inject(ItemApi);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);

  protected readonly PERIOD = PERIOD;
  protected readonly periods: PeriodOption[] = [...PERIODS];
  protected readonly weekdays: Option[] = [...WEEKDAY_OPTIONS];
  protected readonly months: Option[] = [...MONTH_OPTIONS];
  protected readonly days: Option[] = [...DAY_OPTIONS];
  protected readonly nthOptions: Option[] = [...NTH_OPTIONS];
  protected readonly quarters: MonthDayControl[] = [1, 2, 3, 4].map((q) => ({
    label: `Quarter ${q}`,
    month: `quarterly${q}Month` as keyof ItemFormValue,
    day: `quarterly${q}Day` as keyof ItemFormValue,
  }));
  protected readonly halves: MonthDayControl[] = [1, 2].map((h) => ({
    label: h === 1 ? 'First date' : 'Second date',
    month: `semiAnnual${h}Month` as keyof ItemFormValue,
    day: `semiAnnual${h}Day` as keyof ItemFormValue,
  }));

  protected readonly form = new FormGroup(
    Object.fromEntries(
      Object.entries(emptyItemForm()).map(([key, value]) => [key, new FormControl(value, { nonNullable: true })]),
    ) as unknown as ItemFormControls,
  );

  protected readonly selectedPeriod = toSignal(this.form.controls.periodId.valueChanges, { initialValue: null });
  protected readonly dateRange = toSignal(this.form.controls.dateRangeReq.valueChanges, { initialValue: false });

  protected readonly existing = this.api.getById(this.id);

  protected readonly isEdit = computed(() => this.id() !== undefined);
  protected readonly noun = computed(() => (this.itemType() === ITEM_TYPE.credit ? 'Credit' : 'Debit'));
  protected readonly listPath = computed(() => itemListPath(this.itemType()));
  protected readonly title = computed(() => `${this.isEdit() ? 'Edit' : 'New'} ${this.noun()}`);
  protected readonly periodLabel = computed(
    () => PERIODS.find((period) => period.id === this.selectedPeriod())?.label ?? '',
  );

  protected readonly loadError = computed(() => {
    const error = this.existing.error();
    return error
      ? { message: errorMessage(error), correlationId: error instanceof ApiError ? error.correlationId : '' }
      : null;
  });

  protected readonly saving = signal(false);
  protected readonly problem = signal<string | null>(null);

  constructor() {
    // "New" route: start with the period chosen on the picker.
    effect(() => {
      const periodId = this.periodId();
      if (!this.isEdit() && periodId) {
        untracked(() => this.form.controls.periodId.setValue(periodId));
      }
    });

    // "Edit" route: fill the form once the item arrives.
    effect(() => {
      const item = this.existing.hasValue() ? this.existing.value()?.data : undefined;
      if (item) {
        untracked(() => this.form.reset(toFormValue(item)));
      }
    });
  }

  protected submit(): void {
    const value = this.form.getRawValue();
    const problem = validateItemForm(value);
    this.problem.set(problem);
    if (problem) {
      return;
    }

    const request = toItemRequest(value, this.itemType());
    const id = this.id();
    this.saving.set(true);

    (id ? this.api.update(id, request) : this.api.create(request))
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (response) => {
          this.messages.add({ severity: 'success', summary: response.message, detail: request.name, life: 3000 });
          void this.router.navigateByUrl(this.listPath());
        },
        error: (error: unknown) => this.problem.set(errorMessage(error)),
      });
  }
}
