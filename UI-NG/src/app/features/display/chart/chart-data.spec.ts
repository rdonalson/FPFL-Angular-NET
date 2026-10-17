import { sampleLedger } from '../testing-ledger';
import { balanceChart, flowChart } from './chart-data';

describe('chart data', () => {
  it('plots the end-of-day balance with short date labels', () => {
    const { data } = balanceChart(sampleLedger());

    expect(data.labels).toEqual(['Oct 17', 'Oct 18', 'Oct 19']);
    expect(data.datasets).toHaveLength(1);
    expect(data.datasets[0].data).toEqual([1000, 1300, 1300]);
  });

  it('stacks credits above zero and debits below', () => {
    const { data, options } = flowChart(sampleLedger());

    expect(data.datasets.map((d) => d.label)).toEqual(['Credits', 'Debits']);
    expect(data.datasets[0].data).toEqual([0, 2500, 0]);
    expect(data.datasets[1].data).toEqual([0, -2200, 0]);
    expect(options.scales?.['x']?.stacked).toBe(true);
    expect(options.scales?.['y']?.stacked).toBe(true);
  });
});
