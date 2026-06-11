using MediatR;
using SplitMate.Application.Common;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Services;

namespace SplitMate.Application.Balances;

public record MemberBalanceDto(string UserId, string DisplayName, decimal NetBalance);

public record SimplifiedDebtDto(
    string FromUserId,
    string FromDisplayName,
    string ToUserId,
    string ToDisplayName,
    decimal Amount);

public record GroupBalancesDto(
    Guid GroupId,
    string GroupName,
    IReadOnlyList<MemberBalanceDto> MemberBalances,
    IReadOnlyList<SimplifiedDebtDto> SimplifiedDebts);

public record GetGroupBalancesQuery(Guid GroupId, string CurrentUserId)
    : IRequest<Result<GroupBalancesDto>>;

public class GetGroupBalancesQueryHandler(
    IGroupRepository groups,
    IExpenseRepository expenses,
    ISettlementRepository settlements,
    IUserDirectory users)
    : IRequestHandler<GetGroupBalancesQuery, Result<GroupBalancesDto>>
{
    public async Task<Result<GroupBalancesDto>> Handle(
        GetGroupBalancesQuery request, CancellationToken cancellationToken)
    {
        var group = await groups.GetWithMembersAsync(request.GroupId, cancellationToken);
        if (group is null)
        {
            return Result<GroupBalancesDto>.Failure("Group not found.");
        }

        if (group.Members.All(m => m.UserId != request.CurrentUserId))
        {
            return Result<GroupBalancesDto>.Failure("You are not a member of this group.");
        }

        var memberIds = group.Members.Select(m => m.UserId).ToList();
        var groupExpenses = await expenses.GetForGroupAsync(request.GroupId, cancellationToken);
        var groupSettlements = await settlements.GetForGroupAsync(request.GroupId, cancellationToken);

        var net = BalanceCalculator.ComputeNetBalances(memberIds, groupExpenses, groupSettlements);
        var debts = DebtSimplifier.Simplify(
            net.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
               .Select(kvp => (kvp.Key, kvp.Value))
               .ToList());

        var userRefs = await users.GetByIdsAsync(net.Keys, cancellationToken);
        string NameOf(string userId) =>
            userRefs.TryGetValue(userId, out var u) ? u.DisplayName : "Unknown user";

        var dto = new GroupBalancesDto(
            group.Id,
            group.Name,
            net.Select(kvp => new MemberBalanceDto(kvp.Key, NameOf(kvp.Key), kvp.Value))
               .OrderBy(b => b.DisplayName, StringComparer.OrdinalIgnoreCase)
               .ToList(),
            debts.Select(d => new SimplifiedDebtDto(
                    d.FromUserId, NameOf(d.FromUserId), d.ToUserId, NameOf(d.ToUserId), d.Amount))
                 .ToList());

        return Result<GroupBalancesDto>.Success(dto);
    }
}
