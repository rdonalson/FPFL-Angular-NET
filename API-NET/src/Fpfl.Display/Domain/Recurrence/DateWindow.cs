namespace Fpfl.Display.Domain.Recurrence;

/// <summary>An inclusive range of calendar days.</summary>
public readonly record struct DateWindow(DateOnly Start, DateOnly End)
{
    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public int Days => End.DayNumber - Start.DayNumber + 1;

    /// <summary>The overlap of two windows, or null when they don't meet.</summary>
    public DateWindow? Intersect(DateOnly start, DateOnly end)
    {
        var from = start > Start ? start : Start;
        var to = end < End ? end : End;
        return to < from ? null : new DateWindow(from, to);
    }
}
