using MediatR;
using SplitMate.Application.Common;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Services;

namespace SplitMate.Application.Dashboard;

public enum ActivityType { Expense, Settlement }

public record ActivityItemDto(
    ActivityType Type,
    Guid GroupId,
    string GroupName,
    string Title,
    string Subtitle,
    decimal Amount,
    DateOnly Date,
    DateTime CreatedAtUtc);

public record DashboardSummaryDto(
    decimal TotalIOwe,
    decimal TotalIAmOwed,
    IReadOnlyList<ActivityItemDto> RecentActivity);

public record GetDashboardSummaryQuery(string CurrentUserId) : IRequest<Result<DashboardSummaryDto>>;

public class GetDashboardSummaryQueryHandler(
    IGroupRepository groups,
    IExpenseRepository expenses,
    ISettlementRepository settlements,
    IUserDirectory users)
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    public async Task<Result<DashboardSummaryDto>> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var me = request.CurrentUserId;
        var myGroups = await groups.GetGroupsForUserAsync(me, cancellationToken);

        var totalIOwe = 0m;
        var totalIAmOwed = 0m;
        var rawActivity = new List<(Domain.Entities.Expense? Expense,
            Domain.Entities.Settlement? Settlement, string GroupName)>();

        foreach (var group in myGroups)
        {
            var groupExpenses = await expenses.GetForGroupAsync(group.Id, cancellationToken);
            var groupSettlements = await settlements.GetForGroupAsync(group.Id, cancellationToken);

            var net = BalanceCalculator.ComputeNetBalances(
                group.Members.Select(m => m.UserId), groupExpenses, groupSettlements);
            var mine = net.GetValueOrDefault(me);
            // Per-group nets are not netted against each other across groups.
            if (mine > 0m) totalIAmOwed += mine;
            else totalIOwe += -mine;

            rawActivity.AddRange(groupExpenses
                .Where(e => e.PaidByUserId == me || e.Shares.Any(s => s.UserId == me))
                .Select(e => ((Domain.Entities.Expense?)e, (Domain.Entities.Settlement?)null, group.Name)));
            rawActivity.AddRange(groupSettlements
                .Where(s => s.FromUserId == me || s.ToUserId == me)
                .Select(s => ((Domain.Entities.Expense?)null, (Domain.Entities.Settlement?)s, group.Name)));
        }

        var recent = rawActivity
            .OrderByDescending(a => a.Expense?.CreatedAtUtc ?? a.Settlement!.CreatedAtUtc)
            .Take(10)
            .ToList();

        var userIds = recent
            .SelectMany(a => a.Expense is not null
                ? new[] { a.Expense.PaidByUserId }
                : [a.Settlement!.FromUserId, a.Settlement.ToUserId]);
        var userRefs = await users.GetByIdsAsync(userIds, cancellationToken);
        string NameOf(string userId) =>
            userId == me ? "You" :
            userRefs.TryGetValue(userId, out var u) ? u.DisplayName : "Unknown user";

        var activity = recent
            .Select(a => a.Expense is not null
                ? new ActivityItemDto(
                    ActivityType.Expense,
                    a.Expense.GroupId,
                    a.GroupName,
                    a.Expense.Description,
                    $"paid by {NameOf(a.Expense.PaidByUserId)}",
                    a.Expense.Amount,
                    a.Expense.Date,
                    a.Expense.CreatedAtUtc)
                : new ActivityItemDto(
                    ActivityType.Settlement,
                    a.Settlement!.GroupId,
                    a.GroupName,
                    a.Settlement.Note is { Length: > 0 } note ? note : "Settlement",
                    $"{NameOf(a.Settlement.FromUserId)} paid {NameOf(a.Settlement.ToUserId)}",
                    a.Settlement.Amount,
                    a.Settlement.Date,
                    a.Settlement.CreatedAtUtc))
            .ToList();

        return Result<DashboardSummaryDto>.Success(
            new DashboardSummaryDto(totalIOwe, totalIAmOwed, activity));
    }
}
