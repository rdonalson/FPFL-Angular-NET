import { ComponentFixture, TestBed } from '@angular/core/testing';

import { sampleLedger, samplePaydown } from '../testing-ledger';
import { PaydownChart } from './paydown-chart';

describe('PaydownChart', () => {
  let fixture: ComponentFixture<PaydownChart>;

  // jsdom has no canvas for Chart.js to draw on.
  const realGetContext = HTMLCanvasElement.prototype.getContext;
  beforeAll(() => (HTMLCanvasElement.prototype.getContext = (() => null) as unknown as typeof realGetContext));
  afterAll(() => (HTMLCanvasElement.prototype.getContext = realGetContext));

  async function render(debts = [samplePaydown()]) {
    await TestBed.configureTestingModule({ imports: [PaydownChart] }).compileComponents();
    fixture = TestBed.createComponent(PaydownChart);
    fixture.componentRef.setInput('debts', debts);
    fixture.componentRef.setInput('days', sampleLedger());
    fixture.detectChanges();
    await fixture.whenStable();
  }

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('summarizes each debt: owed, paid and when it is paid off', async () => {
    await render();

    expect(text()).toContain('Chase Visa');
    expect(text()).toContain('$1,000.00 → $175.00');
    expect(text()).toContain('$825.00');
    expect(text()).toContain('Paid off Nov 18, 2026 (after this range)');
    expect(text()).not.toContain('Interest');
  });

  it('shows interest and warns when a debt is never paid off', async () => {
    await render([samplePaydown({ totalInterest: 36.36, payoffDate: null })]);

    expect(text()).toContain('$36.36');
    expect(text()).toContain('Not paid off within 30 years');
  });
});
