namespace Fpfl.Items.Domain.Entities;

/// <summary>
///     How often an item recurs (One Time, Daily, Weekly, Bi-Weekly, Monthly, ...).
///     Maps to <c>fpfl.time_periods</c>. The id is supplied by the caller (not generated),
///     matching API-JDK, because the ledger's recurrence expanders are keyed by these ids.
/// </summary>
public sealed class TimePeriod
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
