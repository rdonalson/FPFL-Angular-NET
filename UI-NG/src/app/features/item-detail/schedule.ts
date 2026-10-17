import { Item, PERIOD } from './item';

const WEEKDAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

const SUFFIX: Record<number, string> = { 1: 'st', 2: 'nd', 3: 'rd' };

const dateFormat = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

/** 1 → "1st", 2 → "2nd", 11 → "11th", 22 → "22nd". */
export function ordinal(n: number): string {
  const lastTwo = n % 100;
  if (lastTwo >= 11 && lastTwo <= 13) {
    return `${n}th`;
  }
  return `${n}${SUFFIX[n % 10] ?? 'th'}`;
}

/** 'yyyy-MM-dd' → "Jan 5, 2026" (parsed as a local date, so it never shifts a day). */
export function formatDate(value: string | null): string {
  if (!value) {
    return '';
  }
  const [year, month, day] = value.split('-').map(Number);
  if (!year || !month || !day) {
    return value;
  }
  return dateFormat.format(new Date(year, month - 1, day));
}

/** A readable sentence for an item's recurrence, e.g. "Every other Friday" or "On the 1st and 15th". */
export function describeSchedule(item: Item): string {
  const weekday = (dow: number | null) => (dow === null ? null : WEEKDAYS[dow]);
  const monthDay = (month: number | null, day: number | null) =>
    month && day ? `${MONTHS[month - 1]} ${day}` : null;
  const join = (parts: (string | null)[]) => parts.filter(Boolean).join(', ');

  switch (item.timePeriod?.id) {
    case PERIOD.oneTime:
      return item.beginDate ? `Once, on ${formatDate(item.beginDate)}` : 'Once';
    case PERIOD.daily:
      return 'Every day';
    case PERIOD.weekly:
      return weekday(item.weeklyDow) ? `Every ${weekday(item.weeklyDow)}` : 'Weekly';
    case PERIOD.everyTwoWeeks:
      return weekday(item.everyOtherWeekDow) ? `Every other ${weekday(item.everyOtherWeekDow)}` : 'Every two weeks';
    case PERIOD.biMonthly:
      return item.biMonthlyDay1 && item.biMonthlyDay2
        ? `On the ${ordinal(item.biMonthlyDay1)} and ${ordinal(item.biMonthlyDay2)}`
        : 'Twice a month';
    case PERIOD.monthly:
      return item.monthlyDom ? `Monthly on the ${ordinal(item.monthlyDom)}` : 'Monthly';
    case PERIOD.quarterly: {
      const dates = join([
        monthDay(item.quarterly1Month, item.quarterly1Day),
        monthDay(item.quarterly2Month, item.quarterly2Day),
        monthDay(item.quarterly3Month, item.quarterly3Day),
        monthDay(item.quarterly4Month, item.quarterly4Day),
      ]);
      return dates ? `Quarterly: ${dates}` : 'Quarterly';
    }
    case PERIOD.semiAnnual: {
      const dates = [
        monthDay(item.semiAnnual1Month, item.semiAnnual1Day),
        monthDay(item.semiAnnual2Month, item.semiAnnual2Day),
      ].filter(Boolean);
      return dates.length ? `Twice a year: ${dates.join(' and ')}` : 'Twice a year';
    }
    case PERIOD.annual: {
      const date = monthDay(item.annualMoy, item.annualDom);
      return date ? `Every year on ${date}` : 'Yearly';
    }
    case PERIOD.nthWeekday: {
      const day = weekday(item.nthDow);
      if (!day || item.nthIndex === null) {
        return 'Monthly';
      }
      const which = item.nthIndex === -1 ? 'Last' : ordinal(item.nthIndex);
      return `${which} ${day} of the month`;
    }
    default:
      return item.timePeriod?.name ?? '—';
  }
}

/** When an item applies: "Jan 1, 2026 – Dec 31, 2026", "From Jan 1, 2026", "Until …" or "Ongoing". */
export function describeDateRange(item: Item): string {
  if (item.timePeriod?.id === PERIOD.oneTime) {
    return formatDate(item.beginDate) || '—';
  }
  const begin = formatDate(item.beginDate);
  const end = formatDate(item.endDate);
  if (begin && end) {
    return `${begin} – ${end}`;
  }
  if (begin) {
    return `From ${begin}`;
  }
  if (end) {
    return `Until ${end}`;
  }
  return 'Ongoing';
}
