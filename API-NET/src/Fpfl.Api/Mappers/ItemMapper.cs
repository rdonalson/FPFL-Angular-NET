using Fpfl.Api.Dtos.InitialAmounts;
using Fpfl.Api.Dtos.Items;
using Fpfl.Api.Dtos.ItemTypes;
using Fpfl.Api.Dtos.TimePeriods;
using Fpfl.Common.Sanitization;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Mappers;

/// <summary>
///     Explicit Item mapping (no reflection). Outgoing data is sanitized by the response factory;
///     the item name uses the lenient rules (normalize only) because users type free text there.
/// </summary>
public sealed class ItemMapper
{
    /// <summary>An unsaved item from a request; the service sets the owner and ItemRules validates it.</summary>
    public Item ToDraft(ItemRequest request) => new()
    {
        Name = Sanitizer.Normalize(request.Name),
        Amount = request.Amount,
        ItemTypeId = request.FkItemType ?? 0,
        TimePeriodId = request.FkPeriod,
        BeginDate = request.BeginDate,
        EndDate = request.EndDate,
        WeeklyDow = request.WeeklyDow,
        EveryOtherWeekDow = request.EveryOtherWeekDow,
        BiMonthlyDay1 = request.BiMonthlyDay1,
        BiMonthlyDay2 = request.BiMonthlyDay2,
        MonthlyDom = request.MonthlyDom,
        Quarterly1Month = request.Quarterly1Month,
        Quarterly1Day = request.Quarterly1Day,
        Quarterly2Month = request.Quarterly2Month,
        Quarterly2Day = request.Quarterly2Day,
        Quarterly3Month = request.Quarterly3Month,
        Quarterly3Day = request.Quarterly3Day,
        Quarterly4Month = request.Quarterly4Month,
        Quarterly4Day = request.Quarterly4Day,
        SemiAnnual1Month = request.SemiAnnual1Month,
        SemiAnnual1Day = request.SemiAnnual1Day,
        SemiAnnual2Month = request.SemiAnnual2Month,
        SemiAnnual2Day = request.SemiAnnual2Day,
        AnnualMoy = request.AnnualMoy,
        AnnualDom = request.AnnualDom,
        NthDow = request.NthDow,
        NthIndex = request.NthIndex,
        DateRangeReq = request.DateRangeReq,
    };

    public InitialAmountResponse ToInitialAmountResponse(Item item) => new(
        item.Id,
        item.UserId,
        item.Name ?? string.Empty,
        item.Amount ?? 0,
        item.ItemType is null ? null : new ItemTypeResponse(item.ItemType.Id, item.ItemType.Name),
        item.BeginDate);

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
