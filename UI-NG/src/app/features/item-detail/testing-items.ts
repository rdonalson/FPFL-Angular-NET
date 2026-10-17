import { Item, PERIOD } from './item';

const periodNames: Record<number, string> = {
  [PERIOD.oneTime]: 'One Time',
  [PERIOD.daily]: 'Daily',
  [PERIOD.weekly]: 'Weekly',
  [PERIOD.everyTwoWeeks]: 'Every Two Weeks',
  [PERIOD.biMonthly]: 'Bi-Monthly',
  [PERIOD.monthly]: 'Monthly',
  [PERIOD.quarterly]: 'Quarterly',
  [PERIOD.semiAnnual]: 'Semi-Annual',
  [PERIOD.annual]: 'Annual',
  [PERIOD.nthWeekday]: 'Nth Weekday',
};

/** Test helper: an item with every recurrence field empty, plus overrides. */
export function makeItem(periodId: number, overrides: Partial<Item> = {}): Item {
  return {
    id: 1,
    userId: '8fdbe29e-f25f-450d-b179-92973e2bf7ba',
    name: 'Salary',
    amount: 1000,
    itemType: { id: 1, name: 'Credit' },
    timePeriod: { id: periodId, name: periodNames[periodId] ?? 'Other' },
    beginDate: null,
    endDate: null,
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
    ...overrides,
  };
}
