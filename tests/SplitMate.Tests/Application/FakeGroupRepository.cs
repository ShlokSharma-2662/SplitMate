using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Entities;

namespace SplitMate.Tests.Application;

/// <summary>Minimal in-memory stand-in for validator tests; only member lookup is supported.</summary>
internal sealed class FakeGroupRepository(params string[] memberIds) : IGroupRepository
{
    public Task<IReadOnlyList<string>> GetMemberUserIdsAsync(Guid groupId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(memberIds);

    public Task<Group?> GetWithMembersAsync(Guid groupId, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<Group>> GetGroupsForUserAsync(string userId, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task AddAsync(Group group, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task AddMemberAsync(GroupMember member, CancellationToken ct = default)
        => throw new NotSupportedException();
}
