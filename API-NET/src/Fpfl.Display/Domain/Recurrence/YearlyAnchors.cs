namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Shared by the quarterly, semi-annual and annual expanders: fixed month/day pairs that repeat every year.</summary>
internal static class YearlyAnchors
{
    public static IEnumerable<DateOnly> Expand(DateWindow window, params (int? Month, int? Day)[] anchors)
    {
        for (var year = window.Start.Year; year <= window.End.Year; year++)
        {
            foreach (var (month, day) in anchors)
            {
                if (month is not { } m || !CalendarMath.IsMonth(m) || day is not { } d)
                {
                    continue; // an anchor that isn't set
                }

                var date = CalendarMath.ClampedDate(year, m, d);
                if (window.Contains(date))
                {
                    yield return date;
                }
            }
        }
    }
}
