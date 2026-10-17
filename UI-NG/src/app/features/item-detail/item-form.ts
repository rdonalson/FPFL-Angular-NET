import { Item, ItemRequest, PERIOD } from './item';

/** Everything the item form edits, as its controls hold it (dates as Date objects). */
export interface ItemFormValue {
  name: string;
  amount: number | null;
  periodId: number | null;
  weeklyDow: number | null;
  everyOtherWeekDow: number | null;
  biMonthlyDay1: number | null;
  biMonthlyDay2: number | null;
  monthlyDom: number | null;
  quarterly1Month: number | null;
  quarterly1Day: number | null;
  quarterly2Month: number | null;
  quarterly2Day: number | null;
  quarterly3Month: number | null;
  quarterly3Day: number | null;
  quarterly4Month: number | null;
  quarterly4Day: number | null;
  semiAnnual1Month: number | null;
  semiAnnual1Day: number | null;
  semiAnnual2Month: number | null;
  semiAnnual2Day: number | null;
  annualMoy: number | null;
  annualDom: number | null;
  nthDow: number | null;
  nthIndex: number | null;
  dateRangeReq: boolean;
  beginDate: Date | null;
  endDate: Date | null;
}

export function emptyItemForm(periodId: number | null = null): ItemFormValue {
  return {
    name: '',
    amount: null,
    periodId,
    weeklyDow: null,
    everyOtherWeekDow: null,
    biMonthlyDay1: null,
    biMonthlyDay2: null,
    monthlyDom: null,
    quarterly1Month: null,
    quarterly1Day: null,
    quarterly2Month: null,
    quarterly2Day: null,
    quarterly3Month: null,
    quarterly3Day: null,
    quarterly4Month: null,
    quarterly4Day: null,
    semiAnnual1Month: null,
    semiAnnual1Day: null,
    semiAnnual2Month: null,
    semiAnnual2Day: null,
    annualMoy: null,
    annualDom: null,
    nthDow: null,
    nthIndex: null,
    dateRangeReq: false,
    beginDate: null,
    endDate: null,
  };
}

/** Date → 'yyyy-MM-dd' using the local calendar day (no UTC shift). */
export function toIsoDate(date: Date | null): string | null {
  if (!date) {
    return null;
  }
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** 'yyyy-MM-dd' → local Date at midnight. */
export function fromIsoDate(value: string | null): Date | null {
  if (!value) {
    return null;
  }
  const [year, month, day] = value.split('-').map(Number);
  return year && month && day ? new Date(year, month - 1, day) : null;
}

/** An existing item as form values (for the edit page). */
export function toFormValue(item: Item): ItemFormValue {
  return {
    ...emptyItemForm(item.timePeriod?.id ?? null),
    name: item.name,
    amount: item.amount,
    weeklyDow: item.weeklyDow,
    everyOtherWeekDow: item.everyOtherWeekDow,
    biMonthlyDay1: item.biMonthlyDay1,
    biMonthlyDay2: item.biMonthlyDay2,
    monthlyDom: item.monthlyDom,
    quarterly1Month: item.quarterly1Month,
    quarterly1Day: item.quarterly1Day,
    quarterly2Month: item.quarterly2Month,
    quarterly2Day: item.quarterly2Day,
    quarterly3Month: item.quarterly3Month,
    quarterly3Day: item.quarterly3Day,
    quarterly4Month: item.quarterly4Month,
    quarterly4Day: item.quarterly4Day,
    semiAnnual1Month: item.semiAnnual1Month,
    semiAnnual1Day: item.semiAnnual1Day,
    semiAnnual2Month: item.semiAnnual2Month,
    semiAnnual2Day: item.semiAnnual2Day,
    annualMoy: item.annualMoy,
    annualDom: item.annualDom,
    nthDow: item.nthDow,
    nthIndex: item.nthIndex,
    dateRangeReq: item.dateRangeReq,
    beginDate: fromIsoDate(item.beginDate),
    endDate: fromIsoDate(item.endDate),
  };
}

/**
 * The first broken rule, or null when the form can be saved. Mirrors the API's ItemRules so most
 * mistakes are caught before a request is sent; the API remains the authority.
 */
export function validateItemForm(value: ItemFormValue): string | null {
  const missing = (...fields: (number | null)[]) => fields.some((field) => field === null);

  if (!value.name.trim()) {
    return 'Name is required.';
  }
  if (value.amount === null || value.amount <= 0) {
    return 'Amount must be greater than zero.';
  }

  switch (value.periodId) {
    case null:
      return 'Choose how often it repeats.';
    case PERIOD.oneTime:
      return value.beginDate ? null : 'Choose the date.';
    case PERIOD.weekly:
      if (missing(value.weeklyDow)) return 'Choose the day of the week.';
      break;
    case PERIOD.everyTwoWeeks:
      if (missing(value.everyOtherWeekDow)) return 'Choose the day of the week.';
      if (!value.beginDate) return 'Choose the first date it happens.';
      break;
    case PERIOD.biMonthly:
      if (missing(value.biMonthlyDay1, value.biMonthlyDay2)) return 'Choose both days of the month.';
      if (value.biMonthlyDay1 === value.biMonthlyDay2) return 'The two days must be different.';
      break;
    case PERIOD.monthly:
      if (missing(value.monthlyDom)) return 'Choose the day of the month.';
      break;
    case PERIOD.quarterly:
      if (
        missing(
          value.quarterly1Month, value.quarterly1Day, value.quarterly2Month, value.quarterly2Day,
          value.quarterly3Month, value.quarterly3Day, value.quarterly4Month, value.quarterly4Day,
        )
      ) {
        return 'Choose a month and day for all four quarters.';
      }
      break;
    case PERIOD.semiAnnual:
      if (missing(value.semiAnnual1Month, value.semiAnnual1Day, value.semiAnnual2Month, value.semiAnnual2Day)) {
        return 'Choose both dates.';
      }
      break;
    case PERIOD.annual:
      if (missing(value.annualMoy, value.annualDom)) return 'Choose the month and day.';
      break;
    case PERIOD.nthWeekday:
      if (missing(value.nthDow, value.nthIndex)) return 'Choose which weekday and which week.';
      break;
  }

  if (value.dateRangeReq) {
    if (!value.beginDate || !value.endDate) {
      return 'Choose both the start and end dates, or turn off the date range.';
    }
    if (value.beginDate && value.beginDate > value.endDate) {
      return 'The start date must be on or before the end date.';
    }
  }
  return null;
}

/** The request body for the API: only the fields that belong to the chosen period are sent. */
export function toItemRequest(value: ItemFormValue, itemType: number): ItemRequest {
  const period = value.periodId ?? 0;
  const pick = <T>(periodId: number, field: T): T | null => (period === periodId ? field : null);

  let beginDate: string | null = null;
  let endDate: string | null = null;
  let dateRangeReq = value.dateRangeReq;

  if (period === PERIOD.oneTime) {
    beginDate = toIsoDate(value.beginDate);
    dateRangeReq = true;
  } else if (period === PERIOD.everyTwoWeeks) {
    beginDate = toIsoDate(value.beginDate);
    endDate = value.dateRangeReq ? toIsoDate(value.endDate) : null;
  } else if (value.dateRangeReq) {
    beginDate = toIsoDate(value.beginDate);
    endDate = toIsoDate(value.endDate);
  }

  return {
    name: value.name.trim(),
    amount: value.amount ?? 0,
    fkItemType: itemType,
    fkPeriod: period,
    beginDate,
    endDate,
    weeklyDow: pick(PERIOD.weekly, value.weeklyDow),
    everyOtherWeekDow: pick(PERIOD.everyTwoWeeks, value.everyOtherWeekDow),
    biMonthlyDay1: pick(PERIOD.biMonthly, value.biMonthlyDay1),
    biMonthlyDay2: pick(PERIOD.biMonthly, value.biMonthlyDay2),
    monthlyDom: pick(PERIOD.monthly, value.monthlyDom),
    quarterly1Month: pick(PERIOD.quarterly, value.quarterly1Month),
    quarterly1Day: pick(PERIOD.quarterly, value.quarterly1Day),
    quarterly2Month: pick(PERIOD.quarterly, value.quarterly2Month),
    quarterly2Day: pick(PERIOD.quarterly, value.quarterly2Day),
    quarterly3Month: pick(PERIOD.quarterly, value.quarterly3Month),
    quarterly3Day: pick(PERIOD.quarterly, value.quarterly3Day),
    quarterly4Month: pick(PERIOD.quarterly, value.quarterly4Month),
    quarterly4Day: pick(PERIOD.quarterly, value.quarterly4Day),
    semiAnnual1Month: pick(PERIOD.semiAnnual, value.semiAnnual1Month),
    semiAnnual1Day: pick(PERIOD.semiAnnual, value.semiAnnual1Day),
    semiAnnual2Month: pick(PERIOD.semiAnnual, value.semiAnnual2Month),
    semiAnnual2Day: pick(PERIOD.semiAnnual, value.semiAnnual2Day),
    annualMoy: pick(PERIOD.annual, value.annualMoy),
    annualDom: pick(PERIOD.annual, value.annualDom),
    nthDow: pick(PERIOD.nthWeekday, value.nthDow),
    nthIndex: pick(PERIOD.nthWeekday, value.nthIndex),
    dateRangeReq,
  };
}

/** The list page for an item type. */
export function itemListPath(itemType: number): string {
  return itemType === 1 ? '/credits' : '/debits';
}
