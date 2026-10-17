using System.ComponentModel.DataAnnotations;
using Fpfl.Common.Sanitization;

namespace Fpfl.Api.Dtos.Items;

/// <summary>
///     Body of POST /items and PUT /items/{id} — the same fields as API-JDK's ItemRequest, minus
///     <c>userId</c>: the owner always comes from the access token. Send only the recurrence fields for
///     the chosen <c>fkPeriod</c>; any others are ignored and cleared. Full rules live in ItemRules.
/// </summary>
public sealed record ItemRequest(
    [Required(ErrorMessage = "Item Name is required and cannot be blank")]
    [MaxLength(150, ErrorMessage = "Item Name must be at most 150 characters")]
    [property: LenientText]
    string Name,
    [Required(ErrorMessage = "Item Amount is required and must be a positive currency value")]
    double? Amount,
    [Required(ErrorMessage = "ItemType is required (1 = credit, 2 = debit)")]
    long? FkItemType,
    [Required(ErrorMessage = "Time Period is required (1–10)")]
    long? FkPeriod,
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
    bool DateRangeReq = false);
