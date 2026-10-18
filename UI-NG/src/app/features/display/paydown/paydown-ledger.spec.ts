import { ComponentFixture, TestBed } from '@angular/core/testing';

import { samplePaydown } from '../testing-ledger';
import { PaydownLedger } from './paydown-ledger';

describe('PaydownLedger', () => {
  let fixture: ComponentFixture<PaydownLedger>;

  const carLoan = samplePaydown({
    debtId: 2,
    name: 'Car Loan',
    events: [
      { date: '2026-10-17', kind: 'interest', sourceItemId: null, sourceName: null, percent: null, amount: 12, balanceAfter: 3012 },
      { date: '2026-10-19', kind: 'payment', sourceItemId: 11, sourceName: 'Car Loan', percent: null, amount: -3012, balanceAfter: 0 },
    ],
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [PaydownLedger] }).compileComponents();
    fixture = TestBed.createComponent(PaydownLedger);
    fixture.componentRef.setInput('debts', [samplePaydown(), carLoan]);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  const rows = () => Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr')).map((r) => r.textContent ?? '');

  it('lists every debt’s activity in date order with the balance left', () => {
    const all = rows();
    expect(all).toHaveLength(4);
    expect(all[0]).toContain('Car Loan');
    expect(all[0]).toContain('Interest');
    expect(all[0]).toContain('+$12.00');
    expect(all[1]).toContain('Scheduled payment');
    expect(all[2]).toContain('25% of Paycheck');
    expect(all[2]).toContain('$625.00');
    expect(all[2]).toContain('$175.00');
    expect(all[3]).toContain('Paid off');
  });

  it('filters to one debt', async () => {
    (fixture.componentInstance as unknown as { debtFilter: { set(v: number | null): void } }).debtFilter.set(2);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(rows()).toHaveLength(2);
    expect(rows().every((r) => r.includes('Car Loan'))).toBe(true);
  });
});
