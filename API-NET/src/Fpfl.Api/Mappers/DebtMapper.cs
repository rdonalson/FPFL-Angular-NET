using Fpfl.Api.Dtos.Debts;
using Fpfl.Common.Sanitization;
using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Mappers;

/// <summary>Explicit Debt Paydown mapping (no reflection). Outgoing data is sanitized by the response factory.</summary>
public sealed class DebtMapper
{
    public DebtDraft ToDraft(DebtRequest request) => new(
        request.ItemId,
        request.Balance,
        request.Apr,
        request.BalanceDate,
        (request.Allocations ?? Array.Empty<DebtAllocationRequest>())
            .Select(a => new AllocationDraft(a.CreditItemId, a.Percent))
            .ToList());

    public DebtResponse ToResponse(Debt debt) => new(
        debt.Id,
        debt.ItemId,
        Sanitizer.Normalize(debt.Item?.Name),
        debt.Item?.Amount,
        debt.Item?.TimePeriod?.Name,
        debt.Balance,
        debt.Apr,
        debt.BalanceDate,
        debt.Allocations
            .OrderBy(a => a.CreditItem?.Name)
            .Select(a => new DebtAllocationResponse(
                a.Id,
                a.CreditItemId,
                Sanitizer.Normalize(a.CreditItem?.Name),
                a.CreditItem?.Amount,
                a.CreditItem?.TimePeriod?.Name,
                a.Percent,
                PerOccurrence(a)))
            .ToList());

    private static decimal PerOccurrence(DebtAllocation allocation) =>
        allocation.CreditItem?.Amount is { } amount && double.IsFinite(amount)
            ? decimal.Round((decimal)amount * allocation.Percent / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;
}
