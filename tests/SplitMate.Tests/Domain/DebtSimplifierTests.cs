using FluentAssertions;
using SplitMate.Domain.Services;

namespace SplitMate.Tests.Domain;

public class DebtSimplifierTests
{
    [Fact]
    public void Empty_input_returns_empty_output()
    {
        var result = DebtSimplifier.Simplify([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void All_zero_balances_return_empty_output()
    {
        var result = DebtSimplifier.Simplify([("u1", 0m), ("u2", 0m), ("u3", 0m)]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void One_debtor_one_creditor_produces_single_transaction()
    {
        var result = DebtSimplifier.Simplify([("debtor", -250m), ("creditor", 250m)]);

        result.Should().ContainSingle()
            .Which.Should().Be(new SimplifiedDebt("debtor", "creditor", 250m));
    }

    [Fact]
    public void One_debtor_multiple_creditors_pays_each_creditor()
    {
        var result = DebtSimplifier.Simplify([("d1", -100m), ("c1", 60m), ("c2", 40m)]);

        result.Should().BeEquivalentTo(new[]
        {
            new SimplifiedDebt("d1", "c1", 60m),
            new SimplifiedDebt("d1", "c2", 40m)
        });
        AssertSettlesExactly([("d1", -100m), ("c1", 60m), ("c2", 40m)], result);
    }

    [Fact]
    public void Multiple_debtors_one_creditor_each_pay_the_creditor()
    {
        var result = DebtSimplifier.Simplify([("d1", -70m), ("d2", -30m), ("c1", 100m)]);

        result.Should().BeEquivalentTo(new[]
        {
            new SimplifiedDebt("d1", "c1", 70m),
            new SimplifiedDebt("d2", "c1", 30m)
        });
        AssertSettlesExactly([("d1", -70m), ("d2", -30m), ("c1", 100m)], result);
    }

    [Fact]
    public void Complex_case_uses_at_most_n_minus_1_transactions_and_zeroes_all_balances()
    {
        var balances = new List<(string, decimal)>
        {
            ("u1", 433.34m),
            ("u2", -216.67m),
            ("u3", -216.67m),
            ("u4", 1000.00m),
            ("u5", -500.00m),
            ("u6", -500.00m),
            ("u7", 0m)
        };

        var result = DebtSimplifier.Simplify(balances);

        result.Should().HaveCountLessThanOrEqualTo(balances.Count - 1);
        result.Should().OnlyContain(d => d.Amount > 0m);
        AssertSettlesExactly(balances, result);
    }

    [Fact]
    public void Non_zero_sum_input_throws()
    {
        var act = () => DebtSimplifier.Simplify([("u1", 10m), ("u2", -9.99m)]);

        act.Should().Throw<ArgumentException>().WithMessage("*sum to zero*");
    }

    [Fact]
    public void Equal_balances_are_tie_broken_by_user_id_deterministically()
    {
        var balances = new List<(string, decimal)>
        {
            ("zeta", -50m), ("alpha", -50m), ("mike", 50m), ("bravo", 50m)
        };

        var first = DebtSimplifier.Simplify(balances);
        var second = DebtSimplifier.Simplify(balances.AsEnumerable().Reverse().ToList());

        // Ties on amount must resolve by UserId, so input order must not matter.
        first.Should().Equal(
            new SimplifiedDebt("alpha", "bravo", 50m),
            new SimplifiedDebt("zeta", "mike", 50m));
        second.Should().Equal(first);
    }

    [Fact]
    public void Output_never_contains_zero_or_negative_amounts()
    {
        var result = DebtSimplifier.Simplify(
            [("a", -0.01m), ("b", 0.01m), ("c", 0m)]);

        result.Should().OnlyContain(d => d.Amount > 0m);
    }

    /// <summary>Applying every suggested repayment must bring every balance to exactly zero.</summary>
    private static void AssertSettlesExactly(
        IEnumerable<(string UserId, decimal NetBalance)> balances,
        IEnumerable<SimplifiedDebt> debts)
    {
        var net = balances.ToDictionary(b => b.UserId, b => b.NetBalance);
        foreach (var debt in debts)
        {
            net[debt.FromUserId] += debt.Amount;
            net[debt.ToUserId] -= debt.Amount;
        }

        net.Values.Should().OnlyContain(v => v == 0m);
    }
}
