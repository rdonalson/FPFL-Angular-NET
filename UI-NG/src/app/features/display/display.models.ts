import { fromIsoDate, toIsoDate } from '../item-detail/item-form';

/** One credit or debit on a ledger day (API's LedgerItemResponse, same shape as UI-TSX's ItemDto). */
export interface LedgerItem {
  itemKey: number;
  itemId: number;
  rollupKey: number;
  year: number;
  /** 'yyyy-MM-dd' */
  occurrenceDate: string;
  fkItemType: number;
  itemType: string | null;
  period: string | null;
  name: string | null;
  /** Signed: credits positive, debits negative. */
  amount: number;
}

/** One day of the forecast (API's LedgerDayResponse, same shape as UI-TSX's LedgerDto). */
export interface LedgerDay {
  rollupKey: number;
  year: number;
  /** 'yyyy-MM-dd' */
  wDate: string;
  creditSummary: number;
  /** Zero or negative. */
  debitSummary: number;
  net: number;
  /** Balance at the end of the day. */
  runningTotal: number;
  items: LedgerItem[];
}

/** Body of POST /display/ledger. The API takes the user from the access token. */
export interface LedgerRequest {
  ledgerStartDate: string;
  ledgerEndDate: string;
  includeGrouping: boolean;
}

/** Same cap as the API (ILedgerReadoutService.MaxYears). */
export const MAX_YEARS = 10;

/** Quick ranges, counted from the start date (UI-TSX defaults to 90 days). */
export const RANGE_PRESETS: readonly { label: string; months?: number; days?: number }[] = [
  { label: '30 days', days: 30 },
  { label: '90 days', days: 90 },
  { label: '6 months', months: 6 },
  { label: '1 year', months: 12 },
  { label: '5 years', months: 60 },
];

export function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

/** Calendar months later, pulled back to the month's last day when needed (Jan 31 + 1 month = Feb 28). */
export function addMonths(date: Date, months: number): Date {
  const target = new Date(date.getFullYear(), date.getMonth() + months, 1);
  const lastDay = new Date(target.getFullYear(), target.getMonth() + 1, 0).getDate();
  return new Date(target.getFullYear(), target.getMonth(), Math.min(date.getDate(), lastDay));
}

export function today(): Date {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}

/** Null when the range is fine, otherwise what's wrong with it (mirrors the API's checks). */
export function validateRange(start: Date | null, end: Date | null): string | null {
  if (!start || !end) {
    return 'Choose a start and an end date.';
  }
  if (end < start) {
    return 'The end date must be on or after the start date.';
  }
  if (end > addMonths(start, MAX_YEARS * 12)) {
    return `A forecast can cover at most ${MAX_YEARS} years.`;
  }
  return null;
}

export function toLedgerRequest(start: Date, end: Date): LedgerRequest {
  return { ledgerStartDate: toIsoDate(start)!, ledgerEndDate: toIsoDate(end)!, includeGrouping: false };
}

export interface LedgerSummary {
  startingBalance: number;
  totalCredits: number;
  /** Zero or negative. */
  totalDebits: number;
  endingBalance: number;
  lowest: { amount: number; date: string };
  /** First day the balance ends below zero, if it ever does. */
  firstNegativeDate: string | null;
  /** Days with at least one credit or debit. */
  activeDays: number;
}

/** Headline numbers for the stat tiles. Null for an empty ledger. */
export function summarize(days: readonly LedgerDay[]): LedgerSummary | null {
  if (days.length === 0) {
    return null;
  }

  let lowest = { amount: days[0].runningTotal, date: days[0].wDate };
  let totalCredits = 0;
  let totalDebits = 0;
  let activeDays = 0;
  let firstNegativeDate: string | null = null;

  for (const day of days) {
    totalCredits += day.creditSummary;
    totalDebits += day.debitSummary;
    if (day.items.length > 0) {
      activeDays++;
    }
    if (day.runningTotal < lowest.amount) {
      lowest = { amount: day.runningTotal, date: day.wDate };
    }
    if (firstNegativeDate === null && day.runningTotal < 0) {
      firstNegativeDate = day.wDate;
    }
  }

  return {
    startingBalance: round(days[0].runningTotal - days[0].net),
    totalCredits: round(totalCredits),
    totalDebits: round(totalDebits),
    endingBalance: days[days.length - 1].runningTotal,
    lowest,
    firstNegativeDate,
    activeDays,
  };
}

/** 'yyyy-MM-dd' → "Sat, Oct 17" style label for chart axes. */
export function shortDay(iso: string): string {
  const date = fromIsoDate(iso);
  return date ? shortDayFormat.format(date) : iso;
}

/** 'yyyy-MM-dd' → "Sat, Oct 17, 2026" for the ledger table. */
export function longDay(iso: string): string {
  const date = fromIsoDate(iso);
  return date ? longDayFormat.format(date) : iso;
}

const shortDayFormat = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric' });
const longDayFormat = new Intl.DateTimeFormat('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' });

/** Sums of cents-exact API values can pick up float noise; bring them back to cents. */
function round(value: number): number {
  return Math.round(value * 100) / 100;
}
