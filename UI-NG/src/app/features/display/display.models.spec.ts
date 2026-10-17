import { addDays, addMonths, summarize, toLedgerRequest, validateRange } from './display.models';
import { makeDay, sampleLedger } from './testing-ledger';

describe('display models', () => {
  const d = (y: number, m: number, day: number) => new Date(y, m - 1, day);

  it('builds the request with ISO dates', () => {
    expect(toLedgerRequest(d(2026, 10, 17), d(2027, 1, 15))).toEqual({
      ledgerStartDate: '2026-10-17',
      ledgerEndDate: '2027-01-15',
      includeGrouping: false,
    });
  });

  it('adds days and months, clamping to the end of short months', () => {
    expect(addDays(d(2026, 10, 17), 90)).toEqual(d(2027, 1, 15));
    expect(addMonths(d(2026, 1, 31), 1)).toEqual(d(2026, 2, 28));
    expect(addMonths(d(2026, 10, 17), 12)).toEqual(d(2027, 10, 17));
  });

  it('validates the range like the API does', () => {
    expect(validateRange(d(2026, 10, 17), d(2026, 10, 17))).toBeNull();
    expect(validateRange(null, d(2026, 10, 17))).toContain('Choose');
    expect(validateRange(d(2026, 10, 17), d(2026, 10, 16))).toContain('on or after');
    expect(validateRange(d(2026, 1, 1), d(2036, 1, 1))).toBeNull();
    expect(validateRange(d(2026, 1, 1), d(2036, 1, 2))).toContain('10 years');
  });

  it('summarizes totals, the lowest point and the first negative day', () => {
    const days = [
      makeDay(1, '2026-10-17', 100),
      makeDay(2, '2026-10-18', -50, [{ amount: -150 }]),
      makeDay(3, '2026-10-19', 450, [{ amount: 500 }]),
    ];

    expect(summarize(days)).toEqual({
      startingBalance: 100,
      totalCredits: 500,
      totalDebits: -150,
      endingBalance: 450,
      lowest: { amount: -50, date: '2026-10-18' },
      firstNegativeDate: '2026-10-18',
      activeDays: 2,
    });
  });

  it('works out the starting balance from the first day', () => {
    const summary = summarize(sampleLedger())!;
    expect(summary.startingBalance).toBe(1000);
    expect(summary.firstNegativeDate).toBeNull();
    expect(summarize([])).toBeNull();
  });
});
