import { sampleLedger, samplePaydown } from '../testing-ledger';
import { PAYDOWN_COLORS, describeEvent, paydownChart, payoffStatus } from './paydown.models';

describe('paydown models', () => {
  it('describes each kind of activity', () => {
    const [payment, allocation] = samplePaydown().events;
    expect(describeEvent(payment)).toBe('Scheduled payment');
    expect(describeEvent(allocation)).toBe('25% of Paycheck');
    expect(describeEvent({ ...payment, kind: 'interest' })).toBe('Interest');
    expect(describeEvent({ ...allocation, percent: 33.333 })).toBe('33.33% of Paycheck');
  });

  it('places the payoff inside, after or beyond the range', () => {
    expect(payoffStatus(samplePaydown({ payoffDate: '2026-10-18' }), '2026-10-19')).toBe('paid');
    expect(payoffStatus(samplePaydown(), '2026-10-19')).toBe('later');
    expect(payoffStatus(samplePaydown({ payoffDate: null }), '2026-10-19')).toBe('never');
  });

  it('charts one stepped line per debt, in fixed colors, with a legend only for several', () => {
    const one = paydownChart([samplePaydown()], sampleLedger());
    expect(one.data.labels).toEqual(['Oct 17', 'Oct 18', 'Oct 19']);
    expect(one.data.datasets[0].data).toEqual([1000, 175, 175]);
    expect(one.data.datasets[0].borderColor).toBe(PAYDOWN_COLORS[0]);
    expect(one.options.plugins?.legend?.display).toBe(false);

    const two = paydownChart([samplePaydown(), samplePaydown({ debtId: 2, name: 'Car Loan' })], sampleLedger());
    expect(two.data.datasets.map((d) => d.label)).toEqual(['Chase Visa', 'Car Loan']);
    expect(two.data.datasets[1].borderColor).toBe(PAYDOWN_COLORS[1]);
    expect(two.options.plugins?.legend?.display).toBe(true);
  });
});
