using FluentAssertions;
using SplitMate.Domain.Services;

namespace SplitMate.Tests.Domain;

public class MoneySplitterTests
{
    [Fact]
    public void Rs100_split_3_ways_gives_extra_paisa_to_first_user_by_id()
    {
        var result = MoneySplitter.SplitEqual(100m, ["u1", "u2", "u3"]);

        result.Should().Equal(("u1", 33.34m), ("u2", 33.33m), ("u3", 33.33m));
        result.Sum(r => r.Amount).Should().Be(100m);
    }

    [Fact]
    public void Rs0_01_split_2_ways_gives_one_user_the_paisa_and_no_negative_share()
    {
        var result = MoneySplitter.SplitEqual(0.01m, ["u1", "u2"]);

        result.Should().Equal(("u1", 0.01m), ("u2", 0.00m));
        result.Sum(r => r.Amount).Should().Be(0.01m);
        result.Should().OnlyContain(r => r.Amount >= 0m);
    }

    [Fact]
    public void Rs99_99_split_7_ways_sums_exactly_with_three_users_getting_an_extra_paisa()
    {
        var result = MoneySplitter.SplitEqual(99.99m, ["u1", "u2", "u3", "u4", "u5", "u6", "u7"]);

        result.Should().Equal(
            ("u1", 14.29m), ("u2", 14.29m), ("u3", 14.29m),
            ("u4", 14.28m), ("u5", 14.28m), ("u6", 14.28m), ("u7", 14.28m));
        result.Sum(r => r.Amount).Should().Be(99.99m);
    }

    [Theory]
    [InlineData(1.00, 3)]
    [InlineData(200.10, 6)]
    [InlineData(0.05, 4)]
    [InlineData(123456.78, 11)]
    public void Equal_split_always_sums_exactly_and_never_goes_negative(decimal total, int count)
    {
        var userIds = Enumerable.Range(1, count).Select(i => $"u{i:D2}").ToList();

        var result = MoneySplitter.SplitEqual(total, userIds);

        result.Sum(r => r.Amount).Should().Be(total);
        result.Should().OnlyContain(r => r.Amount >= 0m);
        // Largest-remainder equal split: shares differ by at most one paisa.
        (result.Max(r => r.Amount) - result.Min(r => r.Amount)).Should().BeLessThanOrEqualTo(0.01m);
    }

    [Fact]
    public void Percentage_split_with_terminating_shares_is_exact()
    {
        var result = MoneySplitter.SplitByPercentage(450m,
            [("u1", 50m), ("u2", 30m), ("u3", 20m)]);

        result.Should().Equal(("u1", 225m), ("u2", 135m), ("u3", 90m));
    }

    [Fact]
    public void Percentage_split_distributes_leftover_paise_to_largest_remainder_first()
    {
        // Raw shares: 33.6633, 33.6633, 33.6734 → floors sum to 100.99, one paisa left.
        // u3 has the largest fractional remainder so it gets the extra paisa.
        var result = MoneySplitter.SplitByPercentage(101m,
            [("u1", 33.33m), ("u2", 33.33m), ("u3", 33.34m)]);

        result.Should().Equal(("u1", 33.66m), ("u2", 33.66m), ("u3", 33.68m));
        result.Sum(r => r.Amount).Should().Be(101m);
    }

    [Fact]
    public void Percentages_not_summing_to_100_throw()
    {
        var act = () => MoneySplitter.SplitByPercentage(100m, [("u1", 60m), ("u2", 39.99m)]);

        act.Should().Throw<ArgumentException>().WithMessage("*sum to exactly 100*");
    }

    [Fact]
    public void Empty_participants_throw()
    {
        var act = () => MoneySplitter.SplitEqual(100m, []);

        act.Should().Throw<ArgumentException>();
    }
}
