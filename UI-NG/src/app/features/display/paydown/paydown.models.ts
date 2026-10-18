import type { ChartData, ChartOptions, TooltipItem } from 'chart.js';

import { formatMoney } from '../chart/chart-data';
import { LedgerDay, shortDay } from '../display.models';

/** One change to a debt's balance (API's PaydownEventResponse). Payments are negative, interest positive. */
export interface PaydownEvent {
  /** 'yyyy-MM-dd' */
  date: string;
  kind: 'payment' | 'allocation' | 'interest';
  sourceItemId: number | null;
  sourceName: string | null;
  percent: number | null;
  amount: number;
  balanceAfter: number;
}

/** A tracked debt over the forecast window (API's DebtPaydownResponse). */
export interface DebtPaydown {
  debtId: number;
  itemId: number;
  name: string | null;
  apr: number;
  enteredBalance: number;
  balanceDate: string;
  startingBalance: number;
  endingBalance: number;
  totalPaid: number;
  totalInterest: number;
  /** May fall after the window; null if not paid off within 30 years. */
  payoffDate: string | null;
  /** Owed at the end of each day of the window. */
  balances: number[];
  events: PaydownEvent[];
}

/**
 * One color per debt, in a fixed order (checked for color-blind readers), so a debt keeps its color
 * across the chart, the ledger and the summary cards.
 */
export const PAYDOWN_COLORS = ['#7c3aed', '#0d9488', '#d97706', '#2563eb', '#db2777', '#65a30d'] as const;

export function paydownColor(index: number): string {
  return PAYDOWN_COLORS[index % PAYDOWN_COLORS.length];
}

/** "Payment", "25% of Paycheck", "Interest". */
export function describeEvent(event: PaydownEvent): string {
  switch (event.kind) {
    case 'payment':
      return 'Scheduled payment';
    case 'allocation':
      return `${event.percent !== null ? `${Number(event.percent.toFixed(2))}% of ` : ''}${event.sourceName ?? 'a credit'}`;
    default:
      return 'Interest';
  }
}

/** Is the payoff date inside the window, after it, or not in sight? */
export function payoffStatus(debt: DebtPaydown, windowEnd: string): 'paid' | 'later' | 'never' {
  if (debt.payoffDate === null) {
    return 'never';
  }
  return debt.payoffDate <= windowEnd ? 'paid' : 'later';
}

const compactMoney = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', notation: 'compact', maximumFractionDigits: 1 });

/** Every debt's balance on one axis, one colored line each. */
export function paydownChart(
  debts: readonly DebtPaydown[],
  days: readonly LedgerDay[],
): { data: ChartData<'line'>; options: ChartOptions<'line'> } {
  return {
    data: {
      labels: days.map((day) => shortDay(day.wDate)),
      datasets: debts.map((debt, index) => ({
        label: debt.name ?? `Debt ${index + 1}`,
        data: debt.balances,
        borderColor: paydownColor(index),
        backgroundColor: paydownColor(index),
        borderWidth: 2,
        pointRadius: 0,
        pointHoverRadius: 5,
        pointHoverBorderWidth: 2,
        pointHoverBorderColor: '#ffffff',
        stepped: 'after' as const, // balances change on payment days and stay flat between them
        fill: false,
      })),
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: {
          display: debts.length > 1,
          position: 'top',
          align: 'end',
          labels: { boxWidth: 12, boxHeight: 12, color: '#4b5563' },
        },
        tooltip: {
          callbacks: {
            label: (item: TooltipItem<'line'>) => ` ${item.dataset.label}: ${formatMoney(Number(item.raw))} owed`,
          },
        },
      },
      scales: {
        x: { grid: { display: false }, ticks: { color: '#4b5563', autoSkip: true, maxTicksLimit: 8, maxRotation: 0 } },
        y: {
          beginAtZero: true,
          grid: { color: '#eef0f3' },
          border: { display: false },
          ticks: { color: '#4b5563', maxTicksLimit: 6, callback: (value) => compactMoney.format(Number(value)) },
        },
      },
    },
  };
}
