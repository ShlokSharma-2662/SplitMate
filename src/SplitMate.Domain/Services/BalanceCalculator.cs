using SplitMate.Domain.Entities;

namespace SplitMate.Domain.Services;

/// <summary>
/// Pure service that computes each member's net balance within a group:
/// net = (total paid) − (total owed shares) + (settlements received) − (settlements paid).
/// Positive = others owe the user; negative = the user owes others.
/// The sum of all balances in a group is always zero.
/// </summary>
public static class BalanceCalculator
{
    /// <summary>Computes the net balance for every member of a group.</summary>
    /// <param name="memberUserIds">All group members; members with no activity get a zero balance.</param>
    public static IReadOnlyDictionary<string, decimal> ComputeNetBalances(
        IEnumerable<string> memberUserIds,
        IEnumerable<Expense> expenses,
        IEnumerable<Settlement> settlements)
    {
        ArgumentNullException.ThrowIfNull(memberUserIds);
        ArgumentNullException.ThrowIfNull(expenses);
        ArgumentNullException.ThrowIfNull(settlements);

        var net = memberUserIds.Distinct().ToDictionary(id => id, _ => 0m);

        foreach (var expense in expenses)
        {
            Add(net, expense.PaidByUserId, expense.Amount);
            foreach (var share in expense.Shares)
            {
                Add(net, share.UserId, -share.ShareAmount);
            }
        }

        foreach (var settlement in settlements)
        {
            // Paying a settlement reduces the payer's debt (raises their net);
            // receiving one reduces what the receiver is owed (lowers their net).
            Add(net, settlement.FromUserId, settlement.Amount);
            Add(net, settlement.ToUserId, -settlement.Amount);
        }

        return net;
    }

    private static void Add(Dictionary<string, decimal> net, string userId, decimal delta)
        => net[userId] = net.GetValueOrDefault(userId) + delta;
}
