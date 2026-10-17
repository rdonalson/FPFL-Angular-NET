import { PERIOD } from './item';
import { describeDateRange, describeSchedule, formatDate, ordinal } from './schedule';
import { makeItem } from './testing-items';

describe('ordinal', () => {
  it.each([
    [1, '1st'],
    [2, '2nd'],
    [3, '3rd'],
    [4, '4th'],
    [11, '11th'],
    [12, '12th'],
    [13, '13th'],
    [21, '21st'],
    [22, '22nd'],
    [31, '31st'],
  ])('%i → %s', (n, expected) => {
    expect(ordinal(n)).toBe(expected);
  });
});

describe('formatDate', () => {
  it('formats yyyy-MM-dd without shifting the day', () => {
    expect(formatDate('2026-01-01')).toBe('Jan 1, 2026');
  });

  it('returns an empty string for null', () => {
    expect(formatDate(null)).toBe('');
  });
});

describe('describeSchedule', () => {
  it.each([
    [makeItem(PERIOD.oneTime, { beginDate: '2026-03-15' }), 'Once, on Mar 15, 2026'],
    [makeItem(PERIOD.daily), 'Every day'],
    [makeItem(PERIOD.weekly, { weeklyDow: 5 }), 'Every Friday'],
    [makeItem(PERIOD.everyTwoWeeks, { everyOtherWeekDow: 0 }), 'Every other Sunday'],
    [makeItem(PERIOD.biMonthly, { biMonthlyDay1: 1, biMonthlyDay2: 15 }), 'On the 1st and 15th'],
    [makeItem(PERIOD.monthly, { monthlyDom: 22 }), 'Monthly on the 22nd'],
    [
      makeItem(PERIOD.quarterly, {
        quarterly1Month: 1, quarterly1Day: 15,
        quarterly2Month: 4, quarterly2Day: 15,
        quarterly3Month: 7, quarterly3Day: 15,
        quarterly4Month: 10, quarterly4Day: 15,
      }),
      'Quarterly: Jan 15, Apr 15, Jul 15, Oct 15',
    ],
    [
      makeItem(PERIOD.semiAnnual, { semiAnnual1Month: 1, semiAnnual1Day: 1, semiAnnual2Month: 7, semiAnnual2Day: 1 }),
      'Twice a year: Jan 1 and Jul 1',
    ],
    [makeItem(PERIOD.annual, { annualMoy: 12, annualDom: 25 }), 'Every year on Dec 25'],
    [makeItem(PERIOD.nthWeekday, { nthDow: 2, nthIndex: 2 }), '2nd Tuesday of the month'],
    [makeItem(PERIOD.nthWeekday, { nthDow: 5, nthIndex: -1 }), 'Last Friday of the month'],
  ])('%#: %s', (item, expected) => {
    expect(describeSchedule(item)).toBe(expected);
  });

  it('falls back to a plain description when details are missing', () => {
    expect(describeSchedule(makeItem(PERIOD.weekly))).toBe('Weekly');
    expect(describeSchedule(makeItem(PERIOD.monthly))).toBe('Monthly');
  });

  it('uses the period name for unknown periods', () => {
    expect(describeSchedule(makeItem(42))).toBe('Other');
  });
});

describe('describeDateRange', () => {
  it('shows both ends of a range', () => {
    expect(describeDateRange(makeItem(PERIOD.monthly, { beginDate: '2026-01-01', endDate: '2026-12-31' }))).toBe(
      'Jan 1, 2026 – Dec 31, 2026',
    );
  });

  it('handles open-ended ranges', () => {
    expect(describeDateRange(makeItem(PERIOD.monthly, { beginDate: '2026-01-01' }))).toBe('From Jan 1, 2026');
    expect(describeDateRange(makeItem(PERIOD.monthly, { endDate: '2026-06-30' }))).toBe('Until Jun 30, 2026');
    expect(describeDateRange(makeItem(PERIOD.monthly))).toBe('Ongoing');
  });

  it('shows the single date for one-time items', () => {
    expect(describeDateRange(makeItem(PERIOD.oneTime, { beginDate: '2026-03-15' }))).toBe('Mar 15, 2026');
  });
});
