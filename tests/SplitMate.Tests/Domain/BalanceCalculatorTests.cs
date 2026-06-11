using FluentAssertions;
using SplitMate.Domain.Entities;
using SplitMate.Domain.Enums;
using SplitMate.Domain.Services;

namespace SplitMate.Tests.Domain;

public class BalanceCalculatorTests
{
    private static readonly Guid GroupId = Guid.NewGuid();

    [Fact]
    public void Group_balances_always_sum_to_zero()
    {
        var expenses = new[]
        {
            MakeExpense("u1", 100m, ("u1", 33.34m), ("u2", 33.33m), ("u3", 33.33m)),
            MakeExpense("u2", 250.50m, ("u1", 125.25m), ("u2", 125.25m)),
            MakeExpense("u3", 99.99m, ("u2", 50m), ("u3", 49.99m))
        };
        var settlements = new[] { MakeSettlement("u3", "u1", 20m) };

        var net = BalanceCalculator.ComputeNetBalances(["u1", "u2", "u3"], expenses, settlements);

        net.Values.Sum().Should().Be(0m);
    }

    [Fact]
    public void Net_is_paid_minus_owed_adjusted_by_settlements()
    {
        // u1 paid 100, owes 50 of it; u2 owes the other 50.
        var expenses = new[] { MakeExpense("u1", 100m, ("u1", 50m), ("u2", 50m)) };

        var before = BalanceCalculator.ComputeNetBalances(["u1", "u2"], expenses, []);
        before["u1"].Should().Be(50m);   // gets back ₹50
        before["u2"].Should().Be(-50m);  // owes ₹50

        // u2 settles up by paying u1 ₹50 — everyone ends at zero.
        var after = BalanceCalculator.ComputeNetBalances(
            ["u1", "u2"], expenses, [MakeSettlement("u2", "u1", 50m)]);
        after["u1"].Should().Be(0m);
        after["u2"].Should().Be(0m);
    }

    [Fact]
    public void Partial_settlement_reduces_debt_without_clearing_it()
    {
        var expenses = new[] { MakeExpense("u1", 90m, ("u2", 90m)) };
        var settlements = new[] { MakeSettlement("u2", "u1", 30m) };

        var net = BalanceCalculator.ComputeNetBalances(["u1", "u2"], expenses, settlements);

        net["u2"].Should().Be(-60m);
        net["u1"].Should().Be(60m);
        net.Values.Sum().Should().Be(0m);
    }

    [Fact]
    public void Members_with_no_activity_have_zero_balance()
    {
        var expenses = new[] { MakeExpense("u1", 10m, ("u2", 10m)) };

        var net = BalanceCalculator.ComputeNetBalances(["u1", "u2", "idle"], expenses, []);

        net["idle"].Should().Be(0m);
        net.Values.Sum().Should().Be(0m);
    }

    private static Expense MakeExpense(
        string paidBy, decimal amount, params (string UserId, decimal Share)[] shares)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = GroupId,
            Description = "test",
            Amount = amount,
            PaidByUserId = paidBy,
            Date = new DateOnly(2026, 6, 1),
            SplitType = SplitType.ExactAmounts,
            CreatedAtUtc = DateTime.UtcNow
        };
        foreach (var (userId, share) in shares)
        {
            expense.Shares.Add(new ExpenseShare
            {
                ExpenseId = expense.Id,
                UserId = userId,
                ShareAmount = share
            });
        }

        return expense;
    }

    private static Settlement MakeSettlement(string from, string to, decimal amount) => new()
    {
        Id = Guid.NewGuid(),
        GroupId = GroupId,
        FromUserId = from,
        ToUserId = to,
        Amount = amount,
        Date = new DateOnly(2026, 6, 5),
        CreatedAtUtc = DateTime.UtcNow
    };
}
