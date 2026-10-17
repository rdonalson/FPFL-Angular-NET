import { PERIOD } from './item';
import { ItemFormValue, emptyItemForm, fromIsoDate, toFormValue, toIsoDate, toItemRequest, validateItemForm } from './item-form';
import { makeItem } from './testing-items';

const form = (periodId: number, overrides: Partial<ItemFormValue> = {}): ItemFormValue => ({
  ...emptyItemForm(periodId),
  name: '  Side Gig ',
  amount: 250,
  ...overrides,
});

describe('date helpers', () => {
  it('round-trips yyyy-MM-dd through a local Date without shifting the day', () => {
    expect(toIsoDate(fromIsoDate('2026-01-01'))).toBe('2026-01-01');
    expect(toIsoDate(new Date(2026, 10, 6))).toBe('2026-11-06');
    expect(toIsoDate(null)).toBeNull();
    expect(fromIsoDate(null)).toBeNull();
  });
});

describe('validateItemForm', () => {
  it('accepts a complete monthly item', () => {
    expect(validateItemForm(form(PERIOD.monthly, { monthlyDom: 15 }))).toBeNull();
  });

  it.each<[string, ItemFormValue, string]>([
    ['blank name', form(PERIOD.daily, { name: '  ' }), 'Name is required.'],
    ['zero amount', form(PERIOD.daily, { amount: 0 }), 'Amount must be greater than zero.'],
    ['no period', form(PERIOD.daily, { periodId: null }), 'Choose how often it repeats.'],
    ['one-time without date', form(PERIOD.oneTime), 'Choose the date.'],
    ['weekly without day', form(PERIOD.weekly), 'Choose the day of the week.'],
    ['every two weeks without start', form(PERIOD.everyTwoWeeks, { everyOtherWeekDow: 5 }), 'Choose the first date it happens.'],
    ['bi-monthly same days', form(PERIOD.biMonthly, { biMonthlyDay1: 15, biMonthlyDay2: 15 }), 'The two days must be different.'],
    ['quarterly incomplete', form(PERIOD.quarterly, { quarterly1Month: 1, quarterly1Day: 15 }), 'Choose a month and day for all four quarters.'],
    ['nth weekday incomplete', form(PERIOD.nthWeekday, { nthDow: 5 }), 'Choose which weekday and which week.'],
    [
      'range missing end',
      form(PERIOD.daily, { dateRangeReq: true, beginDate: new Date(2026, 0, 1) }),
      'Choose both the start and end dates, or turn off the date range.',
    ],
    [
      'range backwards',
      form(PERIOD.daily, { dateRangeReq: true, beginDate: new Date(2026, 5, 1), endDate: new Date(2026, 0, 1) }),
      'The start date must be on or before the end date.',
    ],
  ])('%s', (_, value, expected) => {
    expect(validateItemForm(value)).toBe(expected);
  });
});

describe('toItemRequest', () => {
  it('sends only the fields for the chosen period and trims the name', () => {
    const request = toItemRequest(
      form(PERIOD.monthly, { monthlyDom: 15, weeklyDow: 3, annualMoy: 6, beginDate: new Date(2026, 0, 1) }),
      2,
    );

    expect(request).toMatchObject({ name: 'Side Gig', amount: 250, fkItemType: 2, fkPeriod: PERIOD.monthly, monthlyDom: 15 });
    expect(request.weeklyDow).toBeNull();
    expect(request.annualMoy).toBeNull();
    expect(request.beginDate).toBeNull(); // no date range asked for
  });

  it('sends the date range only when it is turned on', () => {
    const request = toItemRequest(
      form(PERIOD.daily, { dateRangeReq: true, beginDate: new Date(2026, 0, 1), endDate: new Date(2026, 11, 31) }),
      1,
    );

    expect(request).toMatchObject({ dateRangeReq: true, beginDate: '2026-01-01', endDate: '2026-12-31' });
  });

  it('one-time items send just their date', () => {
    const request = toItemRequest(form(PERIOD.oneTime, { beginDate: new Date(2026, 3, 15), endDate: new Date(2026, 4, 1) }), 1);

    expect(request).toMatchObject({ dateRangeReq: true, beginDate: '2026-04-15', endDate: null });
  });

  it('every-two-weeks always sends its start date', () => {
    const request = toItemRequest(form(PERIOD.everyTwoWeeks, { everyOtherWeekDow: 5, beginDate: new Date(2026, 10, 6) }), 1);

    expect(request).toMatchObject({ everyOtherWeekDow: 5, beginDate: '2026-11-06', endDate: null, dateRangeReq: false });
  });
});

describe('toFormValue', () => {
  it('turns an item into form values', () => {
    const value = toFormValue(makeItem(PERIOD.annual, { name: 'Insurance', amount: 600, annualMoy: 3, annualDom: 1 }));

    expect(value).toMatchObject({ name: 'Insurance', amount: 600, periodId: PERIOD.annual, annualMoy: 3, annualDom: 1 });
  });
});
