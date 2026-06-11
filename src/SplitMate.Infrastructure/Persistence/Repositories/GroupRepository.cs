using Microsoft.EntityFrameworkCore;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Entities;

namespace SplitMate.Infrastructure.Persistence.Repositories;

// Repositories create a short-lived DbContext per call (via the factory) because
// Blazor Server scopes live as long as the circuit, which makes a shared scoped
// DbContext prone to concurrency and stale-data issues.
public class GroupRepository(IDbContextFactory<SplitMateDbContext> factory) : IGroupRepository
{
    public async Task<Group?> GetWithMembersAsync(Guid groupId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Groups.AsNoTracking()
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);
    }

    public async Task<IReadOnlyList<Group>> GetGroupsForUserAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Groups.AsNoTracking()
            .Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderBy(g => g.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetMemberUserIdsAsync(Guid groupId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Group group, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        db.Groups.Add(group);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddMemberAsync(GroupMember member, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        db.GroupMembers.Add(member);
        await db.SaveChangesAsync(ct);
    }
}
