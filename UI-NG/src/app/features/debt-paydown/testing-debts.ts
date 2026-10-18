import { ITEM_TYPE, PERIOD } from '../item-detail/item';
import { makeItem } from '../item-detail/testing-items';
import { Debt } from './debt-paydown.models';

const debit = { id: ITEM_TYPE.debit, name: 'Debit' };

export const chaseVisa = makeItem(PERIOD.monthly, { id: 10, name: 'Chase Visa', amount: 200, monthlyDom: 15, itemType: debit });
export const carLoan = makeItem(PERIOD.monthly, { id: 11, name: 'Car Loan', amount: 350, monthlyDom: 1, itemType: debit });
export const paycheck = makeItem(PERIOD.everyTwoWeeks, { id: 20, name: 'Paycheck', amount: 2000, everyOtherWeekDow: 5, beginDate: '2026-10-02' });
export const bonus = makeItem(PERIOD.monthly, { id: 21, name: 'Bonus', amount: 1000, monthlyDom: 1 });

/** Chase Visa: $4,500 at 24.99%, with 70% of each paycheck. */
export function chaseDebt(overrides: Partial<Debt> = {}): Debt {
  return {
    id: 1,
    itemId: 10,
    name: 'Chase Visa',
    payment: 200,
    period: 'Monthly',
    balance: 4500,
    apr: 24.99,
    balanceDate: '2026-10-18',
    allocations: [
      { id: 1, creditItemId: 20, creditName: 'Paycheck', creditAmount: 2000, creditPeriod: 'Every Two Weeks', percent: 70, perOccurrence: 1400 },
    ],
    ...overrides,
  };
}
