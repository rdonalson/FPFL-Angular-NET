namespace Fpfl.Items.Domain;

/// <summary>What a request asks a tracked debt to be, before <see cref="DebtRules" /> checks it.</summary>
public sealed record DebtDraft(
    long? ItemId,
    decimal? Balance,
    decimal? Apr,
    DateOnly? BalanceDate,
    IReadOnlyList<AllocationDraft> Allocations);

public sealed record AllocationDraft(long? CreditItemId, decimal? Percent);
