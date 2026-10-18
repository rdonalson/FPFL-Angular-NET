using System.ComponentModel.DataAnnotations;
using Fpfl.Common.Sanitization;

namespace Fpfl.Api.Dtos.Debts;

/// <summary>
///     Body of POST /debts and PUT /debts/{id}. <c>itemId</c> (one of your debits) is required to create and
///     can't change on update. <c>apr</c> defaults to 0 and <c>balanceDate</c> to today. The owner comes from
///     the access token.
/// </summary>
public sealed record DebtRequest(
    long? ItemId,
    [Required(ErrorMessage = "Balance owed is required")]
    decimal? Balance,
    decimal? Apr,
    DateOnly? BalanceDate,
    [MaxLength(20, ErrorMessage = "A debt can have at most 20 allocations")]
    IReadOnlyList<DebtAllocationRequest>? Allocations);

/// <summary>A share of one of your credits sent to the debt each time the credit occurs.</summary>
public sealed record DebtAllocationRequest(
    [Required(ErrorMessage = "Each allocation needs a credit")]
    long? CreditItemId,
    [Required(ErrorMessage = "Each allocation needs a percentage")]
    decimal? Percent);

/// <summary>A tracked debt with its debit's name, payment and schedule, and its allocations.</summary>
public sealed record DebtResponse(
    long Id,
    long ItemId,
    [property: LenientText] string? Name,
    double? Payment,
    string? Period,
    decimal Balance,
    decimal Apr,
    DateOnly BalanceDate,
    IReadOnlyList<DebtAllocationResponse> Allocations);

/// <summary><c>perOccurrence</c> is how much each occurrence of the credit sends (percent of its amount).</summary>
public sealed record DebtAllocationResponse(
    long Id,
    long CreditItemId,
    [property: LenientText] string? CreditName,
    double? CreditAmount,
    string? CreditPeriod,
    decimal Percent,
    decimal PerOccurrence);
