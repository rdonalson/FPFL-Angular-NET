using Fpfl.Common.Sanitization;

namespace Fpfl.Api.Dtos.Display;

/// <summary>
///     One tracked debt over the requested window (POST /display/paydown, same body as /display/ledger).
///     <c>balances</c> has one value per day of the window; <c>payoffDate</c> may fall after the window, or be
///     null if the debt isn't paid off within 30 years.
/// </summary>
public sealed record DebtPaydownResponse(
    long DebtId,
    long ItemId,
    [property: LenientText] string? Name,
    decimal Apr,
    decimal EnteredBalance,
    DateOnly BalanceDate,
    decimal StartingBalance,
    decimal EndingBalance,
    decimal TotalPaid,
    decimal TotalInterest,
    DateOnly? PayoffDate,
    IReadOnlyList<decimal> Balances,
    IReadOnlyList<PaydownEventResponse> Events);

/// <summary>
///     <c>kind</c> is <c>payment</c> (the debit's own payment), <c>allocation</c> (a share of a credit) or
///     <c>interest</c>. Payments and allocations are negative; interest is positive.
/// </summary>
public sealed record PaydownEventResponse(
    DateOnly Date,
    string Kind,
    long? SourceItemId,
    [property: LenientText] string? SourceName,
    decimal? Percent,
    decimal Amount,
    decimal BalanceAfter);
