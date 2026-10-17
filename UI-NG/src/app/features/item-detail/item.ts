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
