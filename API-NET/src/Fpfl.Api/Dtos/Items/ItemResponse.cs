using Fpfl.Api.Dtos.ItemTypes;
using Fpfl.Api.Dtos.TimePeriods;
using Fpfl.Common.Sanitization;

namespace Fpfl.Api.Dtos.Items;

/// <summary>
///     An item as returned to clients: the same fields as API-JDK's ItemResponse, with the item type
///     and time period nested as <c>{ id, name }</c>. Weekdays are 0 = Sunday … 6 = Saturday,
///     months 1 = January, and <c>nthIndex</c> is 1–4 or -1 for "last".
/// </summary>
public sealed record ItemResponse(
    long Id,
    Guid UserId,
    [property: LenientText] string Name,
    double Amount,
    ItemTypeResponse? ItemType,
    TimePeriodResponse? TimePeriod,
    DateOnly? BeginDate,
    DateOnly? EndDate,
    int? WeeklyDow,
    int? EveryOtherWeekDow,
    int? BiMonthlyDay1,
    int? BiMonthlyDay2,
    int? MonthlyDom,
    int? Quarterly1Month,
    int? Quarterly1Day,
    int? Quarterly2Month,
    int? Quarterly2Day,
    int? Quarterly3Month,
    int? Quarterly3Day,
    int? Quarterly4Month,
    int? Quarterly4Day,
    int? SemiAnnual1Month,
    int? SemiAnnual1Day,
    int? SemiAnnual2Month,
    int? SemiAnnual2Day,
    int? AnnualMoy,
    int? AnnualDom,
    int? NthDow,
    int? NthIndex,
    bool DateRangeReq);
