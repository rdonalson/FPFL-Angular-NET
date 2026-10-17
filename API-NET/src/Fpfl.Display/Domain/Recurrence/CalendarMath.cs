namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Date helpers shared by the recurrence expanders.</summary>
public static class CalendarMath
{
    /// <summary>
    ///     The given day of the month, pulled back to the month's last day when the month is shorter
    ///     (the 31st in April is April 30; Feb 29 in a non-leap year is Feb 28).
    /// </summary>
    public static DateOnly ClampedDate(int year, int month, int day) =>
        new(year, month, Math.Clamp(day, 1, DateTime.DaysInMonth(year, month)));

    /// <summary>The first <paramref name="dayOfWeek" /> on or after <paramref name="date" />.</summary>
    public static DateOnly NextOnOrAfter(DateOnly date, DayOfWeek dayOfWeek) =>
        date.AddDays(((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7);

    /// <summary>The 1st–4th (index 1–4) or last (index -1) <paramref name="dayOfWeek" /> of a month.</summary>
    public static DateOnly NthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, int index)
    {
        if (index > 0)
        {
            return NextOnOrAfter(new DateOnly(year, month, 1), dayOfWeek).AddDays(7 * (index - 1));
        }

        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        return last.AddDays(-(((int)last.DayOfWeek - (int)dayOfWeek + 7) % 7));
    }

    /// <summary>The first day of every month that touches the window.</summary>
    public static IEnumerable<(int Year, int Month)> MonthsIn(DateWindow window)
    {
        for (var cursor = new DateOnly(window.Start.Year, window.Start.Month, 1); cursor <= window.End; cursor = cursor.AddMonths(1))
        {
            yield return (cursor.Year, cursor.Month);
        }
    }

    /// <summary>
    ///     Stored weekdays are 0 = Sunday … 6 = Saturday, which is exactly .NET's <see cref="DayOfWeek" />.
    ///     (API-JDK passed them to java.time's 1 = Monday … 7 = Sunday, so a Sunday item threw.)
    /// </summary>
    public static DayOfWeek? ToDayOfWeek(int? stored) =>
        stored is >= 0 and <= 6 ? (DayOfWeek)stored.Value : null;

    public static bool IsMonth(int? month) => month is >= 1 and <= 12;
}
