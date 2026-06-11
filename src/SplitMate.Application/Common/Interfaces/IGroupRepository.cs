using SplitMate.Domain.Entities;

namespace SplitMate.Application.Common.Interfaces;

public interface IGroupRepository
{
    Task<Group?> GetWithMembersAsync(Guid groupId, CancellationToken ct = default);
    Task<IReadOnlyList<Group>> GetGroupsForUserAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetMemberUserIdsAsync(Guid groupId, CancellationToken ct = default);
    Task AddAsync(Group group, CancellationToken ct = default);
    Task AddMemberAsync(GroupMember member, CancellationToken ct = default);
}
