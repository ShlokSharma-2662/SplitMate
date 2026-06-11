using Microsoft.EntityFrameworkCore;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Domain.Entities;

namespace SplitMate.Infrastructure.Persistence.Repositories;

public class SettlementRepository(IDbContextFactory<SplitMateDbContext> factory) : ISettlementRepository
{
    public async Task<IReadOnlyList<Settlement>> GetForGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Settlements.AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Settlement settlement, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        db.Settlements.Add(settlement);
        await db.SaveChangesAsync(ct);
    }
}
