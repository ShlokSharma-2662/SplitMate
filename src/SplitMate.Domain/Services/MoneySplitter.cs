namespace SplitMate.Domain.Services;

/// <summary>
/// Pure service that splits a money amount into per-user shares using the
/// largest-remainder method, guaranteeing the shares sum exactly to the total:
/// each raw share is rounded down to 2 decimal places (paise), then the leftover
/// paise are handed out one at a time — largest fractional remainder first,
/// ties broken by UserId (ordinal) — until the sum equals the total exactly.
/// No share is ever negative.
/// </summary>
public static class MoneySplitter
{
    /// <summary>Splits <paramref name="total"/> equally among the given users.</summary>
    /// <exception cref="ArgumentException">Thrown when total is negative or no users are given.</exception>
    public static IReadOnlyList<(string UserId, decimal Amount)> SplitEqual(
        decimal total, IReadOnlyList<string> userIds)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        if (userIds.Count == 0) throw new ArgumentException("At least one participant is required.", nameof(userIds));
        if (total < 0m) throw new ArgumentException("Total must not be negative.", nameof(total));

        var rawShare = total / userIds.Count;
        return Distribute(total, userIds.Select(id => (id, RawShare: rawShare)).ToList());
    }

    /// <summary>Splits <paramref name="total"/> proportionally to each user's percentage.</summary>
    /// <param name="percentages">Per-user percentage. Must sum to exactly 100.</param>
    /// <exception cref="ArgumentException">Thrown when total is negative, percentages do not sum to 100, or any percentage is negative.</exception>
    public static IReadOnlyList<(string UserId, decimal Amount)> SplitByPercentage(
        decimal total, IReadOnlyList<(string UserId, decimal Percentage)> percentages)
    {
        ArgumentNullException.ThrowIfNull(percentages);
        if (percentages.Count == 0) throw new ArgumentException("At least one participant is required.", nameof(percentages));
        if (total < 0m) throw new ArgumentException("Total must not be negative.", nameof(total));
        if (percentages.Any(p => p.Percentage < 0m))
            throw new ArgumentException("Percentages must not be negative.", nameof(percentages));
        if (percentages.Sum(p => p.Percentage) != 100m)
            throw new ArgumentException("Percentages must sum to exactly 100.", nameof(percentages));

        var raw = percentages
            .Select(p => (p.UserId, RawShare: total * p.Percentage / 100m))
            .ToList();
        return Distribute(total, raw);
    }

    /// <summary>
    /// Largest-remainder allocation: floor each raw share to paise, then distribute the
    /// remaining paise one at a time, largest remainder first, ties by UserId.
    /// </summary>
    private static IReadOnlyList<(string UserId, decimal Amount)> Distribute(
        decimal total, List<(string UserId, decimal RawShare)> rawShares)
    {
        var floored = rawShares
            .Select(s => (s.UserId, Floor: Math.Floor(s.RawShare * 100m) / 100m,
                          Remainder: s.RawShare - Math.Floor(s.RawShare * 100m) / 100m))
            .ToList();

        var remainingPaise = (long)Math.Round((total - floored.Sum(f => f.Floor)) * 100m);

        var byRemainder = floored
            .OrderByDescending(f => f.Remainder)
            .ThenBy(f => f.UserId, StringComparer.Ordinal)
            .Select(f => f.UserId)
            .ToList();

        var extra = new Dictionary<string, decimal>();
        // remainingPaise is always < participant count, so one pass suffices,
        // but loop defensively in case of accumulated raw-share rounding.
        var i = 0;
        while (remainingPaise > 0)
        {
            var userId = byRemainder[i % byRemainder.Count];
            extra[userId] = extra.GetValueOrDefault(userId) + 0.01m;
            remainingPaise--;
            i++;
        }

        return floored
            .Select(f => (f.UserId, Amount: f.Floor + extra.GetValueOrDefault(f.UserId)))
            .ToList();
    }
}
