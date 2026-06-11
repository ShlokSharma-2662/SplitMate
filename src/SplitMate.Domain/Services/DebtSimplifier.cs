namespace SplitMate.Domain.Services;

/// <summary>
/// Pure, side-effect-free service that converts a set of net balances into a small
/// list of suggested repayments using greedy max-debtor → max-creditor matching.
/// Produces at most n−1 transactions for n users with non-zero balances.
/// Finding the true minimum number of transactions is NP-hard; greedy is a good,
/// fast approximation that is always ≤ n−1.
/// </summary>
public static class DebtSimplifier
{
    /// <summary>
    /// Simplifies the given net balances into pairwise debts.
    /// Positive balance = the user is owed money; negative = the user owes money.
    /// </summary>
    /// <param name="balances">Net balance per user. Must sum to exactly zero.</param>
    /// <returns>Deterministic list of (from, to, amount) repayments that zero all balances.</returns>
    /// <exception cref="ArgumentException">Thrown when balances do not sum to exactly zero.</exception>
    public static IReadOnlyList<SimplifiedDebt> Simplify(
        IReadOnlyList<(string UserId, decimal NetBalance)> balances)
    {
        ArgumentNullException.ThrowIfNull(balances);

        var sum = balances.Sum(b => b.NetBalance);
        if (sum != 0m)
        {
            throw new ArgumentException(
                $"Net balances must sum to zero but sum to {sum}.", nameof(balances));
        }

        // Sort once by UserId (ordinal) so ties are broken deterministically.
        var debtors = balances
            .Where(b => b.NetBalance < 0m)
            .OrderBy(b => b.UserId, StringComparer.Ordinal)
            .Select(b => (b.UserId, Remaining: -b.NetBalance)) // store as positive "amount owed"
            .ToList();

        var creditors = balances
            .Where(b => b.NetBalance > 0m)
            .OrderBy(b => b.UserId, StringComparer.Ordinal)
            .Select(b => (b.UserId, Remaining: b.NetBalance))
            .ToList();

        var result = new List<SimplifiedDebt>();

        while (debtors.Count > 0 && creditors.Count > 0)
        {
            // Largest debtor / creditor; on equal amounts the earlier (smaller) UserId
            // wins because the lists were pre-sorted by UserId.
            var di = IndexOfMax(debtors);
            var ci = IndexOfMax(creditors);

            var debtor = debtors[di];
            var creditor = creditors[ci];
            var transfer = Math.Min(debtor.Remaining, creditor.Remaining);

            result.Add(new SimplifiedDebt(debtor.UserId, creditor.UserId, transfer));

            debtor.Remaining -= transfer;
            creditor.Remaining -= transfer;

            if (debtor.Remaining == 0m) debtors.RemoveAt(di);
            else debtors[di] = debtor;

            if (creditor.Remaining == 0m) creditors.RemoveAt(ci);
            else creditors[ci] = creditor;
        }

        return result;
    }

    private static int IndexOfMax(List<(string UserId, decimal Remaining)> items)
    {
        var maxIndex = 0;
        for (var i = 1; i < items.Count; i++)
        {
            if (items[i].Remaining > items[maxIndex].Remaining)
            {
                maxIndex = i;
            }
        }

        return maxIndex;
    }
}
