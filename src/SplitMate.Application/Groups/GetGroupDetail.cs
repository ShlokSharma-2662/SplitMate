using MediatR;
using SplitMate.Application.Common;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Enums;

namespace SplitMate.Application.Groups;

public record GroupMemberDto(string UserId, string DisplayName, string Email, DateTime JoinedAtUtc);

public record ExpenseShareDto(string UserId, string DisplayName, decimal ShareAmount);

public record ExpenseDto(
    Guid Id,
    string Description,
    decimal Amount,
    string PaidByUserId,
    string PaidByDisplayName,
    DateOnly Date,
    SplitType SplitType,
    DateTime CreatedAtUtc,
    IReadOnlyList<ExpenseShareDto> Shares);

public record GroupDetailDto(
    Guid Id,
    string Name,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    IReadOnlyList<GroupMemberDto> Members,
    IReadOnlyList<ExpenseDto> Expenses);

public record GetGroupDetailQuery(Guid GroupId, string CurrentUserId) : IRequest<Result<GroupDetailDto>>;

public class GetGroupDetailQueryHandler(
    IGroupRepository groups, IExpenseRepository expenses, IUserDirectory users)
    : IRequestHandler<GetGroupDetailQuery, Result<GroupDetailDto>>
{
    public async Task<Result<GroupDetailDto>> Handle(
        GetGroupDetailQuery request, CancellationToken cancellationToken)
    {
        var group = await groups.GetWithMembersAsync(request.GroupId, cancellationToken);
        if (group is null)
        {
            return Result<GroupDetailDto>.Failure("Group not found.");
        }

        if (group.Members.All(m => m.UserId != request.CurrentUserId))
        {
            return Result<GroupDetailDto>.Failure("You are not a member of this group.");
        }

        var groupExpenses = await expenses.GetForGroupAsync(request.GroupId, cancellationToken);

        var userIds = group.Members.Select(m => m.UserId)
            .Concat(groupExpenses.Select(e => e.PaidByUserId))
            .Concat(groupExpenses.SelectMany(e => e.Shares).Select(s => s.UserId));
        var userRefs = await users.GetByIdsAsync(userIds, cancellationToken);

        string NameOf(string userId) =>
            userRefs.TryGetValue(userId, out var u) ? u.DisplayName : "Unknown user";

        var dto = new GroupDetailDto(
            group.Id,
            group.Name,
            group.CreatedByUserId,
            group.CreatedAtUtc,
            group.Members
                .Select(m => new GroupMemberDto(
                    m.UserId,
                    NameOf(m.UserId),
                    userRefs.TryGetValue(m.UserId, out var u) ? u.Email : string.Empty,
                    m.JoinedAtUtc))
                .OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            groupExpenses
                .Select(e => new ExpenseDto(
                    e.Id,
                    e.Description,
                    e.Amount,
                    e.PaidByUserId,
                    NameOf(e.PaidByUserId),
                    e.Date,
                    e.SplitType,
                    e.CreatedAtUtc,
                    e.Shares
                        .Select(s => new ExpenseShareDto(s.UserId, NameOf(s.UserId), s.ShareAmount))
                        .OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .ToList()))
                .ToList());

        return Result<GroupDetailDto>.Success(dto);
    }
}
