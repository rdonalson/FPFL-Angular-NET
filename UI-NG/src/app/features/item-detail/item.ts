/** Item type ids in fpfl.item_types. */
export const ITEM_TYPE = { credit: 1, debit: 2 } as const;
export type ItemTypeId = (typeof ITEM_TYPE)[keyof typeof ITEM_TYPE];

/** Time period ids in fpfl.time_periods (same numbering as UI-TSX's add/edit pages). */
export const PERIOD = {
  oneTime: 1,
  daily: 2,
  weekly: 3,
  everyTwoWeeks: 4,
  biMonthly: 5,
  monthly: 6,
  quarterly: 7,
  semiAnnual: 8,
  annual: 9,
  nthWeekday: 10,
} as const;

export interface Lookup {
  id: number;
  name: string;
}

/**
 * Item as returned by GET /items/{userId}/{itemType}. Dates are 'yyyy-MM-dd'; weekdays are
 * 0 = Sunday … 6 = Saturday; months are 1 = January; nthIndex is 1–4, or -1 for "last".
 */
export interface Item {
  id: number;
  userId: string;
  name: string;
  /** Always positive; the item type says whether it is a credit or a debit. */
  amount: number;
  itemType: Lookup | null;
  timePeriod: Lookup | null;
  beginDate: string | null;
  endDate: string | null;
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
}

/** Body of POST /items and PUT /items/{id}. The owner comes from the access token, never the body. */
export interface ItemRequest {
  name: string;
  amount: number;
  fkItemType: number;
  fkPeriod: number;
  beginDate: string | null;
  endDate: string | null;
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
}

export interface PeriodOption {
  id: number;
  label: string;
  icon: string;
  example: string;
}

/** The ten recurrence choices, in the order the period picker shows them. */
export const PERIODS: readonly PeriodOption[] = [
  { id: PERIOD.oneTime, label: 'One Time', icon: 'pi pi-calendar', example: 'A single date, like a tax refund' },
  { id: PERIOD.daily, label: 'Daily', icon: 'pi pi-sun', example: 'Every day, like a commute' },
  { id: PERIOD.weekly, label: 'Weekly', icon: 'pi pi-calendar-clock', example: 'Every Friday' },
  { id: PERIOD.everyTwoWeeks, label: 'Every Two Weeks', icon: 'pi pi-sync', example: 'Every other Friday, like a paycheck' },
  { id: PERIOD.biMonthly, label: 'Bi-Monthly', icon: 'pi pi-clone', example: 'Twice a month, on the 1st and 15th' },
  { id: PERIOD.monthly, label: 'Monthly', icon: 'pi pi-calendar-plus', example: 'Once a month, like rent' },
  { id: PERIOD.quarterly, label: 'Quarterly', icon: 'pi pi-th-large', example: 'Four dates a year, like estimated taxes' },
  { id: PERIOD.semiAnnual, label: 'Semi-Annual', icon: 'pi pi-arrows-h', example: 'Two dates a year, like insurance' },
  { id: PERIOD.annual, label: 'Annual', icon: 'pi pi-star', example: 'Once a year, like a membership' },
  { id: PERIOD.nthWeekday, label: 'Nth Weekday', icon: 'pi pi-list', example: 'The 2nd Tuesday or last Friday of the month' },
];

export interface Option {
  label: string;
  value: number;
}

export const WEEKDAY_OPTIONS: readonly Option[] = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
  .map((label, value) => ({ label, value }));

export const MONTH_OPTIONS: readonly Option[] = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
].map((label, index) => ({ label, value: index + 1 }));

export const DAY_OPTIONS: readonly Option[] = Array.from({ length: 31 }, (_, index) => ({
  label: String(index + 1),
  value: index + 1,
}));

export const NTH_OPTIONS: readonly Option[] = [
  { label: '1st', value: 1 },
  { label: '2nd', value: 2 },
  { label: '3rd', value: 3 },
  { label: '4th', value: 4 },
  { label: 'Last', value: -1 },
];
