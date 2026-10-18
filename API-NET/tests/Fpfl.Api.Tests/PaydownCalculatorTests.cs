using Fpfl.Display.Domain;
using Fpfl.Display.Domain.Paydown;
using Fpfl.Display.Domain.Recurrence;
using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Tests;

/// <summary>
///     Calendar anchors: 2026-10-02 is a Friday, so an every-two-weeks Friday paycheck from then lands on
///     Oct 2, 16, 30, Nov 13, 27, Dec 11, 25. Balances are entered as of Oct 17.
/// </summary>
public sealed class PaydownCalculatorTests
{
    private static DateOnly D(int month, int day, int year = 2026) => new(year, month, day);

    private static readonly DateOnly AsOf = D(10, 17);

    private static Item Card(double payment = 200, int dom = 15) => new()
    {
        Id = 10,
        Name = "Chase Visa",
        Amount = payment,
        ItemTypeId = ItemRules.Debit,
        TimePeriodId = ItemRules.Monthly,
        MonthlyDom = dom,
    };

    private static Item Paycheck() => new()
    {
        Id = 20,
        Name = "Paycheck",
        Amount = 2000,
        ItemTypeId = ItemRules.Credit,
        TimePeriodId = ItemRules.EveryTwoWeeks,
        EveryOtherWeekDow = 5,
        BeginDate = D(10, 2),
    };

    private static Debt MakeDebt(decimal balance, decimal apr = 0, params (long CreditId, decimal Percent)[] allocations) => new()
    {
        Id = 1,
        ItemId = 10,
        Balance = balance,
        Apr = apr,
        BalanceDate = AsOf,
        Allocations = allocations.Select((a, i) => new DebtAllocation { Id = i + 1, DebtId = 1, CreditItemId = a.CreditId, Percent = a.Percent }).ToList(),
    };

    private static readonly DateWindow Quarter = new(D(10, 18), D(12, 31));

    private static PaydownResult Run(Debt debt, DateWindow window, params Item[] items) =>
        PaydownCalculator.Calculate(items, [debt], window);

    [Fact]
    public void Allocations_pay_the_debt_off()
    {
        var result = Run(MakeDebt(1000, 0, (20, 25)), Quarter, Card(), Paycheck());
        var debt = Assert.Single(result.Debts);

        Assert.Equal("Chase Visa", debt.Name);
        Assert.Equal(new[] { D(10, 30), D(11, 13) }, debt.Events.Select(e => e.Date));
        Assert.All(debt.Events, e => Assert.Equal(PaydownEventKind.Allocation, e.Kind));
        Assert.Equal(new[] { -500m, -500m }, debt.Events.Select(e => e.Amount));
        Assert.Equal(new[] { 500m, 0m }, debt.Events.Select(e => e.BalanceAfter));
        Assert.Equal(D(11, 13), debt.PayoffDate);
        Assert.Equal(1000m, debt.TotalPaid);

        // One balance per day; the card's Nov 15 payment never happens because it's already paid off.
        Assert.Equal(75, debt.Balances.Count);
        Assert.Equal(1000m, debt.StartingBalance);
        Assert.Equal(1000m, debt.Balances[0]);
        Assert.Equal(500m, debt.Balances[12]); // Oct 30
        Assert.Equal(0m, debt.EndingBalance);
    }

    [Fact]
    public void Allocations_become_extra_debits_for_the_main_ledger()
    {
        var result = Run(MakeDebt(1000, 0, (20, 25)), Quarter, Card(), Paycheck());

        Assert.Equal(2, result.Adjustments.Count);
        Assert.All(result.Adjustments, a =>
        {
            Assert.Equal("Chase Visa paydown", a.Name);
            Assert.Equal("25% of Paycheck", a.Detail);
            Assert.Equal(10, a.ItemId);
            Assert.Equal(-500m, a.Amount);
        });
    }

    [Fact]
    public void The_last_payment_is_cut_to_what_is_left()
    {
        var result = Run(MakeDebt(650, 0, (20, 12.5m)), Quarter, Card(payment: 0), Paycheck());
        var debt = result.Debts[0];

        // 12.5% of $2,000 = $250: Oct 30 → 400, Nov 13 → 150, Nov 27 pays only 150.
        Assert.Equal(new[] { -250m, -250m, -150m }, debt.Events.Select(e => e.Amount));
        Assert.Equal(D(11, 27), debt.PayoffDate);
        Assert.Equal("12.5% of Paycheck", result.Adjustments[0].Detail);
        Assert.Equal(-150m, result.Adjustments[^1].Amount);
    }

    [Fact]
    public void The_debits_own_payment_pays_it_down_and_projects_past_the_window()
    {
        var result = Run(MakeDebt(3000), Quarter, Card(payment: 200));
        var debt = result.Debts[0];

        Assert.Equal(new[] { D(11, 15), D(12, 15) }, debt.Events.Select(e => e.Date));
        Assert.All(debt.Events, e => Assert.Equal(PaydownEventKind.Payment, e.Kind));
        Assert.Equal(2600m, debt.EndingBalance);

        // 15 payments of $200 starting Nov 15, 2026 → the last on Jan 15, 2028.
        Assert.Equal(D(1, 15, 2028), debt.PayoffDate);

        // The card's payment is already a debit in the main ledger, so it isn't added again.
        Assert.Empty(result.Adjustments);
    }

    [Fact]
    public void Interest_is_charged_monthly_at_month_end()
    {
        var debt = Run(MakeDebt(1200, apr: 12), Quarter, Card(payment: 0)).Debts[0];

        // 1% a month: Oct 31 +12.00, Nov 30 +12.12, Dec 31 +12.24.
        Assert.Equal(new[] { 12m, 12.12m, 12.24m }, debt.Events.Select(e => e.Amount));
        Assert.All(debt.Events, e => Assert.Equal(PaydownEventKind.Interest, e.Kind));
        Assert.Equal(1236.36m, debt.EndingBalance);
        Assert.Equal(36.36m, debt.TotalInterest);
        Assert.Null(debt.PayoffDate); // nothing is paying it
    }

    [Fact]
    public void The_debits_payment_comes_before_allocations_on_the_same_day()
    {
        var debt = Run(MakeDebt(1000, 0, (20, 25)), Quarter, Card(payment: 100, dom: 30), Paycheck()).Debts[0];

        var oct30 = debt.Events.Where(e => e.Date == D(10, 30)).ToList();
        Assert.Equal(new[] { PaydownEventKind.Payment, PaydownEventKind.Allocation }, oct30.Select(e => e.Kind));
        Assert.Equal(new[] { 900m, 400m }, oct30.Select(e => e.BalanceAfter));
    }

    [Fact]
    public void A_later_window_starts_from_the_balance_at_that_point()
    {
        var result = Run(MakeDebt(1000, 0, (20, 25)), new DateWindow(D(11, 1), D(11, 30)), Card(), Paycheck());
        var debt = result.Debts[0];

        Assert.Equal(500m, debt.StartingBalance);
        Assert.Equal(500m, debt.Balances[0]);
        Assert.Equal(0m, debt.Balances[12]); // Nov 13
        Assert.Single(debt.Events);
        Assert.Single(result.Adjustments);
    }

    [Fact]
    public void Days_before_the_balance_date_show_the_entered_balance()
    {
        var debt = Run(MakeDebt(1000, 0, (20, 25)), new DateWindow(D(10, 10), D(10, 31)), Card(), Paycheck()).Debts[0];

        Assert.Equal(22, debt.Balances.Count);
        Assert.All(debt.Balances.Take(8), b => Assert.Equal(1000m, b)); // Oct 10–17
        Assert.Equal(500m, debt.Balances[^2]); // Oct 30
        Assert.Equal(1000m, debt.StartingBalance);
        Assert.Single(debt.Events); // the Oct 16 paycheck is before the balance date
    }

    [Fact]
    public void A_zero_balance_is_already_paid_off()
    {
        var debt = Run(MakeDebt(0, 0, (20, 25)), Quarter, Card(), Paycheck()).Debts[0];

        Assert.Equal(AsOf, debt.PayoffDate);
        Assert.Empty(debt.Events);
        Assert.All(debt.Balances, b => Assert.Equal(0m, b));
    }

    [Fact]
    public void Debts_whose_debit_is_gone_or_credits_that_are_missing_are_skipped()
    {
        Assert.Empty(Run(MakeDebt(1000), Quarter, Paycheck()).Debts);

        var debt = Run(MakeDebt(1000, 0, (99, 50)), Quarter, Card(payment: 0)).Debts[0];
        Assert.Empty(debt.Events);
    }

    [Fact]
    public void The_main_ledger_lists_allocations_as_debt_paydown_debits()
    {
        Item[] items = [Paycheck(), new Item { Id = 99, ItemTypeId = 3, Name = "IA", Amount = 100 }];
        var adjustment = new LedgerAdjustment { Date = D(10, 30), ItemId = 10, Name = "Chase Visa paydown", Detail = "25% of Paycheck", Amount = -500 };

        var day = LedgerBuilder.Build(items, new DateWindow(D(10, 30), D(10, 30)), [adjustment])[0];

        Assert.Equal(2000m, day.CreditSummary);
        Assert.Equal(-500m, day.DebitSummary);
        Assert.Equal(1600m, day.RunningTotal);

        var entry = day.Entries[^1];
        Assert.Equal("Chase Visa paydown", entry.Name);
        Assert.Equal(LedgerBuilder.PaydownTypeName, entry.ItemTypeName);
        Assert.Equal("25% of Paycheck", entry.PeriodName);
        Assert.Equal(-500m, entry.Amount);
        Assert.Equal(new[] { 1, 2 }, day.Entries.Select(e => e.ItemKey));
    }
}
