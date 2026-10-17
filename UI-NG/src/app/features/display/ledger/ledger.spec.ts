import { ComponentFixture, TestBed } from '@angular/core/testing';

import { sampleLedger } from '../testing-ledger';
import { Ledger } from './ledger';

describe('Ledger', () => {
  let fixture: ComponentFixture<Ledger>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [Ledger] }).compileComponents();
    fixture = TestBed.createComponent(Ledger);
    fixture.componentRef.setInput('days', sampleLedger());
    fixture.detectChanges();
    await fixture.whenStable();
  });

  const el = () => fixture.nativeElement as HTMLElement;
  const bodyRows = () => el().querySelectorAll('tbody tr');

  it('lists every day with its balance', () => {
    expect(bodyRows()).toHaveLength(3);
    expect(el().textContent).toContain('Sat, Oct 17, 2026');
    expect(el().textContent).toContain('Sun, Oct 18, 2026');
    expect(el().textContent).toContain('2 items');
    expect(el().textContent).toContain('$300.00'); // net on the 18th
  });

  it('expands a day to show its items', async () => {
    expect(el().textContent).not.toContain('Paycheck');

    el().querySelector<HTMLButtonElement>('button[aria-label^="Show items"]')!.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(el().textContent).toContain('Paycheck');
    expect(el().textContent).toContain('Every Two Weeks');
    expect(el().textContent).toContain('Rent');
    expect(el().textContent).toContain('-$2,200.00');
  });

  it('can hide days without activity', async () => {
    el().querySelector<HTMLInputElement>('#ledger-active-only')!.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(bodyRows()).toHaveLength(1);
  });
});
