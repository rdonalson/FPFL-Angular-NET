import type { ChartData, ChartOptions, TooltipItem } from 'chart.js';

import { LedgerDay, shortDay } from '../display.models';

/**
 * Chart.js configs for the Display page. Two charts instead of UI-TSX's one, because the balance
 * (thousands) and a day's credits/debits (tens) don't share a scale, and one axis per chart keeps both
 * honest. Colors passed a color-blindness check, and position (above or below zero) also tells
 * credits from debits.
 */
export const CHART_COLORS = {
  balance: '#2563eb',
  balanceFill: 'rgba(37, 99, 235, 0.08)',
  credit: '#059669',
  debit: '#dc2626',
  grid: '#eef0f3',
  zero: '#9ca3af',
  text: '#4b5563',
} as const;

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
const compactMoney = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', notation: 'compact', maximumFractionDigits: 1 });

export function formatMoney(value: number): string {
  return money.format(value);
}

function baseOptions(): ChartOptions<'line' | 'bar'> {
  return {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      tooltip: {
        callbacks: {
          title: (items: TooltipItem<'line' | 'bar'>[]) => items[0]?.label ?? '',
          label: (item: TooltipItem<'line' | 'bar'>) => ` ${item.dataset.label}: ${formatMoney(Number(item.raw))}`,
        },
      },
    },
    scales: {
      x: {
        grid: { display: false },
        ticks: { color: CHART_COLORS.text, autoSkip: true, maxTicksLimit: 8, maxRotation: 0 },
      },
      y: {
        grid: {
          // The zero line stands out; the rest stay faint.
          color: (context) => (context.tick?.value === 0 ? CHART_COLORS.zero : CHART_COLORS.grid),
        },
        border: { display: false },
        ticks: { color: CHART_COLORS.text, maxTicksLimit: 6, callback: (value) => compactMoney.format(Number(value)) },
      },
    },
  };
}

/** The headline: balance at the end of each day. One series, so no legend. */
export function balanceChart(days: readonly LedgerDay[]): { data: ChartData<'line'>; options: ChartOptions<'line'> } {
  const options = baseOptions() as ChartOptions<'line'>;
  options.plugins = { ...options.plugins, legend: { display: false } };

  return {
    data: {
      labels: days.map((day) => shortDay(day.wDate)),
      datasets: [
        {
          label: 'Balance',
          data: days.map((day) => day.runningTotal),
          borderColor: CHART_COLORS.balance,
          backgroundColor: CHART_COLORS.balanceFill,
          borderWidth: 2,
          fill: 'origin',
          pointRadius: 0,
          pointHoverRadius: 5,
          pointHoverBorderWidth: 2,
          pointHoverBorderColor: '#ffffff',
          pointHoverBackgroundColor: CHART_COLORS.balance,
          tension: 0,
        },
      ],
    },
    options,
  };
}

/** Money in above zero, money out below, one slot per day. */
export function flowChart(days: readonly LedgerDay[]): { data: ChartData<'bar'>; options: ChartOptions<'bar'> } {
  const options = baseOptions() as ChartOptions<'bar'>;
  options.plugins = {
    ...options.plugins,
    legend: { display: true, position: 'top', align: 'end', labels: { boxWidth: 12, boxHeight: 12, color: CHART_COLORS.text } },
  };
  options.scales = {
    ...options.scales,
    x: { ...options.scales!['x'], stacked: true },
    y: { ...options.scales!['y'], stacked: true },
  };

  const bar = { borderRadius: 4, borderSkipped: 'start' as const, maxBarThickness: 14, categoryPercentage: 0.9, barPercentage: 0.9 };

  return {
    data: {
      labels: days.map((day) => shortDay(day.wDate)),
      datasets: [
        { label: 'Credits', data: days.map((day) => day.creditSummary), backgroundColor: CHART_COLORS.credit, ...bar },
        { label: 'Debits', data: days.map((day) => day.debitSummary), backgroundColor: CHART_COLORS.debit, ...bar },
      ],
    },
    options,
  };
}
