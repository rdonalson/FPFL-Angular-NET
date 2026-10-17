using Fpfl.Api.Dtos.Display;
using Fpfl.Display.Domain.Models;

namespace Fpfl.Api.Mappers;

/// <summary>Explicit ledger mapping (no reflection). Outgoing data is sanitized by the response factory.</summary>
public sealed class DisplayMapper
{
    public IReadOnlyList<LedgerDayResponse> ToResponse(IReadOnlyList<LedgerDay> days) =>
        days.Select(ToResponse).ToList();

    public LedgerDayResponse ToResponse(LedgerDay day) => new(
        day.RollupKey,
        day.Year,
        day.Date,
        day.CreditSummary,
        day.DebitSummary,
        day.Net,
        day.RunningTotal,
        day.Entries.Select(entry => new LedgerItemResponse(
            entry.ItemKey,
            entry.ItemId,
            day.RollupKey,
            day.Year,
            entry.OccurrenceDate,
            entry.ItemTypeId,
            entry.ItemTypeName,
            entry.PeriodName,
            entry.Name,
            entry.Amount)).ToList());
}
