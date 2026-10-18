import {
  allocatedElsewhere,
  emptyDebtForm,
  formatPercent,
  newAllocation,
  perOccurrence,
  toDebtForm,
  toDebtRequest,
  validateDebtForm,
} from './debt-paydown.models';
import { chaseDebt } from './testing-debts';

describe('debt paydown models', () => {
  const nameMap: Record<number, string> = { 20: 'Paycheck', 21: 'Bonus' };
  const names = (id: number) => nameMap[id] ?? 'That credit';
  const valid = () => ({ ...emptyDebtForm(new Date(2026, 9, 18)), itemId: 11, balance: 1000 });

  it('works out each occurrence’s share in cents', () => {
    expect(perOccurrence(2000, 25)).toBe(500);
    expect(perOccurrence(2000, 33.33)).toBe(666.6);
    expect(perOccurrence(null, 50)).toBe(0);
  });

  it('round-trips a debt through the form and builds the request', () => {
    const form = toDebtForm(chaseDebt());
    expect(form.balanceDate).toEqual(new Date(2026, 9, 18));

    expect(toDebtRequest({ ...form, apr: null })).toEqual({
      itemId: 10,
      balance: 4500,
      apr: 0,
      balanceDate: '2026-10-18',
      allocations: [{ creditItemId: 20, percent: 70 }],
    });
  });

  it('counts what other debts take, ignoring the one being edited', () => {
    const debts = [chaseDebt(), chaseDebt({ id: 2, itemId: 11, allocations: [{ ...chaseDebt().allocations[0], id: 2, percent: 20 }] })];

    expect(allocatedElsewhere(debts, null).get(20)).toBe(90);
    expect(allocatedElsewhere(debts, 1).get(20)).toBe(20);
  });

  it('accepts a complete form', () => {
    const form = { ...valid(), allocations: [newAllocation(20, 30)] };
    expect(validateDebtForm(form, new Map([[20, 70]]), names)).toBeNull();
  });

  it('explains what is missing or wrong', () => {
    const elsewhere = new Map([[20, 70]]);
    expect(validateDebtForm({ ...valid(), itemId: null }, elsewhere, names)).toContain('Choose the debit');
    expect(validateDebtForm({ ...valid(), balance: -5 }, elsewhere, names)).toContain('balance owed');
    expect(validateDebtForm({ ...valid(), apr: 120 }, elsewhere, names)).toContain('APR');
    expect(validateDebtForm({ ...valid(), allocations: [newAllocation()] }, elsewhere, names)).toContain('Choose a credit');
    expect(validateDebtForm({ ...valid(), allocations: [newAllocation(21, 0)] }, elsewhere, names)).toContain('percentage of Bonus');
    expect(validateDebtForm({ ...valid(), allocations: [newAllocation(21, 10), newAllocation(21, 10)] }, elsewhere, names)).toContain(
      'more than once',
    );
    expect(validateDebtForm({ ...valid(), allocations: [newAllocation(20, 40)] }, elsewhere, names)).toBe(
      'Paycheck is already 70% allocated to other debts; at most 30% is left.',
    );
  });

  it('formats percentages without trailing zeros', () => {
    expect(formatPercent(25)).toBe('25%');
    expect(formatPercent(33.333)).toBe('33.33%');
    expect(formatPercent(24.99)).toBe('24.99%');
  });
});
