using System.Globalization;
using Fpfl.Display.Domain.Recurrence;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Paydown;

/// <summary>
///     Projects how each tracked debt is paid down. Pure (no database, no clock) so every rule is unit-tested.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>The entered balance is what was owed at the end of its date; activity counts from the next day.</item>
///         <item>Each occurrence of the debt's own debit pays its amount; each occurrence of an allocated credit pays its percentage of the credit (rounded to cents).</item>
///         <item>Payments never go below zero: the last one is cut to what's left, and none follow the payoff.</item>
///         <item>Interest is APR / 12 on the balance at each month end (after that day's payments), while anything is owed.</item>
///         <item>Within a day the debit's payment comes first, then allocations by credit name.</item>
///     </list>
///     The simulation runs from the balance date, so a window that starts later still begins from the right
///     balance, and keeps going past the window (up to <see cref="HorizonYears" />) to find the payoff date.
/// </remarks>
public static class PaydownCalculator
{
    public const int HorizonYears = 30;

    public static PaydownResult Calculate(IEnumerable<Item> items, IEnumerable<Debt> debts, DateWindow window)
    {
        var byId = items.ToDictionary(i => i.Id);
        var results = new List<DebtPaydown>();
        var adjustments = new List<LedgerAdjustment>();

        foreach (var debt in debts.OrderBy(d => byId.GetValueOrDefault(d.ItemId)?.Name).ThenBy(d => d.Id))
        {
            if (!byId.TryGetValue(debt.ItemId, out var debitItem))
            {
                continue; // the debit is gone; nothing to project
            }

            var (paydown, debtAdjustments) = Project(debt, debitItem, byId, window);
            results.Add(paydown);
            adjustments.AddRange(debtAdjustments);
        }

        return new PaydownResult(results, adjustments.OrderBy(a => a.Date).ThenBy(a => a.Name).ToList());
    }

    private sealed record Scheduled(DateOnly Date, int Order, string? SortName, PaydownEventKind Kind, Item Source, decimal Amount, decimal? Percent);

    private static (DebtPaydown, List<LedgerAdjustment>) Project(
        Debt debt, Item debitItem, IReadOnlyDictionary<long, Item> items, DateWindow window)
    {
        var first = debt.BalanceDate.AddDays(1);
        var last = Max(window.End, debt.BalanceDate.AddYears(HorizonYears));
        var simulation = new DateWindow(first, last);

        var scheduled = new List<Scheduled>();
        var payment = Money(debitItem.Amount);
        if (payment > 0)
        {
            scheduled.AddRange(RecurrenceEngine.Occurrences(debitItem, simulation)
                .Select(date => new Scheduled(date, 0, null, PaydownEventKind.Payment, debitItem, payment, null)));
        }

        foreach (var allocation in debt.Allocations)
        {
            if (!items.TryGetValue(allocation.CreditItemId, out var credit) || allocation.Percent <= 0)
            {
                continue;
            }

            var share = decimal.Round(Money(credit.Amount) * Math.Min(allocation.Percent, 100m) / 100m, 2, MidpointRounding.AwayFromZero);
            if (share > 0)
            {
                scheduled.AddRange(RecurrenceEngine.Occurrences(credit, simulation)
                    .Select(date => new Scheduled(date, 1, credit.Name, PaydownEventKind.Allocation, credit, share, allocation.Percent)));
            }
        }

        var byDate = scheduled
            .OrderBy(s => s.Date).ThenBy(s => s.Order).ThenBy(s => s.SortName).ThenBy(s => s.Source.Id)
            .ToLookup(s => s.Date);

        var name = debitItem.Name;
        var monthlyRate = debt.Apr / 100m / 12m;
        var balance = Math.Max(0, debt.Balance);
        DateOnly? payoff = balance == 0 ? debt.BalanceDate : null;

        var events = new List<PaydownEvent>();
        var adjustments = new List<LedgerAdjustment>();
        var balances = new List<decimal>(window.Days);

        // Days of the window before the balance date have no history: show the entered balance.
        for (var day = window.Start; day <= window.End && day <= debt.BalanceDate; day = day.AddDays(1))
        {
            balances.Add(balance);
        }

        var startingBalance = balance;

        for (var day = first; day <= last; day = day.AddDays(1))
        {
            var inWindow = window.Contains(day);
            if (day == window.Start)
            {
                startingBalance = balance;
            }

            foreach (var item in byDate[day])
            {
                if (balance == 0)
                {
                    break;
                }

                var paid = Math.Min(item.Amount, balance);
                balance -= paid;
                if (balance == 0)
                {
                    payoff ??= day;
                }

                if (inWindow)
                {
                    events.Add(new PaydownEvent
                    {
                        Date = day,
                        Kind = item.Kind,
                        SourceItemId = item.Source.Id,
                        SourceName = item.Source.Name,
                        Percent = item.Percent,
                        Amount = -paid,
                        BalanceAfter = balance,
                    });

                    if (item.Kind == PaydownEventKind.Allocation)
                    {
                        adjustments.Add(new LedgerAdjustment
                        {
                            Date = day,
                            ItemId = debitItem.Id,
                            Name = $"{name} paydown",
                            Detail = $"{item.Percent!.Value.ToString("0.##", CultureInfo.InvariantCulture)}% of {item.Source.Name}",
                            Amount = -paid,
                        });
                    }
                }
            }

            if (balance > 0 && monthlyRate > 0 && day.Day == DateTime.DaysInMonth(day.Year, day.Month))
            {
                var interest = decimal.Round(balance * monthlyRate, 2, MidpointRounding.AwayFromZero);
                if (interest > 0)
                {
                    balance += interest;
                    if (inWindow)
                    {
                        events.Add(new PaydownEvent { Date = day, Kind = PaydownEventKind.Interest, Amount = interest, BalanceAfter = balance });
                    }
                }
            }

            if (inWindow)
            {
                balances.Add(balance);
            }

            // Past the window, keep going only to find the payoff date.
            if (day >= window.End && balance == 0)
            {
                break;
            }
        }

        // A window entirely before the balance date was filled above; one that ends after a payoff
        // that stopped the loop early stays at zero.
        while (balances.Count < window.Days)
        {
            balances.Add(0);
        }

        var paydown = new DebtPaydown
        {
            DebtId = debt.Id,
            DebtItemId = debitItem.Id,
            Name = name,
            Apr = debt.Apr,
            EnteredBalance = debt.Balance,
            BalanceDate = debt.BalanceDate,
            StartingBalance = startingBalance,
            EndingBalance = balances.Count > 0 ? balances[^1] : startingBalance,
            Balances = balances,
            TotalPaid = -events.Where(e => e.Kind != PaydownEventKind.Interest).Sum(e => e.Amount),
            TotalInterest = events.Where(e => e.Kind == PaydownEventKind.Interest).Sum(e => e.Amount),
            PayoffDate = payoff,
            Events = events,
        };

        return (paydown, adjustments);
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    private static decimal Money(double? amount) =>
        amount is { } value && double.IsFinite(value) && value > 0
            ? decimal.Round((decimal)value, 2, MidpointRounding.AwayFromZero)
            : 0m;
}
