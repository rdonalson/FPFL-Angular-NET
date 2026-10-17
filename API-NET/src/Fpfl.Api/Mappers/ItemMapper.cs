using Fpfl.Api.Dtos.ItemTypes;
using Fpfl.Api.Dtos.Items;
using Fpfl.Api.Dtos.TimePeriods;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Mappers;

/// <summary>
///     Explicit Item mapping (no reflection). Sanitizing happens in the response factory;
///     the item name uses the lenient rules because users type free text there.
/// </summary>
public sealed class ItemMapper
{
    public ItemResponse ToResponse(Item item) => new(
        item.Id,
        item.UserId,
        item.Name ?? string.Empty,
        item.Amount ?? 0,
        item.ItemType is null ? null : new ItemTypeResponse(item.ItemType.Id, item.ItemType.Name),
        item.TimePeriod is null ? null : new TimePeriodResponse(item.TimePeriod.Id, item.TimePeriod.Name),
        item.BeginDate,
        item.EndDate,
        item.WeeklyDow,
        item.EveryOtherWeekDow,
        item.BiMonthlyDay1,
        item.BiMonthlyDay2,
        item.MonthlyDom,
        item.Quarterly1Month,
        item.Quarterly1Day,
        item.Quarterly2Month,
        item.Quarterly2Day,
        item.Quarterly3Month,
        item.Quarterly3Day,
        item.Quarterly4Month,
        item.Quarterly4Day,
        item.SemiAnnual1Month,
        item.SemiAnnual1Day,
        item.SemiAnnual2Month,
        item.SemiAnnual2Day,
        item.AnnualMoy,
        item.AnnualDom,
        item.NthDow,
        item.NthIndex,
        item.DateRangeReq);
}
