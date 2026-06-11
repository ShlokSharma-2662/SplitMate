using Microsoft.EntityFrameworkCore;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Infrastructure.Persistence;

namespace SplitMate.Infrastructure.Identity;

public class UserDirectory(IDbContextFactory<SplitMateDbContext> factory) : IUserDirectory
{
    public async Task<UserRef?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);
        return user is null ? null : new UserRef(user.Id, user.DisplayName, user.Email ?? string.Empty);
    }

    public async Task<IReadOnlyDictionary<string, UserRef>> GetByIdsAsync(
        IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<string, UserRef>();
        }

        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => new UserRef(u.Id, u.DisplayName, u.Email ?? string.Empty),
                ct);
    }
}
